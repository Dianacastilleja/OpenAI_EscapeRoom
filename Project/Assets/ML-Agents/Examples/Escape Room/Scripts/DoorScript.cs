using UnityEngine;

public class Door : MonoBehaviour
{
    public Vector3 openPosition;  // The position the door moves to when it opens
    public Vector3 closedPosition;  // The position the door moves to when it's closed

    private bool isOpen = false;

    private void Start()
    {
        // Explicitly set the closed position at the start
        transform.localPosition = closedPosition;
        Debug.Log("Door initialized at closed position: " + transform.localPosition);
    }

    // Open the door
    public void OpenDoor()
    {
        if (!isOpen)
        {
            isOpen = true;
            transform.localPosition = openPosition;  // Snap to open position
            Debug.Log("Door opened at position: " + transform.localPosition);
        }
    }

    // Close the door
    public void CloseDoor()
    {
        if (isOpen)
        {
            isOpen = false;
            transform.localPosition = closedPosition;  // Snap to closed position
            Debug.Log("Door closed at position: " + transform.localPosition);
        }
    }

    void OnEnable()
    {
        Debug.Log("Door is enabled");
    }

    void OnDisable()
    {
        Debug.Log("Warning: Door has been deactivated!");
    }
}
