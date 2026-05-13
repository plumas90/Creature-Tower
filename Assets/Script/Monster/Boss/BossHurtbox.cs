using UnityEngine;

[DisallowMultipleComponent]
public class BossHurtbox : MonoBehaviour
{
    [SerializeField] private CreatureBase ownerCreature;

    private void Awake()
    {
        if (ownerCreature == null)
            ownerCreature = GetComponentInParent<CreatureBase>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (ownerCreature == null)
            return;

        ownerCreature.HandleHurtboxTrigger(collision);
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (ownerCreature == null)
            return;

        ownerCreature.HandleHurtboxTrigger(collision);
    }
}
