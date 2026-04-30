using UnityEngine;

public class DoorController : MonoBehaviour
{
    private bool isOpen = false;
    private Quaternion closedRotation;
    private Quaternion openRotation;

    public float openAngle = 90f;

    void Start()
    {
        closedRotation = transform.rotation;
        openRotation = Quaternion.Euler(0, transform.eulerAngles.y + openAngle, 0);
    }
  public void ToggleDoor()
  {
    if (!isOpen)
    {
        // Only check when OPENING
        RaycastHit hit;

        if (Physics.Raycast(transform.position, transform.forward, out hit, 2f))
        {
            if (hit.collider.CompareTag("Barricade"))
            {
                return; // BLOCK opening only
            }
        }
    }

    isOpen = !isOpen;

    if (isOpen)
        transform.rotation = openRotation;
    else
        transform.rotation = closedRotation;

    Debug.Log("Door state changed");

    PathManager.Instance.RecalculatePath();

  }
 
}