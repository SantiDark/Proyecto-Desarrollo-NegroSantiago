using UnityEngine;

public class TPSCameraFollow : MonoBehaviour
{
    public Transform player;                    
    public Vector3 offset = new Vector3(0f, 2f, -4.5f);
    public float mouseSensitivity = 150f;
    public float minPitch = -35f, maxPitch = 70f;

    float yaw;   // Giro (Según GPT)
    float pitch; // Inclinación de la cámara (Según GPT)

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (player) yaw = player.eulerAngles.y;
        pitch = 10f; // inicio cómodo
    }

    void LateUpdate()
    {
        if (!player) return;

        // MOUSE
        float mx = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float my = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        yaw += mx;
        pitch -= my;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        // ROTAR EN X
        player.rotation = Quaternion.Euler(0f, yaw, 0f);

        // ROTAR EN Y
        Quaternion camRot = Quaternion.Euler(pitch, yaw, 0f);
        transform.position = player.position + camRot * offset;
        transform.rotation = camRot;
    }
}
