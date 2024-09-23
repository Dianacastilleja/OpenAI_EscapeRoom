using UnityEngine;
using UnityEngine.SceneManagement;  // For scene management

public class EndGoal : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        // Check if the object entering the trigger is the player (assuming tagged as "Player")
        if (other.CompareTag("Agent"))
        {
            // Print to the console
            Debug.Log("EndGoal Reached!");

            // Restart the current scene
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
}
