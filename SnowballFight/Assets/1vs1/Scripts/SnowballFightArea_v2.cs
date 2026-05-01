using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class SnowballFightArea_v2 : MonoBehaviour
{
    public List<SnowballFightPlayerState_v2> playerStates = new List<SnowballFightPlayerState_v2>();
    public bool teamDeath;
    public float damagePercentage;

    private bool m_IsResetting = false;

    // Are we training this platform or is this game/movie mode
    // This determines if win screens and various effects will trigger
    public enum SceneType
    {
        Game,
        Training
    }

    public bool m_Initialized;

    public UIDocument UIDoc;
    public Label InfoText;

    public SceneType CurrentSceneType = SceneType.Training;

    public bool ShouldPlayEffects
    {
        get
        {
            return CurrentSceneType != SceneType.Training;
        }
    }

    public void Initialize()
    {
        ResetScene();
        m_Initialized = true;
    }

    // Start is called before the first frame update
    void Start()
    {
        // Display Info text doc if we are in a game mode.
        if (CurrentSceneType == SceneType.Game)
        {
            // Get the label
            var root = UIDoc.rootVisualElement;
            InfoText = root.Q<Label>("InfoText");
        }
        
    }

    /// <summary>
    /// Show a countdown UI when the round starts
    /// </summary>
    /// <returns></returns>
    IEnumerator GameCountdown()
    {
        Time.timeScale = 0;
        InfoText.text = "3";
        yield return new WaitForSecondsRealtime(1);
        InfoText.text = "2";
        yield return new WaitForSecondsRealtime(1);
        InfoText.text = "1";
        yield return new WaitForSecondsRealtime(1);
        InfoText.text = "Go!";
        yield return new WaitForSecondsRealtime(1);
        Time.timeScale = 1;
        InfoText.text = "";
    }



    public void Awake()
    {
        teamDeath = false;
    }

    /// <summary>
    /// When the snowball hits
    /// </summary>
    /// <param name="hitAgent"></param>
    /// <param name="attackerTeamId"></param>
    public void SnowballHit(SnowballFightAgent_v2 hitAgent, int attackerTeamId)
    {
        // Prevent recursive calls or simultaneous resets
        if (m_IsResetting) return;

        // Reduce health only for the specific agent hit
        hitAgent.health -= damagePercentage;
        hitAgent.m_health.SetHealth();

        // Give reward to all agents on the attacker team
        // Use ToArray to avoid "Collection was modified" error
        var currentStates = playerStates.ToArray();
        foreach (var ps in currentStates)
        {
            if (ps.agentScript.m_BehaviorParameters.TeamId == attackerTeamId)
            {
                ps.agentScript.AddReward(0.1f);
            }
        }

        // If the agent is dead, hide them
        if (hitAgent.health <= 0)
        {
            hitAgent.SetActiveState(false);
        }

        // Check if the victim team is wiped out
        int victimTeamId = hitAgent.m_BehaviorParameters.TeamId;
        bool teamWiped = true;

        foreach (var ps in currentStates)
        {
            if (ps.agentScript.m_BehaviorParameters.TeamId == victimTeamId)
            {
                if (ps.agentScript.health > 0)
                {
                    teamWiped = false;
                    break;
                }
            }
        }

        // If the entire team is dead
        if (teamWiped)
        {
            m_IsResetting = true; // Lock the reset process

            float winnerTeamRemainingHealth = 0f;
            foreach (var ps in currentStates)
            {
                if (ps.agentScript.m_BehaviorParameters.TeamId != victimTeamId)
                {
                    winnerTeamRemainingHealth += ps.agentScript.health;
                }
            }

            foreach (var ps in currentStates)
            {
                if (ps.agentScript.m_BehaviorParameters.TeamId == victimTeamId)
                {
                    ps.agentScript.AddReward(-1.0f);
                }
                else
                {
                    // Winner team gets bonus
                    ps.agentScript.AddReward(1.0f + (winnerTeamRemainingHealth * 0.25f) + ps.agentScript.timePenalty);
                }
                
                ps.agentScript.EndEpisode();
            }
            
            if (CurrentSceneType == SceneType.Game)
            {
                ResetScene();
            }

            m_IsResetting = false; // Unlock
        }
    }

    void ResetScene()
    {
        StopAllCoroutines();

        //Clear win screens and start countdown
        if (ShouldPlayEffects)
        {
            if (CurrentSceneType == SceneType.Game)
            {
                StartCoroutine(GameCountdown());
            }
        }
    }

    // Update is called once per frame
    void Update()
    {

        if (!m_Initialized)
        {
            Initialize();
        }
    }
}