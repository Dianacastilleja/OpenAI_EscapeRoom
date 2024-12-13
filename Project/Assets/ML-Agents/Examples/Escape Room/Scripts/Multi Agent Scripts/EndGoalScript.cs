using UnityEngine;

public class EndGoal : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Agent") && other.gameObject.activeSelf) // Only active agents
        {
            EscapeRoomAgent agent = other.GetComponent<EscapeRoomAgent>();
            if (agent != null)
            {
                agent.ReachedEndGoal();
            }
            else
            {
                Debug.LogError($"EndGoal: No EscapeRoomAgent component found on {other.gameObject.name}!");
            }
        }
        else
        {
            Debug.LogWarning($"EndGoal: Ignoring {other.gameObject.name}, not an active agent.");
        }
    }


}
