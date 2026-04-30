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
        isOpen = !isOpen;

        if (isOpen)
            transform.rotation = openRotation;
        else
            transform.rotation = closedRotation;
    }
}