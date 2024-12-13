using UnityEngine;

public class Door : MonoBehaviour
{
    public Vector3 openPosition;   // The local position when the door is open
    public Vector3 closedPosition; // The local position when the door is closed

    private bool isOpen = false;   // Tracks whether the door is open or closed

    public bool IsOpen
    {
        get { return isOpen; }
    }

    private void Start()
    {
        // Ensure the door starts in the closed position
        CloseDoor();
       // Debug.Log("Door: Initialized and set to closed position.");
    }

    // Opens the door by moving it to the open position
    public void OpenDoor()
    {
        if (!isOpen)
        {
            isOpen = true;
            transform.localPosition = openPosition; // Move to open position
          //  Debug.Log($"Door: Opened at position {transform.localPosition}.");
        }
        else
        {
           // Debug.LogWarning("Door: Attempted to open an already open door.");
        }
    }

    // Closes the door by moving it to the closed position
    public void CloseDoor()
    {
        if (isOpen)
        {
            isOpen = false;
            transform.localPosition = closedPosition; // Move to closed position
          //  Debug.Log($"Door: Closed at position {transform.localPosition}.");
        }
        else
        {
          //  Debug.LogWarning("Door: Attempted to close an already closed door.");
        }
    }

    private void OnEnable()
    {
      //  Debug.Log("Door: Enabled in the scene.");
    }

    private void OnDisable()
    {
      //  Debug.LogWarning("Door: Deactivated.");
    }
}
