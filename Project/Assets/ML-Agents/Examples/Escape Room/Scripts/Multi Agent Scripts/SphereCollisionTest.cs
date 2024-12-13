using UnityEngine;

public class SphereCollisionTest : MonoBehaviour
{
    public float bounceForce = 5f; // The force applied to bounce the sphere away

    private void OnCollisionEnter(Collision collision)
    {
        // Check if the collided object is tagged "Lava"
        if (collision.gameObject.CompareTag("Lava"))
        {
            // Log the interaction to the console
            Debug.Log("Sphere touched LavaBlock1");

            // Get the direction to bounce away
            Vector3 bounceDirection = transform.position - collision.transform.position;
            bounceDirection = bounceDirection.normalized; // Normalize the direction

            // Apply a force to the sphere to make it bounce away
            Rigidbody sphereRigidbody = GetComponent<Rigidbody>();
            if (sphereRigidbody != null)
            {
                sphereRigidbody.AddForce(bounceDirection * bounceForce, ForceMode.Impulse);
            }
        }
    }
}
