using UnityEngine;
using UnityEngine.UI;

using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;

public class SnowballFightAgent_v2 : Agent
{
    /// <summary>
    /// What team we are
    /// </summary>
    public enum Team
    {
        Blue = 0,
        Purple = 1,
    }
    [HideInInspector]
    public Team team;

    // Speed
    [HideInInspector]
    public float m_ForwardSpeed;
    [HideInInspector]
    public float m_LateralSpeed;

    // Time penalty
    [HideInInspector]
    public float timePenalty;
    [HideInInspector]
    public float m_Penalty;

    // Behavior Parameters
    public BehaviorParameters m_BehaviorParameters;

    [Header("THROW FORCES")]
    public float forceToUse;
    public ForceMode forceMode;

    [Header("HEALTH SYSTEM")]
    public float health;
    public SnowballFightHealth_v2 m_health;
    public int hitPointsRemaining;
    public int NumberOfTimesPlayerCanBeHit = 2;

    [Header("SHOOT SYSTEM")]
    //public 
    public float nextFire;
    public bool possibleShoot;
    public Slider m_ShootSlider;

    [Header("HOME")]
    public Vector3 m_HomeDirection;
    public Vector3 transformPos;
    public Quaternion transformRot;

    [Header("AREA")]
    public SnowballFightArea_v2 area;

    [Header("ENVIRONMENT GAME OBJECTS")]
    public Rigidbody agentRb;
    public GameObject agentShootingGO;
    public GameObject snowballGO;
    public Transform projectileOrigin; //the transform the projectile will originate from
    int m_PlayerIndex;

    [HideInInspector]
    public GameObject[] snowballs;

    private SnowballFightAgent_v2 m_Teammate;
    private bool m_IsInitialized = false;


    /// <summary>
    /// Fire the snowball
    /// </summary>
    public void FireSnowball()
    {
        // Instantiate the snowball from agentShootingGameObject position
        GameObject snowball = GameObject.Instantiate(snowballGO,
                                                    agentShootingGO.transform.position,
                                                    agentShootingGO.transform.rotation,
                                                    area.transform);

        // Define the snowball team ID of the launcher (no friendly fire)
        var snowballController = snowball.GetComponent<SnowballController_v2>();
        snowballController.TeamToIgnore = m_BehaviorParameters.TeamId;
        snowballController.Shooter = this;

        // Get the snowball rb and define its position and velocity
        Rigidbody rb = snowball.GetComponent<Rigidbody>();
        rb.transform.position = projectileOrigin.position;
        rb.transform.rotation = projectileOrigin.rotation;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.gameObject.SetActive(true);

        // Add force to the snowball rb
        rb.AddForce(projectileOrigin.forward * forceToUse, forceMode);

        // Small penalty for firing to prevent spamming (reduced to encourage more shooting)
        AddReward(-0.005f);
    }

    /// <summary>
    /// Initialize the environment
    /// </summary>
    public override void Initialize()
    {
        if (m_IsInitialized) return;
        m_IsInitialized = true;

        // Calculate the penality rate (this push our agent to meet faster its goal)
        m_Penalty = 1f / MaxStep;

        // Get the behavior parameters (to place correctly the agent)
        m_BehaviorParameters = gameObject.GetComponent<BehaviorParameters>();

        // Store the initial position and rotation from the scene
        transformPos = transform.localPosition;
        transformRot = transform.localRotation;

        // Map behavior team ID to internal team enum
        if (m_BehaviorParameters.TeamId == 0)
        {
            team = Team.Blue;
        }
        else
        {
            team = Team.Purple;
        }

        // Define the health
        health = 1.0f;
        m_health.SetHealth();

        // Define the forward and lateral speed of our agents
        m_ForwardSpeed = 0.6f;
        m_LateralSpeed = 0.2f;

        // Get the rb of the agent
        agentRb = GetComponent<Rigidbody>();
        agentRb.maxAngularVelocity = 50;

        // SnowballFightPlayerState
        var playerState = new SnowballFightPlayerState_v2
        {
            agentRb = agentRb,
            startingPos = transform.position,
            agentScript = this,
        };

        // Add this agent state to the playerStates List
        area.playerStates.Add(playerState);
        m_PlayerIndex = area.playerStates.IndexOf(playerState);
        playerState.playerIndex = m_PlayerIndex;
    }

