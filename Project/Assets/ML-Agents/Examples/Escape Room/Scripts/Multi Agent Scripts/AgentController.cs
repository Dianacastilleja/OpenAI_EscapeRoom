using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class EscapeRoomAgent : Agent
{
    private Rigidbody rb;
    public GameObject pressurePlate1; // Reference to the first pressure plate
    public GameObject pressurePlate2; // Reference to the second pressure plate
    public GameObject endGoal;
    public GameObject door;

    public float stuckThreshold = 0.1f; // Threshold for detecting if the agent is stuck
    public int stuckCheckFrequency = 100; // Number of steps before checking for stuck
    public int stuckGracePeriod = 5; // Number of consecutive stuck checks allowed before penalizing

    private int stepsSinceLastMoveCheck = 0; // Tracks steps for the next stuck check
    private int stuckChecks = 0; // Tracks consecutive stuck checks

    private Vector3 lastPosition; // Tracks the agent's last position
    private float lastDistanceToGoal;
    private float lastDistanceToPlate1;
    private float lastDistanceToPlate2;

    private bool goalReached = false;

    private Door doorScript;
    private PressurePlate plateScript1;
    private PressurePlate plateScript2;

    public override void Initialize()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        // Cache scripts for door and plates
        doorScript = door.GetComponent<Door>();
        plateScript1 = pressurePlate1.GetComponent<PressurePlate>();
        plateScript2 = pressurePlate2.GetComponent<PressurePlate>();

        Debug.Log("EscapeRoomAgent: Initialization complete.");
    }

    public override void OnEpisodeBegin()
    {
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        goalReached = false;
        stepsSinceLastMoveCheck = 0;
        stuckChecks = 0;

        // Reset door and pressure plates
        doorScript?.CloseDoor();
        plateScript1?.ResetPlate();
        plateScript2?.ResetPlate();

        RandomizePositions();

        // Cache initial distances
        lastDistanceToPlate1 = Vector3.Distance(transform.localPosition, pressurePlate1.transform.localPosition);
        lastDistanceToPlate2 = Vector3.Distance(transform.localPosition, pressurePlate2.transform.localPosition);
        lastDistanceToGoal = Vector3.Distance(transform.localPosition, endGoal.transform.localPosition);

        Debug.Log("EscapeRoomAgent: Episode initialized.");
    }

    private void RandomizePositions()
    {
        // Randomize agent and pressure plate positions within the specified bounds
        transform.localPosition = new Vector3(
            Random.Range(-4f, 14f),
            -0.75f,
            Random.Range(-4f, 14f)
        );
        pressurePlate1.transform.localPosition = new Vector3(
            Random.Range(-5.047f, 14.35f), // X-axis range
            -1.241f,                       // Fixed Y-axis position
            Random.Range(-4.786f, 14.534f) // Z-axis range
        );
        pressurePlate2.transform.localPosition = new Vector3(
            Random.Range(-5.047f, 14.35f), // X-axis range
            -1.241f,                       // Fixed Y-axis position
            Random.Range(-4.786f, 14.534f) // Z-axis range
        );

        Debug.Log("EscapeRoomAgent: Randomized positions for agent and pressure plates.");
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        float maxRange = 20f;

        // Agent's position and velocity
        sensor.AddObservation(transform.localPosition / maxRange);
        sensor.AddObservation(rb.velocity.x);
        sensor.AddObservation(rb.velocity.z);

        // Pressure plates
        sensor.AddObservation((pressurePlate1.transform.localPosition - transform.localPosition) / maxRange);
        sensor.AddObservation((pressurePlate2.transform.localPosition - transform.localPosition) / maxRange);
        sensor.AddObservation(plateScript1.IsActivated ? 1.0f : 0.0f);
        sensor.AddObservation(plateScript2.IsActivated ? 1.0f : 0.0f);

        // Door and goal
        sensor.AddObservation(doorScript.IsOpen ? 1.0f : 0.0f);
        sensor.AddObservation((endGoal.transform.localPosition - transform.localPosition) / maxRange);

        Debug.Log("EscapeRoomAgent: Observations collected.");
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

        AddReward(-0.001f); // Small time penalty

        if (StepCount >= MaxStep && !goalReached)
        {
            AddReward(-0.5f);
            Debug.LogWarning("EscapeRoomAgent: Episode ended due to max steps.");
            EndEpisode();
        }
    }

    private void HandleMovement(ActionBuffers actions)
    {
        float moveX = actions.ContinuousActions[0];
        float turn = actions.ContinuousActions[1];

        float moveSpeed = 5f;
        float rotationSpeed = 100f;

        Vector3 movement = transform.forward * moveX * Time.fixedDeltaTime * moveSpeed;
        rb.MovePosition(rb.position + movement);

        Quaternion rotation = Quaternion.Euler(0f, turn * Time.fixedDeltaTime * rotationSpeed, 0f);
        rb.MoveRotation(rb.rotation * rotation);

        Debug.Log($"EscapeRoomAgent: Movement applied (MoveX: {moveX}, Turn: {turn}).");
    }

    private void HandlePenalties()
    {
        CheckForStuckAgent();
    }

    private void HandleRewards()
    {
        // Distance-based reward for moving closer to respective pressure plates
        float distanceToPlate1 = Vector3.Distance(transform.localPosition, pressurePlate1.transform.localPosition);
        float distanceToPlate2 = Vector3.Distance(transform.localPosition, pressurePlate2.transform.localPosition);

        // Assign preferred plates (Agent Red prefers Plate 1, Agent Blue prefers Plate 2)
        if (gameObject.name.Contains("Red"))
        {
            float progress1 = lastDistanceToPlate1 - distanceToPlate1;
            AddReward(progress1 * 0.1f);
            lastDistanceToPlate1 = distanceToPlate1;

            if (plateScript2.IsActivated)
            {
                AddReward(-0.2f); // Penalize for approaching already activated Plate 2
            }
        }
        else if (gameObject.name.Contains("Blue"))
        {
            float progress2 = lastDistanceToPlate2 - distanceToPlate2;
            AddReward(progress2 * 0.1f);
            lastDistanceToPlate2 = distanceToPlate2;

            if (plateScript1.IsActivated)
            {
                AddReward(-0.2f); // Penalize for approaching already activated Plate 1
            }
        }

        // Cooperative reward for activating both plates simultaneously
        if (plateScript1.IsActivated && plateScript2.IsActivated && !doorScript.IsOpen)
        {
            AddReward(5.0f); // Large cooperative reward for opening the door
            doorScript.OpenDoor();
            Debug.Log("Both plates activated simultaneously. Door opened!");
        }
    }

    private void CheckForStuckAgent()
    {
        stepsSinceLastMoveCheck++;
        if (stepsSinceLastMoveCheck >= stuckCheckFrequency)
        {
            if (Vector3.Distance(transform.localPosition, lastPosition) < stuckThreshold)
            {
                stuckChecks++;
                if (stuckChecks > stuckGracePeriod)
                {
                    AddReward(-0.1f);
                    Debug.LogWarning("EscapeRoomAgent: Stuck penalty applied. Ending episode.");
                    EndEpisode();
                }
            }
            else
            {
                stuckChecks = 0; // Reset if the agent moved
            }
            lastPosition = transform.localPosition; // Update position
            stepsSinceLastMoveCheck = 0; // Reset step counter
        }
    }

    public void ReachedEndGoal()
    {
        if (!goalReached)
        {
            goalReached = true;
            AddReward(10.0f); // Large reward for completing the task
            Debug.Log("EscapeRoomAgent: Goal reached.");
            EndEpisode();
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        // Manual testing with keyboard input
        var continuousActions = actionsOut.ContinuousActions;
        continuousActions[0] = Input.GetAxis("Vertical");   // Forward/backward movement
        continuousActions[1] = Input.GetAxis("Horizontal"); // Turning left/right

        Debug.Log("EscapeRoomAgent: Heuristic actions applied.");
    }
}
