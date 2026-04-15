using UnityEngine;

[DisallowMultipleComponent]
public class BossHurtbox : MonoBehaviour
{
    [SerializeField] private BossBase ownerBoss;

    private void Awake()
    {
        if (ownerBoss == null)
            ownerBoss = GetComponentInParent<BossBase>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (ownerBoss == null)
            return;

        ownerBoss.HandleHurtboxTrigger(collision);
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (ownerBoss == null)
            return;

        ownerBoss.HandleHurtboxTrigger(collision);
    }
}
