using UnityEngine;

public enum ItemType { Ammo, MedKit }

[CreateAssetMenu(menuName = "Parcial2/Item", fileName = "SO_Item")]
public class ItemSO : ScriptableObject
{
    public ItemType type;
    public int amount = 1; // cargadores extra o vida

    public void Apply(Health health, Gun gun)
    {
        switch (type)
        {
            case ItemType.Ammo:
                if (gun != null)
                {
                    gun.AddAmmoClips(amount);
                    Debug.Log($"[Item] +{amount} cargadores");
                }
                break;

            case ItemType.MedKit:
                if (health != null)
                {
                    health.Heal(amount);               // 👈 USAR Heal
                    Debug.Log($"[Item] +{amount} vida");
                }
                break;
        }
    }
}
