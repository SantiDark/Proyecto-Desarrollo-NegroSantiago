using UnityEngine;

public class BillboardText : MonoBehaviour
{
    Camera cam;
    void LateUpdate()
    {
        if (!cam) cam = Camera.main;
        if (!cam) return;
        transform.LookAt(transform.position + cam.transform.rotation * Vector3.forward,
                         cam.transform.rotation * Vector3.up);
    }
}
