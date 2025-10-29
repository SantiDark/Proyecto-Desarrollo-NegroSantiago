using UnityEngine;

public class Gun : MonoBehaviour
{
    [Header("Pistola (no automática)")]
    public float range = 100f;
    public float damage = 15f;
    public float fireRate = 3f;
    public LayerMask hittableMask;   

    [Header("Raycast")]
    public Camera cam;             

    float nextShotTime;

    void Awake()
    {
        if (!cam) cam = Camera.main;
        if (!cam) Debug.LogWarning("[Gun] No camera assigned and no Camera.main found (Tag 'MainCamera' missing?)");
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0) && Time.time >= nextShotTime)
        {
            nextShotTime = Time.time + 1f / Mathf.Max(0.01f, fireRate);
            Shoot();
        }
    }

    void Shoot()
    {
        if (!cam)
        {
            Debug.LogError("[Gun] cam is null. Assign Main Camera in Inspector or tag your camera as 'MainCamera'.");
            return;
        }

        Vector3 origin = cam.transform.position;
        Vector3 dir = cam.transform.forward;

        // RAYCAST
        Debug.DrawRay(origin, dir * range, Color.white, 0.25f);

        // RAYCAST DE DAÑO
        if (Physics.Raycast(origin, dir, out RaycastHit hit, range, hittableMask, QueryTriggerInteraction.Collide))
        {
            var hp = hit.collider.GetComponentInParent<Health>();
            if (hp != null)
            {
                hp.TakeDamage(Mathf.RoundToInt(damage));
                Debug.Log($"[Gun] Hit {hit.collider.name} (layer {LayerMask.LayerToName(hit.collider.gameObject.layer)}) dmg {damage}");
            }

            else
            {
                Debug.Log($"[Gun] Hit {hit.collider.name} in layer {LayerMask.LayerToName(hit.collider.gameObject.layer)} but no Health on it");
            }
            return;
        }

        // 2) DEBUG
        if (Physics.Raycast(origin, dir, out RaycastHit debugHit, range, ~0, QueryTriggerInteraction.Collide))
        {
            string layerName = LayerMask.LayerToName(debugHit.collider.gameObject.layer);
            Debug.Log($"[Gun][DEBUG] Ray hits '{debugHit.collider.name}' on layer '{layerName}' but it's not in hittableMask");
        }
        else
        {
            Debug.Log("[Gun] Miss (no hit at all within range)");
        }
    }
}