    /// <summary>
    /// When the episode starts (when we reset the episode)
    /// </summary>
    public override void OnEpisodeBegin()
    {
        // Remove all snowballs that are still present IN THIS AREA only
        foreach (Transform child in area.transform)
        {
            if (child.CompareTag("snowball"))
            {
                Destroy(child.gameObject);
            }
        }
        
        // Set time penality to 0
        timePenalty = 0;

        transform.localPosition = transformPos;
        transform.localRotation = transformRot;
        
        agentRb.velocity = Vector3.zero;
        agentRb.angularVelocity = Vector3.zero;

        // Define the health
        health = 1.0f;
        m_health.SetHealth();
        SetActiveState(true);
    }

    /// <summary>
    /// Toggle agent's visibility and physics (for death state)
    /// </summary>
    public void SetActiveState(bool state)
    {
        // Toggle MeshRenderers
        foreach (var renderer in GetComponentsInChildren<Renderer>())
        {
            renderer.enabled = state;
        }

        // Toggle Colliders (so snowballs pass through)
        foreach (var collider in GetComponentsInChildren<Collider>())
        {
            collider.enabled = state;
        }

        // Hide Health Slider if dead (Use components instead of SetActive to avoid recursion)
        if (m_health != null)
        {
            foreach (var img in m_health.GetComponentsInChildren<Image>())
            {
                img.enabled = state;
            }
            foreach (var canvas in m_health.GetComponentsInChildren<Canvas>())
            {
                canvas.enabled = state;
            }
        }
    }

    /// <summary>
    /// Collect the observations (vector obs)
    /// </summary>
    /// <param name="sensor"></param>
    public override void CollectObservations(VectorSensor sensor)
    {
        // Can I shoot or not? (Bool)
        sensor.AddObservation(possibleShoot);

        // Remaining health
        sensor.AddObservation(health);

        // Speed
        sensor.AddObservation(Vector3.Dot(agentRb.velocity, agentRb.transform.forward));
        sensor.AddObservation(Vector3.Dot(agentRb.velocity, agentRb.transform.right));

        // Position from "home"
        sensor.AddObservation(transform.InverseTransformDirection(m_HomeDirection));

        // Teammate Observations
        if (m_Teammate == null)
        {
            foreach (var ps in area.playerStates)
            {
                if (ps.agentScript != this && ps.agentScript.m_BehaviorParameters.TeamId == m_BehaviorParameters.TeamId)
                {
                    m_Teammate = ps.agentScript;
                    break;
                }
            }
        }

        if (m_Teammate != null)
        {
            // Relative position and velocity of teammate
            sensor.AddObservation(transform.InverseTransformPoint(m_Teammate.transform.position));
            sensor.AddObservation(m_Teammate.agentRb.velocity);
            sensor.AddObservation(m_Teammate.health); // Know if teammate is low on health
        }
        else
        {
            sensor.AddObservation(Vector3.zero);
            sensor.AddObservation(Vector3.zero);
            sensor.AddObservation(0f);
        }

        // Is dead?
        sensor.AddObservation(health <= 0);

        // --- NEW: Enemy Observations (Radar) ---
        // Find the closest enemy to give the agent a "global sense" of where to turn
        SnowballFightAgent_v2 closestEnemy = null;
        float minEnemyDist = float.MaxValue;
        foreach (var ps in area.playerStates)
        {
            if (ps.agentScript != null && ps.agentScript.health > 0 && 
                ps.agentScript.m_BehaviorParameters.TeamId != m_BehaviorParameters.TeamId)
            {
                float d = Vector3.Distance(transform.position, ps.agentScript.transform.position);
                if (d < minEnemyDist)
                {
                    minEnemyDist = d;
                    closestEnemy = ps.agentScript;
                }
            }
        }

        if (closestEnemy != null)
        {
            // Add relative direction to closest enemy
            sensor.AddObservation(transform.InverseTransformPoint(closestEnemy.transform.position).normalized);
        }
        else
        {
            sensor.AddObservation(Vector3.zero);
        }
    }

