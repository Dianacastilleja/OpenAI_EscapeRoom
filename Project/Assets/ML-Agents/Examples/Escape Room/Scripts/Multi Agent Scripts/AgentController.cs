using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class EscapeRoomAgent : Agent
{
    private Rigidbody rb;
    public GameObject pressurePlate1;
    public GameObject pressurePlate2;
    public GameObject endGoal;
    public GameObject door;

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

    private Door doorScript;
    private PressurePlate plateScript1;
    private PressurePlate plateScript2;

    public override void Initialize()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

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

        doorScript?.CloseDoor();
        plateScript1?.ResetPlate();
        plateScript2?.ResetPlate();

        RandomizePositions();

        lastDistanceToPlate1 = Vector3.Distance(transform.localPosition, pressurePlate1.transform.localPosition);
        lastDistanceToPlate2 = Vector3.Distance(transform.localPosition, pressurePlate2.transform.localPosition);
        lastDistanceToGoal = Vector3.Distance(transform.localPosition, endGoal.transform.localPosition);

        Debug.Log("EscapeRoomAgent: Episode initialized.");
    }

    private void RandomizePositions()
    {
        transform.localPosition = new Vector3(
            Random.Range(-4f, 14f),
            -0.75f,
            Random.Range(-4f, 14f)
        );
        pressurePlate1.transform.localPosition = new Vector3(
            Random.Range(-5.047f, 14.35f),
            -1.241f,
            Random.Range(-4.786f, 14.534f)
        );
        pressurePlate2.transform.localPosition = new Vector3(
            Random.Range(-5.047f, 14.35f),
            -1.241f,
            Random.Range(-4.786f, 14.534f)
        );

        Debug.Log("EscapeRoomAgent: Randomized positions for agent and pressure plates.");
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        float maxRange = 20f;

        sensor.AddObservation(transform.localPosition / maxRange);
        sensor.AddObservation(rb.velocity.x);
        sensor.AddObservation(rb.velocity.z);

        sensor.AddObservation((pressurePlate1.transform.localPosition - transform.localPosition) / maxRange);
        sensor.AddObservation((pressurePlate2.transform.localPosition - transform.localPosition) / maxRange);
        sensor.AddObservation(plateScript1.IsActivated ? 1.0f : 0.0f);
        sensor.AddObservation(plateScript2.IsActivated ? 1.0f : 0.0f);

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

        AddReward(-0.001f);

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

       // Debug.Log($"EscapeRoomAgent: Movement applied (MoveX: {moveX}, Turn: {turn}).");
    }

    private void HandlePenalties()
    {
        CheckForStuckAgent();

        float plate1Distance = Vector3.Distance(transform.localPosition, pressurePlate1.transform.localPosition);
        float plate2Distance = Vector3.Distance(transform.localPosition, pressurePlate2.transform.localPosition);

        if (plate1Distance < 1f && plate2Distance < 1f)
        {
            AddReward(-0.05f);
            Debug.Log("EscapeRoomAgent: Penalized for crowding the same plate.");
        }
    }

    private void HandleRewards()
    {
        float distanceToPlate1 = Vector3.Distance(transform.localPosition, pressurePlate1.transform.localPosition);
        float distanceToPlate2 = Vector3.Distance(transform.localPosition, pressurePlate2.transform.localPosition);

        if (gameObject.name.Contains("Red"))
        {
            float progress1 = lastDistanceToPlate1 - distanceToPlate1;
            AddReward(progress1 * 0.1f);
            lastDistanceToPlate1 = distanceToPlate1;

            if (plateScript2.IsActivated)
            {
                AddReward(-0.2f);
            }
        }
        else if (gameObject.name.Contains("Blue"))
        {
            float progress2 = lastDistanceToPlate2 - distanceToPlate2;
            AddReward(progress2 * 0.1f);
            lastDistanceToPlate2 = distanceToPlate2;

            if (plateScript1.IsActivated)
            {
                AddReward(-0.2f);
            }
        }

        if (plateScript1.IsActivated && plateScript2.IsActivated && !doorScript.IsOpen)
        {
            AddReward(5.0f);
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
                stuckChecks = 0;
            }
            lastPosition = transform.localPosition;
            stepsSinceLastMoveCheck = 0;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Wall"))
        {
            AddReward(-0.1f);
            Debug.Log("EscapeRoomAgent: Penalized for hitting a wall.");
        }
        else if (collision.gameObject.CompareTag("Agent"))
        {
            AddReward(-0.2f);
            Debug.Log("EscapeRoomAgent: Penalized for colliding with another agent.");
        }
    }

    public void ReachedEndGoal()
    {
        if (!goalReached)
        {
            goalReached = true;
            AddReward(10.0f);
            Debug.Log("EscapeRoomAgent: Goal reached.");
            EndEpisode();
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActions = actionsOut.ContinuousActions;
        continuousActions[0] = Input.GetAxis("Vertical");
        continuousActions[1] = Input.GetAxis("Horizontal");

        Debug.Log("EscapeRoomAgent: Heuristic actions applied.");
    }
}
