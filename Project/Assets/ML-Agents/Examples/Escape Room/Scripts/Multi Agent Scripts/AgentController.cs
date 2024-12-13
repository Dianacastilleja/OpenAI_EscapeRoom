using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using System.Collections.Generic;
using System.Collections;


public class EscapeRoomAgent : Agent
{
    private Rigidbody rb;
    public GameObject pressurePlate1;
    public GameObject pressurePlate2;
    public GameObject endGoal;
    public GameObject door;

    public GameObject lavaBlock1;
    public GameObject lavaBlock2;
    public GameObject lavaBlock3;
    public GameObject lavaBlock4;

    public float outOfBoundsThresholdY = -5f; // Y position threshold for falling out of the map
    public float stuckThreshold = 0.1f;
    public int stuckCheckFrequency = 100;
    public int stuckGracePeriod = 5;

    private int stepsSinceLastMoveCheck = 0;
    private int stuckChecks = 0;

    private Vector3 lastPosition;
    private float lastDistanceToGoal;
    private float lastDistanceToPlate1;
    private float lastDistanceToPlate2;

    private bool goalReached = false;

   /* public GameObject explosionEffect; // Assign a particle system prefab for explosion in the Inspector
    public AudioClip explosionSound;*/   // Assign the sound effect for the explosion in the Inspector
    public EnvironmentManager environmentManager; // Assign in the Inspector


    private Door doorScript;
    private PressurePlate plateScript1;
    private PressurePlate plateScript2;

    private const float spawnBuffer = 1.5f; // Minimum distance between objects


    public override void Initialize()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        if (environmentManager == null)
        {
            environmentManager = FindObjectOfType<EnvironmentManager>();
            if (environmentManager == null)
            {
                //Debug.LogError("EnvironmentManager not found in the scene. Please assign it in the Inspector.");
            }
        }

        doorScript = door.GetComponent<Door>();
        plateScript1 = pressurePlate1.GetComponent<PressurePlate>();
        plateScript2 = pressurePlate2.GetComponent<PressurePlate>();

