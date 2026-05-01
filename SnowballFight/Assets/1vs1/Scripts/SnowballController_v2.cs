using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SnowballController_v2 : MonoBehaviour
{
    public SnowballFightArea_v2 area;
    public int TeamToIgnore;

    public SnowballFightAgent_v2 Shooter;

    void Awake()
    {
        // Get the SnowballFightv2Area
        area = this.transform.GetComponentInParent<SnowballFightArea_v2>();
    }

    /// <summary>
    /// When the snowball hits an agent, wall, shelter or floor, we call the Area Snowballhit function.
    /// </summary>
    /// <param name="collision">The collision the snowball hit</param>
    private void OnTriggerEnter(Collider collision)
    {
        // Try to get the Agent component from the hit object
        SnowballFightAgent_v2 hitAgent = collision.gameObject.GetComponent<SnowballFightAgent_v2>();

        if (hitAgent != null && hitAgent.health > 0)
        {
            // If the agent belongs to the other team
            if (hitAgent.m_BehaviorParameters.TeamId != TeamToIgnore)
            {
                // ADDED: Reward the shooter for hitting the enemy
                if (Shooter != null)
                {
                    Shooter.AddReward(0.3f);
                }

                area.SnowballHit(hitAgent, TeamToIgnore);
                Destroy(gameObject);
            }
        }

        if (collision.gameObject.CompareTag("wall") || collision.gameObject.CompareTag("shelter") || collision.gameObject.CompareTag("floor"))
        {
            Destroy(gameObject);
        }
    }    
}
