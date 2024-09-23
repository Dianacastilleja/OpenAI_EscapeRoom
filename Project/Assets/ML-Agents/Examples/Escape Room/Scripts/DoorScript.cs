using System.Collections;  // For IEnumerator
using System.Collections.Generic;  // For collections like List
using UnityEngine;  // For MonoBehaviour, Vector3, etc.

public class Door : MonoBehaviour
{
    private Vector3 openPosition;
    private Vector3 closedPosition;

    public float speed = 2f;  // Speed of the door opening/closing

    private void Start()
    {
        closedPosition = transform.position;  // Save the initial position as closed position
        openPosition = transform.position + new Vector3(0, 3f, 0);  // Set open position (e.g., 3 units upward)
    }

    public void OpenDoor()
    {
        StopAllCoroutines();
        StartCoroutine(MoveDoor(openPosition));
    }

    public void CloseDoor()
    {
        StopAllCoroutines();
        StartCoroutine(MoveDoor(closedPosition));
    }

    private IEnumerator MoveDoor(Vector3 targetPosition)
    {
        while (Vector3.Distance(transform.position, targetPosition) > 0.01f)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime);
            yield return null;
        }
        transform.position = targetPosition;
    }
}
