using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float speed = 5f;
    public float mouseSensitivity = 2f;

    void Update()
    {
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        Vector3 move = transform.right * x + transform.forward * z;
        GetComponent<CharacterController>().Move(move * speed * Time.deltaTime);

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * 100f * Time.deltaTime;
        transform.Rotate(Vector3.up * mouseX);
    }

  void OnControllerColliderHit(ControllerColliderHit hit)

{

    Rigidbody rb = hit.collider.attachedRigidbody;

    if (rb != null && !rb.isKinematic)

    {

        Vector3 pushDir = new Vector3(hit.moveDirection.x, 0, hit.moveDirection.z);

        rb.AddForce(pushDir * 1f, ForceMode.VelocityChange);

    }

}

}