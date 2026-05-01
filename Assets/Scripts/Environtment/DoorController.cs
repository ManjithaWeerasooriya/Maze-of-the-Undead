using UnityEngine;

public class DoorController : MonoBehaviour
{
    private bool isOpen = false;

    private Quaternion closedRotation;
    private Quaternion openRotation;

    public float openAngle = 90f;
    public float speed = 3f;

    void Start()
    {
        closedRotation = transform.rotation;

        // FIXED: correct open rotation based on current rotation
        openRotation = Quaternion.Euler(
            transform.eulerAngles.x,
            transform.eulerAngles.y + openAngle,
            transform.eulerAngles.z
        );
    }

    void Update()
    {
        // Smooth rotation instead of instant snap
        Quaternion targetRotation = isOpen ? openRotation : closedRotation;
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * speed);
    }

    public void ToggleDoor()
    {
        if (!isOpen)
        {
            // Only block when OPENING
            RaycastHit hit;

            if (Physics.Raycast(transform.position, transform.forward, out hit, 2f))
            {
                if (hit.collider.CompareTag("Barricade"))
                {
                    Debug.Log("Door blocked by barricade");
                    return;
                }
            }
        }

        isOpen = !isOpen;

        Debug.Log("Door state changed");

        if (PathManager.Instance != null)
        {
            PathManager.Instance.RecalculatePath();
        }
    }
}