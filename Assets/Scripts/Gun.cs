using UnityEngine;
using TMPro;  // necesario para usar TextMeshProUGUI

public class Gun : MonoBehaviour
{
    [Header("Pistola (no automática)")]
    public float range = 100f;
    public float damage = 15f;
    public float fireRate = 3f;
    public LayerMask hittableMask;

    [Header("Raycast")]
    public Camera cam;

    [Header("Ammo")]
    public int magazineSize = 7;             // tamaño del cargador
    public int bulletsInMag;                 // balas actuales
    public int totalAmmo = 2;               // balas totales (reserva)
    public KeyCode reloadKey = KeyCode.R;    // tecla para recargar

    [Header("UI")]
    public TextMeshProUGUI ammoText;         // referencia al TMP del HUD

    float nextShotTime;

    void Awake()
    {
        if (!cam) cam = Camera.main;
        if (!cam)
            Debug.LogWarning("[Gun] No camera assigned and no Camera.main found (Tag 'MainCamera' missing?)");

        // cargador lleno al iniciar
        bulletsInMag = magazineSize;
        UpdateAmmoUI();
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0) && Time.time >= nextShotTime)
            TryShoot();

        if (Input.GetKeyDown(reloadKey))
            TryReload();
    }

    void TryShoot()
    {
        if (bulletsInMag <= 0)
        {
            Debug.Log("[Gun] Cargador vacío. Presiona R para recargar.");
            UpdateAmmoUI();
            return;
        }

        nextShotTime = Time.time + 1f / Mathf.Max(0.01f, fireRate);
        bulletsInMag--;
        UpdateAmmoUI();

        ShootRaycast();
    }

    void TryReload()
    {
        if (bulletsInMag == magazineSize)
        {
            Debug.Log("[Gun] Cargador completo, no se recarga.");
            return;
        }

        if (totalAmmo <= 0)
        {
            Debug.Log("[Gun] Sin munición extra.");
            return;
        }

        int bulletsNeeded = magazineSize - bulletsInMag;
        int bulletsToLoad = Mathf.Min(bulletsNeeded, totalAmmo);

        bulletsInMag += bulletsToLoad;
        totalAmmo -= bulletsToLoad;

        UpdateAmmoUI();
        Debug.Log($"[Gun] Recargado ({bulletsToLoad} balas). Total restante: {totalAmmo}");
    }

    void ShootRaycast()
    {
        if (!cam)
        {
            Debug.LogError("[Gun] cam is null. Asigna la cámara en el Inspector o taggeá la MainCamera.");
            return;
        }

        Vector3 origin = cam.transform.position;
        Vector3 dir = cam.transform.forward;

        Debug.DrawRay(origin, dir * range, Color.white, 0.25f);

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
                Debug.Log($"[Gun] Hit {hit.collider.name} en layer {LayerMask.LayerToName(hit.collider.gameObject.layer)} pero sin componente Health");
            }
        }
        else if (Physics.Raycast(origin, dir, out RaycastHit debugHit, range, ~0, QueryTriggerInteraction.Collide))
        {
            string layerName = LayerMask.LayerToName(debugHit.collider.gameObject.layer);
            Debug.Log($"[Gun][DEBUG] Ray hits '{debugHit.collider.name}' en layer '{layerName}' pero no está en hittableMask");
        }
        else
        {
            Debug.Log("[Gun] Miss (no hit dentro del rango)");
        }
    }

    void UpdateAmmoUI()
    {
        if (ammoText)
            ammoText.text = $"{bulletsInMag} | {totalAmmo}";

    }

}
