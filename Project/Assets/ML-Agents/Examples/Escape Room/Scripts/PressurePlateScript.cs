using UnityEngine;

public class PressurePlate : MonoBehaviour
{
    public Door door;                   // Reference to the Door script
    public Material activatedMaterial;  // Material to indicate the plate is activated
    public Material defaultMaterial;    // Default material
    public Renderer plateRenderer;      // The Renderer component for the pressure plate

    private bool isActivated = false;   // To prevent repeated activations

    private void Start()
    {
        // Ensure the plate starts with its default material
        if (plateRenderer != null && defaultMaterial != null)
        {
            plateRenderer.material = defaultMaterial;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isActivated && other.CompareTag("Agent"))
        {
            ActivatePlate();
            // Open the door
            if (door != null)
            {
                door.OpenDoor();
            }
            Debug.Log("Pressure plate activated by agent.");
        }
    }

    // Method to activate the pressure plate
    public void ActivatePlate()
    {
        if (!isActivated)
        {
            isActivated = true;

            // Change the material to indicate the plate is activated
            if (plateRenderer != null && activatedMaterial != null)
            {
                plateRenderer.material = activatedMaterial;
            }
            Debug.Log("PressurePlate: ActivatePlate() called.");
        }
    }

    // Method to reset the pressure plate to its default state
    public void ResetPlate()
    {
        isActivated = false;

        // Reset the material of the plate to the default one
        if (plateRenderer != null && defaultMaterial != null)
        {
            plateRenderer.material = defaultMaterial;
        }
        Debug.Log("PressurePlate: ResetPlate() called.");
    }
}