        //Debug.Log("EscapeRoomAgent: Initialization complete.");
    }

    public override void OnEpisodeBegin()
    {
        EnableAgent(); // Ensure the agent is fully active and reset

        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        goalReached = false;

        stepsSinceLastMoveCheck = 0;
        stuckChecks = 0;

        doorScript?.CloseDoor();
        plateScript1?.ResetPlate();
        plateScript2?.ResetPlate();

        RandomizePositions();

        lastDistanceToPlate1 = Vector3.Distance(transform.localPosition, pressurePlate1.transform.localPosition);
        lastDistanceToPlate2 = Vector3.Distance(transform.localPosition, pressurePlate2.transform.localPosition);
        lastDistanceToGoal = Vector3.Distance(transform.localPosition, endGoal.transform.localPosition);

        //Debug.Log($"{gameObject.name}: Episode initialized.");
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        float maxRange = 20f; // Normalize observations within the environment scale.

        // Agent's position and velocity
        sensor.AddObservation(transform.localPosition / maxRange); // Agent position
        sensor.AddObservation(rb.velocity.x / maxRange); // Normalized velocity
        sensor.AddObservation(rb.velocity.z / maxRange); // Normalized velocity
        sensor.AddObservation(rb.angularVelocity.y / 10f); // Normalized angular velocity

        // Pressure Plates (positions and states)
        sensor.AddObservation((pressurePlate1.transform.localPosition - transform.localPosition) / maxRange);
        sensor.AddObservation((pressurePlate2.transform.localPosition - transform.localPosition) / maxRange);
        sensor.AddObservation(plateScript1.IsActivated ? 1.0f : 0.0f);
        sensor.AddObservation(plateScript2.IsActivated ? 1.0f : 0.0f);

        // End Goal position
        sensor.AddObservation((endGoal.transform.localPosition - transform.localPosition) / maxRange);

        // Door position and state
        sensor.AddObservation((door.transform.localPosition - transform.localPosition) / maxRange);
        sensor.AddObservation(doorScript.IsOpen ? 1.0f : 0.0f);

        // Lava blocks positions (only active blocks are included)
        foreach (GameObject lavaBlock in new[] { lavaBlock1, lavaBlock2, lavaBlock3, lavaBlock4 })
        {
            if (lavaBlock.activeSelf)
            {
                sensor.AddObservation((lavaBlock.transform.localPosition - transform.localPosition) / maxRange);
            }
        }

        // Debug for observation size (optional)
        // Debug.Log($"Observation Count: {sensor.ObservationSize()}");
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        HandleMovement(actions);
        HandlePenalties();
        HandleRewards();

        if (doorScript.IsOpen && Vector3.Distance(transform.localPosition, endGoal.transform.localPosition) < 1f)
        {
            ReachedEndGoal();
        }

        AddReward(-0.001f);

        if (StepCount >= MaxStep && !goalReached)
        {
            AddReward(-0.5f);
            //Debug.LogWarning("EscapeRoomAgent: Episode ended due to max steps.");
            EndEpisode();
        }
    }

    public void DisableAgent()
    {
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // Disable CCD before setting the Rigidbody to kinematic
        rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
        rb.isKinematic = true;
        rb.detectCollisions = false; // Disable physics interactions

        Collider agentCollider = GetComponent<Collider>();
        if (agentCollider != null)
        {
            agentCollider.enabled = false; // Disable the collider
        }

        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            renderer.enabled = false; // Disable visuals
        }

        // Debug.Log($"{gameObject.name} has been visually disabled.");
    }



    public void EnableAgent()
    {
        gameObject.SetActive(true);

        rb.isKinematic = false;
        rb.detectCollisions = true; // Re-enable collisions
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        Collider agentCollider = GetComponent<Collider>();
        if (agentCollider != null)
        {
            agentCollider.enabled = true; // Re-enable the collider
        }

        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            renderer.enabled = true; // Re-enable visuals
        }

        goalReached = false;
    }


    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActions = actionsOut.ContinuousActions;

        if (gameObject.name.Contains("Red"))
        {
            // Controls for Agent Red (WASD)
            continuousActions[0] = Input.GetKey(KeyCode.W) ? 1f : Input.GetKey(KeyCode.S) ? -1f : 0f; // Forward/Backward
            continuousActions[1] = Input.GetKey(KeyCode.D) ? 1f : Input.GetKey(KeyCode.A) ? -1f : 0f; // Right/Left
        }
        else if (gameObject.name.Contains("Blue"))
        {
            // Controls for Agent Blue (Arrow Keys)
            continuousActions[0] = Input.GetKey(KeyCode.UpArrow) ? 1f : Input.GetKey(KeyCode.DownArrow) ? -1f : 0f; // Forward/Backward
            continuousActions[1] = Input.GetKey(KeyCode.RightArrow) ? 1f : Input.GetKey(KeyCode.LeftArrow) ? -1f : 0f; // Right/Left
        }

        // Check if the "R" key is pressed to reset the episode
        if (Input.GetKeyDown(KeyCode.R))
        {
           // Debug.Log("Heuristic: 'R' key pressed. Resetting episode.");
            EndEpisode(); // Resets the current episode
        }
    }

    private void RandomizePositions()
    {
        List<Vector3> usedPositions = new List<Vector3>();

        // Randomize Agent Red position
        GameObject agentRed = GameObject.Find("agentRed");
        if (agentRed != null)
        {
            agentRed.transform.localPosition = GetValidSpawnPosition(
                new Vector3(-4.36f, -0.762f, -4.129f),
                new Vector3(14.83f, -0.762f, 14.71f),
                usedPositions,
                "Agent"
            );
           // Debug.Log($"Agent Red spawned at: {agentRed.transform.localPosition}");
        }

        // Randomize Agent Blue position
        GameObject agentBlue = GameObject.Find("agentBlue");
        if (agentBlue != null)
        {
            agentBlue.transform.localPosition = GetValidSpawnPosition(
                new Vector3(-4.36f, -0.762f, -4.129f),
                new Vector3(14.83f, -0.762f, 14.71f),
                usedPositions,
                "Agent"
            );
            //Debug.Log($"Agent Blue spawned at: {agentBlue.transform.localPosition}");
        }

        // Randomize Pressure Plate positions
        pressurePlate1.transform.localPosition = GetValidSpawnPosition(
            new Vector3(-5f, -1.241f, -4.55f),
            new Vector3(14.337f, -1.241f, 14.45f),
            usedPositions,
            "PressurePlate"
        );

        pressurePlate2.transform.localPosition = GetValidSpawnPosition(
            new Vector3(-5f, -1.241f, -4.55f),
            new Vector3(14.337f, -1.241f, 14.45f),
            usedPositions,
            "PressurePlate"
        );

       // Debug.Log($"PressurePlate1 spawned at: {pressurePlate1.transform.localPosition}");
       // Debug.Log($"PressurePlate2 spawned at: {pressurePlate2.transform.localPosition}");

        // Randomize Lava block positions
        lavaBlock1.transform.localPosition = GetValidSpawnPosition(
            new Vector3(-1.36f, -1.254f, -0.36f),
            new Vector3(12.09f, -1.254f, 14.28f),
            usedPositions,
            "Lava"
        );
        lavaBlock2.transform.localPosition = GetValidSpawnPosition(
            new Vector3(-1.29f, -1.254f, -0.53f),
            new Vector3(12.839f, -1.254f, 14.88f),
            usedPositions,
            "Lava"
        );
        lavaBlock3.transform.localPosition = GetValidSpawnPosition(
            new Vector3(-1.46f, -1.254f, 0.69f),
            new Vector3(10.92f, -1.254f, 14.03f),
            usedPositions,
            "Lava"
        );
        lavaBlock4.transform.localPosition = GetValidSpawnPosition(
            new Vector3(-1.41f, -1.254f, -0.41f),
            new Vector3(12.01f, -1.254f, 14.07f),
            usedPositions,
            "Lava"
        );

       // Debug.Log("Randomized all positions for agents, pressure plates, and lava blocks.");
    }

    private Vector3 GetValidSpawnPosition(Vector3 minBounds, Vector3 maxBounds, List<Vector3> usedPositions, string tag)
    {
        int attempts = 0;
        Vector3 randomPosition;

        do
        {
            // Generate a random position within the bounds
            randomPosition = new Vector3(
                Random.Range(minBounds.x, maxBounds.x),
                minBounds.y, // Fixed Y-coordinate for the object type
                Random.Range(minBounds.z, maxBounds.z)
            );

            attempts++;
        }
        while ((!IsPositionValid(randomPosition, usedPositions, tag) && attempts < 100));

        if (attempts >= 100)
        {
           // Debug.LogWarning($"Failed to find a valid spawn position for {tag} after 100 attempts.");
        }
        else
        {
            usedPositions.Add(randomPosition); // Track the valid position
        }

        return randomPosition;
    }

    private bool IsPositionValid(Vector3 position, List<Vector3> usedPositions, string tag)
    {
        float bufferRadius = spawnBuffer; // Use the defined spawn buffer as the radius

        // Adjust buffer based on object type (optional, replace with actual sizes if needed)
        if (tag == "Lava") bufferRadius = 2.5f; // Larger buffer for lava blocks

        // Perform a sphere overlap check to see if the position collides with anything
        Collider[] colliders = Physics.OverlapSphere(position, bufferRadius);

        foreach (Collider collider in colliders)
        {
            // Prevent overlap with any object of interest
            if (collider.CompareTag("Wall") || collider.CompareTag("Agent") || collider.CompareTag("Door") || collider.CompareTag("PressurePlate") || collider.CompareTag("Lava"))
            {
                return false; // Overlap detected with a prohibited object
            }
        }

        // Ensure the position doesn't overlap previously used positions
        foreach (Vector3 usedPosition in usedPositions)
        {
            if (Vector3.Distance(position, usedPosition) < bufferRadius)
            {
                return false; // Too close to an existing position
            }
        }

        return true; // Valid position
    }

    private void HandleMovement(ActionBuffers actions)
    {
        float accelerationInput = actions.ContinuousActions[0]; // Forward/backward acceleration
        float steeringInput = actions.ContinuousActions[1]; // Left/right steering

        // Default forces for movement
        float accelerationForce = 50f;
        float torqueForce = 25f;

        // Apply forward/backward acceleration
        rb.AddForce(transform.forward * accelerationInput * accelerationForce, ForceMode.Acceleration);

        // Apply turning torque
        rb.AddTorque(Vector3.up * steeringInput * torqueForce, ForceMode.Acceleration);

        // Optional: Damp angular velocity to prevent uncontrollable spinning
        float angularDamping = 1f; // Adjust between 0 (no damping) and 1 (full damping)
        rb.angularVelocity = rb.angularVelocity * angularDamping;

        // Limit velocity for better control
        float maxVelocity = 5f;
        rb.velocity = Vector3.ClampMagnitude(rb.velocity, maxVelocity);

        // Debug log for movement details
       // Debug.Log($"Movement applied - Acceleration: {accelerationInput}, Steering: {steeringInput}");
    }

    private void HandleRewards()
    {
        // Reward for activating the correct plate
        if (gameObject.name.Contains("Red") && plateScript1.IsActivated && !plateScript2.IsActivated)
        {
            AddReward(1.0f); // Intermediate reward for Red activating Plate 1
        }
        else if (gameObject.name.Contains("Blue") && plateScript2.IsActivated && !plateScript1.IsActivated)
        {
            AddReward(1.0f); // Intermediate reward for Blue activating Plate 2
        }

        // Penalty for activating the wrong plate
        if (gameObject.name.Contains("Red") && plateScript2.IsActivated && !plateScript1.IsActivated)
        {
            AddReward(-1.0f); // Penalty for Red activating Plate 2
        }
        else if (gameObject.name.Contains("Blue") && plateScript1.IsActivated && !plateScript2.IsActivated)
        {
            AddReward(-1.0f); // Penalty for Blue activating Plate 1
        }

        // Cooperative reward for activating both plates together
        if (plateScript1.IsActivated && plateScript2.IsActivated && !doorScript.IsOpen)
        {
            AddReward(5.0f); // High cooperative reward
            doorScript.OpenDoor();
        }

        // Reward for reaching the EndGoal after the door opens
        if (doorScript.IsOpen && Vector3.Distance(transform.localPosition, endGoal.transform.localPosition) < 1f)
        {
            AddReward(10.0f); // Significant reward for reaching the goal
            ReachedEndGoal();
        }
    }

    private void HandlePenalties()
    {
        // Penalty for colliding with walls or obstacles
        if (Vector3.Distance(transform.localPosition, door.transform.localPosition) < 0.5f)
        {
            AddReward(-0.2f); // Mild penalty for hitting walls
        }

        // Penalty for colliding with another agent
        float distanceToOtherAgent = Vector3.Distance(transform.localPosition,
            gameObject.name.Contains("Red") ? GameObject.Find("agentBlue").transform.localPosition : GameObject.Find("agentRed").transform.localPosition);
        if (distanceToOtherAgent < 1.0f)
        {
            AddReward(-0.2f); // Penalty for clustering or collisions
        }

        // Strong penalty for touching lava
        foreach (GameObject lavaBlock in new[] { lavaBlock1, lavaBlock2, lavaBlock3, lavaBlock4 })
        {
            if (lavaBlock.activeSelf && Vector3.Distance(transform.localPosition, lavaBlock.transform.localPosition) < 0.5f)
            {
                AddReward(-1.0f); // Strong penalty for touching lava
            }
        }

        // Penalty for idling or lack of movement
        stepsSinceLastMoveCheck++;
        if (stepsSinceLastMoveCheck >= stuckCheckFrequency)
        {
            if (Vector3.Distance(transform.localPosition, lastPosition) < stuckThreshold)
            {
                stuckChecks++;
                if (stuckChecks > stuckGracePeriod)
                {
                    AddReward(-0.5f); // Strong penalty for prolonged inactivity
                    EndEpisode(); // End episode if idling persists
                }
            }
            else
            {
                stuckChecks = 0; // Reset the stuck counter if movement is detected
            }
            lastPosition = transform.localPosition; // Update last position
            stepsSinceLastMoveCheck = 0; // Reset the move check counter
        }

        // Penalty for failing to activate any plate over time
        if (!plateScript1.IsActivated && !plateScript2.IsActivated)
        {
            AddReward(-0.01f * Time.deltaTime); // Continuous mild penalty for lack of progress
        }
    }

    private void Update()
    {
        CheckOutOfBounds();

        // Check if the "R" key is pressed
        if (Input.GetKeyDown(KeyCode.R))
        {
           // Debug.Log("Resetting episode using the 'R' key.");
            EndEpisode(); // Reset the current episode
        }
    }

    private void CheckOutOfBounds()
    {
        // Check if the agent's Y position is below the threshold
        if (transform.position.y < outOfBoundsThresholdY)
        {
          //  Debug.LogWarning($"{gameObject.name} fell out of the map! Ending episode.");
            AddReward(-1.0f); // Penalty for falling off the map
            EndEpisode();     // Reset the episode
        }
    }

    private void CheckForStuckAgent()
    {
        stepsSinceLastMoveCheck++;

        // Check periodically if the agent is stuck
        if (stepsSinceLastMoveCheck >= stuckCheckFrequency)
        {
            float velocityMagnitude = rb.velocity.magnitude;
            float angularVelocityMagnitude = rb.angularVelocity.magnitude;

            // Check if the agent is moving very slowly
            bool isPhysicallyStuck = velocityMagnitude < stuckThreshold && angularVelocityMagnitude < stuckThreshold;

            // Check if the agent is not making meaningful progress
            float distanceToPlate1 = Vector3.Distance(transform.localPosition, pressurePlate1.transform.localPosition);
            float distanceToPlate2 = Vector3.Distance(transform.localPosition, pressurePlate2.transform.localPosition);
            bool isNotProgressing =
                (gameObject.name.Contains("Red") && distanceToPlate1 >= lastDistanceToPlate1) ||
                (gameObject.name.Contains("Blue") && distanceToPlate2 >= lastDistanceToPlate2);

            if (isPhysicallyStuck || isNotProgressing)
            {
                stuckChecks++;

                if (stuckChecks > stuckGracePeriod)
                {
                    AddReward(-0.5f); // Penalty for being stuck or not progressing
                    EndEpisode();
                }
            }
            else
            {
                stuckChecks = 0; // Reset the stuck counter if the agent is moving or progressing
            }

            // Update position and distances for the next check
            lastPosition = transform.localPosition;
            if (gameObject.name.Contains("Red"))
            {
                lastDistanceToPlate1 = distanceToPlate1;
            }
            else if (gameObject.name.Contains("Blue"))
            {
                lastDistanceToPlate2 = distanceToPlate2;
            }

            stepsSinceLastMoveCheck = 0; // Reset step counter for the next check
        }
    }


    public void ReachedEndGoal()
    {
        if (!goalReached) // Check to prevent duplicate calls
        {
            goalReached = true;
            AddReward(10.0f); // Reward the agent for reaching the goal

            // Notify the EnvironmentManager that this agent has reached the goal
            if (environmentManager != null)
            {
                environmentManager.AgentReachedGoal(this);
            }

            // Disable agent visuals and physics
            DisableAgent();

            // Ensure the collider is disabled to prevent blocking
            Collider agentCollider = GetComponent<Collider>();
            if (agentCollider != null)
            {
                agentCollider.enabled = false; // Disable the collider
            }

            // Ensure Rigidbody interactions are disabled
            rb.isKinematic = true;
            rb.detectCollisions = false;

            // Check if all agents have reached their goal
            if (environmentManager != null && environmentManager.AreAllAgentsAtGoal())
            {
                environmentManager.ResetEnvironment(); // Reset the environment when all agents finish
            }
        }
    }


    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Wall"))
        {
            AddReward(-0.2f); // Penalty for hitting walls
          //  Debug.Log($"{gameObject.name}: Penalized for hitting a wall.");
        }

        if (collision.gameObject.CompareTag("Agent"))
        {
            AddReward(-0.2f); // Penalty for colliding with another agent
           // Debug.Log($"{gameObject.name}: Penalized for colliding with another agent.");
        }

        if (collision.gameObject.CompareTag("EndGoal"))
        {
           // Debug.Log($"{gameObject.name} has reached the EndGoal.");
            ReachedEndGoal();
        }

        if (collision.gameObject.CompareTag("Lava"))
        {
            AddReward(-1.0f); // Heavy penalty for touching lava
           // Debug.Log($"{gameObject.name}: Collided with lava. Triggering fail and resetting environment.");

           // BlowUpAgent(); // Trigger explosion effect

           // Notify the EnvironmentManager to reset the environment
            if (environmentManager != null)
            {
                environmentManager.ResetEnvironment();
            }
            else
            {
              //  Debug.LogError("EnvironmentManager is not assigned! Unable to reset the environment.");
            }
        }
    }

   /* private void BlowUpAgent()
    {
        // Instantiate the explosion effect at the agent's position
        if (explosionEffect != null)
        {
            GameObject explosion = Instantiate(explosionEffect, transform.position, Quaternion.identity);

            // Destroy the explosion effect after its duration
            ParticleSystem ps = explosion.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                Destroy(explosion, ps.main.duration + ps.main.startLifetime.constantMax);
            }
            else
            {
                Destroy(explosion, 1.5f); // Fallback in case no ParticleSystem is found
            }
        }

        // Play the explosion sound
        if (explosionSound != null)
        {
            AudioSource.PlayClipAtPoint(explosionSound, transform.position);
        }

        // Disable CCD before making the Rigidbody kinematic
        if (rb != null)
        {
            rb.collisionDetectionMode = CollisionDetectionMode.Discrete; // Disable CCD
            rb.isKinematic = true;  // Disable physics interactions
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // Hide the agent visually (check for Renderer components)
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            foreach (Renderer renderer in renderers)
            {
                renderer.enabled = false;  // Visually hide the agent
            }
        }
        else
        {
           // Debug.LogWarning($"{gameObject.name} does not have any Renderer components to hide.");
        }

      //  Debug.Log($"{gameObject.name} exploded and is visually hidden.");
    }

    private void ReactivateAgent()
    {
        gameObject.SetActive(true);
    }*/
}