    /// <summary>
    /// Hybrid Action Space (Supports both Continuous and Discrete)
    /// </summary>
    public void MoveAgent(ActionBuffers actions)
    {
        // If dead, do nothing
        if (health <= 0)
        {
            agentRb.velocity = Vector3.zero;
            agentRb.angularVelocity = Vector3.zero;
            return;
        }

        var dirToGo = Vector3.zero;
        var rotateDir = Vector3.zero;
        bool wantToShoot = false;

        // --- 1. MOVEMENT LOGIC ---
        if (actions.ContinuousActions.Length >= 3)
        {
            float forwardInput = actions.ContinuousActions[0];
            float rightInput = actions.ContinuousActions[1];
            float rotateInput = actions.ContinuousActions[2];

            dirToGo = (transform.forward * forwardInput * m_ForwardSpeed) + 
                       (transform.right * rightInput * m_LateralSpeed);
            rotateDir = transform.up * rotateInput;
        }
        else
        {
            var forwardAxis = actions.DiscreteActions[0];
            var rightAxis = actions.DiscreteActions[1];
            var rotateAxis = actions.DiscreteActions[2];

            switch (forwardAxis)
            {
                case 1: dirToGo += transform.forward * m_ForwardSpeed; break;
                case 2: dirToGo -= transform.forward * m_ForwardSpeed; break;
            }
            switch (rightAxis)
            {
                case 1: dirToGo += transform.right * m_LateralSpeed; break;
                case 2: dirToGo -= transform.right * m_LateralSpeed; break;
            }
            switch (rotateAxis)
            {
                case 1: rotateDir = transform.up * -1f; break;
                case 2: rotateDir = transform.up * 1f; break;
            }
        }

        // --- 2. SHOOTING LOGIC ---
        if (actions.ContinuousActions.Length >= 3)
        {
            wantToShoot = actions.DiscreteActions.Length > 0 && actions.DiscreteActions[0] == 1;
        }
        else
        {
            wantToShoot = actions.DiscreteActions.Length >= 4 && actions.DiscreteActions[3] == 1;
        }

        transform.Rotate(rotateDir, Time.deltaTime * 100f);
        agentRb.AddForce(dirToGo * 2f, ForceMode.VelocityChange);

        if (wantToShoot && Time.time > nextFire)
        {
            nextFire = Time.time + 2f;
            FireSnowball();
            possibleShoot = false;
        }
        else if (Time.time > nextFire)
        {
            possibleShoot = true;
        }
    }

    /// <summary>
    /// Debug movement
    /// </summary>
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        if (m_BehaviorParameters.BehaviorType == BehaviorType.HeuristicOnly)
        {
            var discreteActionsOut = actionsOut.DiscreteActions;
            var continuousActionsOut = actionsOut.ContinuousActions;

            // Heuristic for Discrete
            if (discreteActionsOut.Length >= 4)
            {
                discreteActionsOut.Clear();
                if (Input.GetKey(KeyCode.W)) discreteActionsOut[0] = 1;
                if (Input.GetKey(KeyCode.S)) discreteActionsOut[0] = 2;
                if (Input.GetKey(KeyCode.D)) discreteActionsOut[1] = 1;
                if (Input.GetKey(KeyCode.A)) discreteActionsOut[1] = 2;
                if (Input.GetKey(KeyCode.LeftArrow)) discreteActionsOut[2] = 1;
                if (Input.GetKey(KeyCode.RightArrow)) discreteActionsOut[2] = 2;
                if (Input.GetKey(KeyCode.UpArrow)) discreteActionsOut[3] = 1;
            }
            // Heuristic for Continuous
            else if (continuousActionsOut.Length >= 3)
            {
                continuousActionsOut[0] = Input.GetAxis("Vertical");
                continuousActionsOut[1] = Input.GetAxis("Horizontal");
                continuousActionsOut[2] = Input.GetKey(KeyCode.LeftArrow) ? -1f : (Input.GetKey(KeyCode.RightArrow) ? 1f : 0f);
                if (discreteActionsOut.Length > 0)
                {
                    discreteActionsOut[0] = Input.GetKey(KeyCode.UpArrow) ? 1 : 0;
                }
            }
        }
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        MoveAgent(actions);
        timePenalty -= m_Penalty;

        // --- NEW: Proximity Penalty & Aiming Reward (Anti-Stuck & Combat Logic) ---
        // Encourage agents to maintain distance from enemies and reward aiming
        foreach (var ps in area.playerStates)
        {
            if (ps.agentScript != null && ps.agentScript.health > 0 && 
                ps.agentScript.m_BehaviorParameters.TeamId != m_BehaviorParameters.TeamId)
            {
                float distance = Vector3.Distance(transform.position, ps.agentScript.transform.position);
                
                // 1. Proximity Penalty: Don't clump together
                if (distance < 2.0f) 
                {
                    AddReward(-0.002f); 
                }

                // 2. Aiming Reward: Reward facing the enemy
                Vector3 dirToEnemy = (ps.agentScript.transform.position - transform.position).normalized;
                float dot = Vector3.Dot(transform.forward, dirToEnemy);
                if (dot > 0.8f) // If looking roughly at the enemy
                {
                    AddReward(0.001f);
                }
            }
        }
    }

    public void FixedUpdate()
    {
        // Now updates UI regardless of training mode
        if (m_ShootSlider != null)
        {
            float timeBeforeShoot = (nextFire - Time.time);
            m_ShootSlider.value = timeBeforeShoot;
        }
    }
}
