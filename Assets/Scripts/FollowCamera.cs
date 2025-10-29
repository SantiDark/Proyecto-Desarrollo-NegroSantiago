using UnityEngine;

public class FollowCamera : MonoBehaviour
{
    public Transform pivot; // arrastrá CameraPivot
    public Vector3 offset = new Vector3(0f, 2f, -4.5f);
    public float mouseSensitivity = 150f;
    public float minPitch = -35f, maxPitch = 70f;
    float yaw, pitch;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        var e = transform.eulerAngles; yaw = e.y; pitch = e.x;
    }

    void LateUpdate()
    {
        if (!pivot) return;
        yaw += Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        var rot = Quaternion.Euler(pitch, yaw, 0f);
        transform.position = pivot.position + rot * offset;
        transform.rotation = rot;
    }
}
