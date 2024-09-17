using UnityEngine;

public class BlockMovement : MonoBehaviour
{
    // Movement speed
    public float moveSpeed = 10f;

    // Reference to the Rigidbody component
    private Rigidbody rb;

    // Initialize the Rigidbody component
    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        // Get input from W, A, S, D keys
        float moveHorizontal = Input.GetAxis("Horizontal"); // A, D
        float moveVertical = Input.GetAxis("Vertical"); // W, S

        // Create a movement vector
        Vector3 movement = new Vector3(moveHorizontal, 0.0f, moveVertical);

        // Apply the movement to the Rigidbody
        rb.AddForce(movement * moveSpeed);
    }
}
