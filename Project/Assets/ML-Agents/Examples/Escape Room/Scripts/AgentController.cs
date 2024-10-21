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
    public float stuckThreshold = 0.1f;  // Threshold for detecting if agent is stuck
    public int stuckCheckFrequency = 100; // Frequency of checking if agent is stuck
    public int stuckGracePeriod = 5;      // Grace period before applying penalty
    public float proximityPenaltyThreshold = 3.0f; // Time threshold for applying penalty for hugging the door
    public float distanceRewardMultiplier = 1.0f;  // Multiplier for rewarding agent for moving towards the end goal when door is open

    private bool platePressed = false; // To track if plate was pressed
    private bool goalReached = false;  // To track if goal was reached
    private bool doorIsOpen = false;   // To track if the door has been opened
    private float totalReward;         // Track total reward during the episode

    private Vector3 lastPosition;      // For checking if the agent is stuck
    private int stepsSinceLastMoveCheck = 0; // For controlling when to check for being stuck
    private int stuckChecks = 0;       // Count of stuck checks
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
        rb.velocity = Vector3.zero;  // Reset agent's velocity
        rb.angularVelocity = Vector3.zero; // Reset angular velocity
        platePressed = false;
        goalReached = false;
        doorIsOpen = false;
        totalReward = 0f;  // Reset total reward at the start of the episode
        proximityTimer = 0f;
        stuckChecks = 0;   // Reset stuck checks

        // Reset the pressure plate and door
        pressurePlate.GetComponent<PressurePlate>().ResetPlate();
        door.GetComponent<Door>().CloseDoor();

        RandomizePositions();  // Randomize agent and pressure plate positions
    }

    private void RandomizePositions()
    {
        float randomX = Random.Range(-4f, 14.0f);
        float randomZ = Random.Range(-4f, 14.0f);
        transform.localPosition = new Vector3(randomX, -0.75f, randomZ);

        float plateRandomX = Random.Range(-6f, 13.0f);
        float plateRandomZ = Random.Range(-18f, 1.0f);
        pressurePlate.transform.localPosition = new Vector3(plateRandomX, 0f, plateRandomZ);
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        float maxRange = 20f; // Assuming environment boundaries are from -20 to 20 on each axis

        // Normalize agent's local position
        Vector3 normalizedAgentPos = transform.localPosition / maxRange;
        sensor.AddObservation(normalizedAgentPos);

        // Include agent's Y rotation (yaw), normalized to [-1, 1]
        float yaw = transform.eulerAngles.y;
        float normalizedYaw = Mathf.Repeat(yaw, 360f) / 180f - 1f;
        sensor.AddObservation(normalizedYaw);

        // Normalize relative positions
        Vector3 relativePlatePos = (pressurePlate.transform.localPosition - transform.localPosition) / maxRange;
        sensor.AddObservation(relativePlatePos);

        Vector3 relativeEndGoalPos = (endGoal.transform.localPosition - transform.localPosition) / maxRange;
        sensor.AddObservation(relativeEndGoalPos);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        HandleMovement(actions);  // Handle movement and rotation
        HandlePenalties();        // Handle different penalties (stuck, proximity to door)
        HandleRewards();          // Handle rewards for reaching goals

        // Apply penalty if MaxStep is reached without reaching the goal
        if (StepCount >= MaxStep && !goalReached)
        {
            AddReward(-0.5f); // Penalty for not reaching the goal
            Debug.Log("Failed to reach the goal within MaxStep. Penalty applied: -0.5");
            EndEpisode();
        }
    }

    private void HandleMovement(ActionBuffers actions)
    {
        float moveX = actions.ContinuousActions[0];
        float turn = actions.ContinuousActions[1];

        float moveSpeed = 5f;        // Reduced from 10f
        float rotationSpeed = 100f;  // Reduced from 300f

        if (Mathf.Abs(moveX) > 0.01f)
        {
            Vector3 movement = transform.forward * moveX * Time.fixedDeltaTime * moveSpeed;
            rb.MovePosition(rb.position + movement);
        }

        if (Mathf.Abs(turn) > 0.01f)
        {
            Quaternion rotation = Quaternion.Euler(new Vector3(0f, turn * Time.fixedDeltaTime * rotationSpeed, 0f));
            rb.MoveRotation(rb.rotation * rotation);
        }
    }

    private void HandlePenalties()
    {
        CheckForStuckAgent();      // Check if the agent is stuck
        CheckProximityToDoor();    // Check if the agent is hugging the door
    }

    private void CheckProximityToDoor()
    {
        if (Vector3.Distance(transform.localPosition, door.transform.localPosition) < 1.5f && !doorIsOpen)
        {
            proximityTimer += Time.deltaTime;
            if (proximityTimer > proximityPenaltyThreshold)
            {
                AddReward(-0.1f); // Reduced penalty magnitude
                Debug.Log("Penalty for hugging the door: -0.1");
                proximityTimer = 0f; // Reset timer after penalty
            }
        }
        else
        {
            proximityTimer = 0f;
        }
    }

    private void CheckForStuckAgent()
    {
        stepsSinceLastMoveCheck++;
        if (stepsSinceLastMoveCheck >= stuckCheckFrequency)
        {
            float distanceMoved = Vector3.Distance(transform.localPosition, lastPosition);
            if (distanceMoved < stuckThreshold)
            {
                stuckChecks++;

                if (stuckChecks > stuckGracePeriod)
                {
                    AddReward(-0.1f); // Reduced penalty magnitude
                    LogRewardChange(-0.1f);
                    Debug.Log("Agent stuck. Penalty applied: -0.1");
                    stuckChecks = 0; // Reset stuck checks after penalty
                }
            }
            else
            {
                stuckChecks = 0; // Reset if the agent moves
            }

            lastPosition = transform.localPosition;
            stepsSinceLastMoveCheck = 0;
        }
    }

    private void HandleRewards()
    {
        // Reward for moving closer to the pressure plate
        if (!platePressed)
        {
            float distanceToPlate = Vector3.Distance(transform.localPosition, pressurePlate.transform.localPosition);
            float normalizedDistance = distanceToPlate / 20f; // Normalize based on maximum possible distance
            float distanceReward = (1.0f - normalizedDistance) * 0.01f; // Small incremental reward
            AddReward(distanceReward);
        }

        // Reward for pressing the plate
        if (!platePressed && Vector3.Distance(transform.localPosition, pressurePlate.transform.localPosition) < 1f)
        {
            platePressed = true;
            AddReward(0.5f); // Adjusted reward magnitude
            pressurePlate.GetComponent<PressurePlate>().ActivatePlate(); // Activate the pressure plate
            door.GetComponent<Door>().OpenDoor();
            doorIsOpen = true;
            Debug.Log("Pressure plate pressed. Reward given: 0.5");
        }

        // Reward for moving closer to the end goal
        if (doorIsOpen && !goalReached)
        {
            float distanceToGoal = Vector3.Distance(transform.localPosition, endGoal.transform.localPosition);
            float normalizedDistance = distanceToGoal / 20f;
            float distanceReward = (1.0f - normalizedDistance) * 0.01f;
            AddReward(distanceReward);
        }
    }

    // Method called when the agent reaches the end goal
    public void ReachedEndGoal()
    {
        if (!goalReached)
        {
            goalReached = true;
            AddReward(2.0f); // Increased reward magnitude
            LogRewardChange(2.0f);
            Debug.Log("End goal reached. Reward given: 2.0");
            EndEpisode(); // End the episode
        }
    }

    // Detect collision with a wall (assume walls are tagged as "Wall")
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.CompareTag("Wall"))
        {
            AddReward(-0.2f); // Reduced penalty
            LogRewardChange(-0.2f);
            Debug.Log("Collision with wall. Penalty given: -0.2. Not ending episode.");
            // Do not end the episode to allow the agent to recover
        }
    }

    // Detect trigger entry (for the end goal)
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("EndGoal"))
        {
            Debug.Log("Agent entered EndGoal trigger.");
            ReachedEndGoal();
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActions = actionsOut.ContinuousActions;
        continuousActions[0] = Input.GetAxis("Vertical");
        continuousActions[1] = Input.GetAxis("Horizontal");
    }

    private void LogRewardChange(float rewardChange)
    {
        totalReward += rewardChange;
        Debug.Log("Reward Change: " + rewardChange + ", Total Reward: " + totalReward);
    }
}
