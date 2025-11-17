using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ItemPickup : MonoBehaviour
{
    public ItemSO itemData;

    void Reset()
    {
        // Para que el trigger se dispare al pasar por encima
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        // Buscamos componentes del jugador
        Health playerHealth = other.GetComponentInParent<Health>();
        Gun playerGun = other.GetComponentInParent<Gun>();

        if (itemData != null)
        {
            itemData.Apply(playerHealth, playerGun);
        }

        // Desaparece del mundo
        Destroy(gameObject);
    }
}
