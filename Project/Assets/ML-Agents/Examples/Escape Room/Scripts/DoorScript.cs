using System.Collections;  // Needed for IEnumerator
using UnityEngine;  // Needed for MonoBehaviour, Vector3, and other Unity components

public class Door : MonoBehaviour
{
    public Vector3 openPosition;  // The position the door moves to when it opens
    public Vector3 closedPosition;  // The original position of the door
    public float openSpeed = 5f;  // Speed at which the door opens/closes

    private bool isOpen = false;  // Track if the door is open or closed

    private void Start()
    {
        // Ensure the door starts in the correct position
        Debug.Log("Door starting position: " + transform.localPosition);
    }

    // Open the door
    public void OpenDoor()
    {
        if (!isOpen) // Only open the door if it's not already open
        {
            isOpen = true;
            Debug.Log("Opening door. Moving to position: " + openPosition);
            StopAllCoroutines();  // Stop any ongoing animations
            StartCoroutine(MoveDoor(openPosition));
        }
    }

    // Close the door
    public void CloseDoor()
    {
        if (isOpen) // Only close the door if it's open
        {
            isOpen = false;
            Debug.Log("Closing door. Moving to position: " + closedPosition);
            StopAllCoroutines();  // Stop any ongoing animations
            StartCoroutine(MoveDoor(closedPosition));
        }
    }

    // Move the door over time to its target position
    private IEnumerator MoveDoor(Vector3 targetPosition)
    {
        while (Vector3.Distance(transform.localPosition, targetPosition) > 0.01f)
        {
            transform.localPosition = Vector3.MoveTowards(transform.localPosition, targetPosition, openSpeed * Time.deltaTime);
            yield return null;  // Wait for the next frame
        }

        // Ensure the door snaps exactly to the target position
        transform.localPosition = targetPosition;
        Debug.Log("Door reached position: " + transform.localPosition);
    }
}
