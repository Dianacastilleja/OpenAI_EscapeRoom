using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class EscapeRoomAgent : Agent
{
    private Rigidbody rb;
    public GameObject pressurePlate; // Reference to the PressurePlate object
    public GameObject endGoal;       // Reference to the EndGoal object
    public GameObject door;          // Reference to the Door object
    public float spawnRange = 8f;
    public float stuckThreshold = 0.05f;  // Threshold for detecting if agent is stuck
    public int stuckCheckFrequency = 100; // Frequency of checking if agent is stuck
    public float proximityPenaltyThreshold = 3.0f; // Time threshold for applying penalty for hugging the door
    public float distanceRewardMultiplier = 1.0f; // Multiplier for rewarding agent for moving towards the end goal when door is open

    private bool platePressed = false; // To track if plate was pressed
    private bool goalReached = false;  // To track if goal was reached
    private bool doorIsOpen = false;   // To track if the door has been opened
    private float totalReward;         // Track total reward during the episode

    private Vector3 lastPosition; // For checking if the agent is stuck
    private int stepsSinceLastMoveCheck = 0; // For controlling when to check for being stuck
    private float proximityTimer = 0f; // To track time spent near the door

    public override void Initialize()
    {
        rb = GetComponent<Rigidbody>();

        // Freeze rotation on X and Z axes to prevent flipping
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        lastPosition = transform.localPosition; // Initialize lastPosition to the starting position
    }

    public override void OnEpisodeBegin()
    {
        // Reset agent's velocity
        rb.velocity = Vector3.zero;

        // Reset task progress and reward
        platePressed = false;
        goalReached = false;
        doorIsOpen = false;
        totalReward = 0f; // Reset the total reward at the start of the episode
        proximityTimer = 0f; // Reset the proximity timer

        // Call the ResetPlate method on the PressurePlate component
        pressurePlate.GetComponent<PressurePlate>().ResetPlate();

        // Randomize agent position
        float randomX = Random.Range(-4f, 14.0f);
        float randomZ = Random.Range(-4f, 14.0f);
        transform.localPosition = new Vector3(randomX, 0.2f, randomZ);

        // Randomize pressure plate position within defined bounds
        float plateRandomX = Random.Range(-6f, 13.0f);  // Adjust bounds based on level design
        float plateRandomZ = Random.Range(-18f, 1.0f);  // Adjust bounds based on level design
        pressurePlate.transform.localPosition = new Vector3(plateRandomX, 0f, plateRandomZ);  // Keep Y at 0f for floor level

        // Log the start of a new episode
        Debug.Log("New episode started. Agent and objects repositioned.");
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // Collect agent's position
        sensor.AddObservation(transform.localPosition);

        // Collect pressure plate's relative position
        sensor.AddObservation(pressurePlate.transform.localPosition - transform.localPosition);

        // Collect end goal's relative position
        sensor.AddObservation(endGoal.transform.localPosition - transform.localPosition);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        // Check if the agent has fallen out of the map
        if (transform.localPosition.y < -5f) // Adjust threshold based on map design
        {
            Debug.Log("Agent fell out of the map at position: " + transform.localPosition);
            SetReward(-5.0f);  // Significant penalty for falling off the map
            LogRewardChange(-5.0f);
            EndEpisode();
        }

        // Movement logic
        float moveX = actions.ContinuousActions[0]; // Forward/Backward
        float turn = actions.ContinuousActions[1];  // Left/Right

        // Apply movement and rotation to the agent
        if (Mathf.Abs(moveX) > 0.01f)
        {
            // Move the agent forward or backward
            transform.localPosition += transform.forward * moveX * Time.deltaTime * 10f; // Adjust speed multiplier as needed
        }

        if (Mathf.Abs(turn) > 0.01f)
        {
            // Rotate the agent
            transform.Rotate(new Vector3(0, turn * Time.deltaTime * 300f, 0)); // Adjust rotation speed as needed
        }

        // Penalize unnecessary turning (when turning but not moving forward much)
        if (Mathf.Abs(turn) > 0.5f && Mathf.Abs(moveX) < 0.1f)  // If agent turns sharply but isn't moving much
        {
            SetReward(-0.2f);  // Penalty for unnecessary turning
            LogRewardChange(-0.2f);
            Debug.Log("Penalty for unnecessary turning: -0.2");
        }

        // Check if the agent is stuck (hasn't moved much for a while)
        stepsSinceLastMoveCheck++;
        if (stepsSinceLastMoveCheck >= stuckCheckFrequency)
        {
            if (Vector3.Distance(transform.localPosition, lastPosition) < stuckThreshold)
            {
                SetReward(-0.5f);  // Small penalty for being stuck
                LogRewardChange(-0.5f);
                Debug.Log("Agent stuck. Penalty for being stuck: -0.5");
            }
            lastPosition = transform.localPosition;  // Update last known position
            stepsSinceLastMoveCheck = 0; // Reset the move check counter
        }

        // Check if agent is too close to the door for too long
        if (Vector3.Distance(transform.localPosition, door.transform.localPosition) < 1.5f && !doorIsOpen)
        {
            proximityTimer += Time.deltaTime;

            if (proximityTimer > proximityPenaltyThreshold)
            {
                SetReward(-1.0f); // Penalty for staying near the door too long when it's not open
                LogRewardChange(-1.0f);
                Debug.Log("Penalty for hugging the door: -1.0");
            }
        }
        else
        {
            proximityTimer = 0f; // Reset the timer if agent moves away from the door
        }

        // Check if agent pressed the plate (you can also use TriggerEnter for more accuracy)
        if (!platePressed && Vector3.Distance(transform.localPosition, pressurePlate.transform.localPosition) < 1f)
        {
            platePressed = true;  // Ensure the reward is given only once per episode
            SetReward(5.0f); // Reward for pressing the plate
            LogRewardChange(5.0f);
            door.GetComponent<Door>().OpenDoor(); // Ensure the door opens
            doorIsOpen = true;  // Mark that the door is now open
            Debug.Log("Pressure plate pressed. Reward given: 5.0");
        }

        // Apply distance-based reward to the end goal only if the door is open
        if (doorIsOpen && !goalReached)
        {
            float distanceToGoal = Vector3.Distance(transform.localPosition, endGoal.transform.localPosition);
            float distanceReward = 0.1f / distanceToGoal;  // Increase reward as the agent gets closer to the goal
            SetReward(distanceReward * distanceRewardMultiplier);  // Scale the reward with a multiplier
            LogRewardChange(distanceReward * distanceRewardMultiplier);
            Debug.Log($"Distance-based reward to the goal: {distanceReward * distanceRewardMultiplier}");
        }

        // Check if agent reached the end goal
        if (!goalReached && Vector3.Distance(transform.localPosition, endGoal.transform.localPosition) < 1f)
        {
            goalReached = true;
            SetReward(10.0f); // Big reward for reaching the goal
            LogRewardChange(10.0f);
            Debug.Log("End goal reached. Big reward given: 10.0");

            // Track and log the agent's last position before ending the episode
            lastPosition = transform.localPosition;
            Debug.Log("Last position before episode ended: " + lastPosition);

            EndEpisode();    // End the episode after reaching the goal
        }
    }

    // Detect collision with a wall (assume walls are tagged as "Wall")
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.CompareTag("Wall"))
        {
            SetReward(-1.0f); // Negative reward for hitting a wall
            LogRewardChange(-1.0f);
            Debug.Log("Collision with wall. Penalty given: -1.0. Ending episode.");

            // Track and log the agent's last position before ending the episode
            lastPosition = transform.localPosition;
            Debug.Log("Last position before episode ended: " + lastPosition);

            EndEpisode();     // End the episode
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        // For manual testing using keyboard input
        var continuousActions = actionsOut.ContinuousActions;
        continuousActions[0] = Input.GetAxis("Vertical");
        continuousActions[1] = Input.GetAxis("Horizontal");
    }

    // Method to log the reward change and update totalReward
    private void LogRewardChange(float rewardChange)
    {
        totalReward += rewardChange;
        Debug.Log("Reward Change: " + rewardChange + ", Total Reward: " + totalReward);
    }
}
