using UnityEngine;
using TMPro;

public class Gun : MonoBehaviour
{
    [Header("Pistola (no automática)")]
    public float range = 100f;
    public float damage = 15f;
    public float fireRate = 3f;
    public LayerMask hittableMask;

    [Header("Raycast")]
    public Camera cam;

    [Header("Ammo (Cargadores)")]
    public int magazineSize = 15;   // 15 balas por cargador
    public int magazines = 2;       // cantidad de cargadores (enteros)
    public KeyCode reloadKey = KeyCode.R;

    [Header("UI")]
    public TextMeshProUGUI ammoText;

    int bulletsInMag;               // balas actuales en el cargador puesto
    float nextShotTime;

    void Awake()
    {
        if (!cam) cam = Camera.main;
        if (!cam)
            Debug.LogWarning("[Gun] No camera assigned and no Camera.main found (Tag 'MainCamera' missing?)");

        ResetAmmo(); // arranca en 2x15
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
        if (bulletsInMag >= magazineSize)
        {
            Debug.Log("[Gun] Cargador completo, no se recarga.");
            return;
        }

        if (magazines <= 0)
        {
            Debug.Log("[Gun] Sin cargadores restantes.");
            return;
        }

        magazines--;                  // gastás 1 cargador
        bulletsInMag = magazineSize;  // recarga a 15
        UpdateAmmoUI();
        Debug.Log($"[Gun] Recargado. Cargadores restantes: {magazines}");
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
                Debug.Log($"[Gun] Hit {hit.collider.name} dmg {damage}");
            }
        }
    }

    void UpdateAmmoUI()
    {
        if (ammoText)
            ammoText.text = $"{bulletsInMag} | {magazines}";
    }

    public void ResetAmmo()
    {
        magazines = 2;
        bulletsInMag = magazineSize;
        UpdateAmmoUI();
    }
}
