using UnityEngine;

public class BillboardToCamera : MonoBehaviour
{
    Camera cam;
    void LateUpdate()
    {
        if (!cam) cam = Camera.main;
        if (!cam) return;
        transform.forward = cam.transform.forward;
    }
}
