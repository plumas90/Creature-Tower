using UnityEngine;

[DisallowMultipleComponent]
public class CaptainCrabClawSweepHitbox : MonoBehaviour
{
    [SerializeField] private float damage = 10f;
    [SerializeField] private bool activeState;
    private Collider2D[] hitColliders;

    public void SetDamage(float value)
    {
        damage = Mathf.Max(0f, value);
    }

    public void SetActiveState(bool active)
    {
        activeState = active;
        CacheCollidersIfNeeded();
        for (int i = 0; i < hitColliders.Length; i++)
        {
            Collider2D col = hitColliders[i];
            if (col == null || !col.isTrigger)
                continue;
            col.enabled = active;
        }
    }

    private void Awake()
    {
        CacheCollidersIfNeeded();
        SetActiveState(false);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        TryDamagePlayer(collision);
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        TryDamagePlayer(collision);
    }

    private void TryDamagePlayer(Collider2D collision)
    {
        if (!activeState || collision == null || damage <= 0f)
            return;

        PlayerStatControl player = collision.GetComponent<PlayerStatControl>();
        if (player == null)
            player = collision.GetComponentInParent<PlayerStatControl>();

        if (player != null)
            player.TryApplyContactDamage(damage, gameObject.GetInstanceID());
    }

    private void CacheCollidersIfNeeded()
    {
        if (hitColliders == null || hitColliders.Length == 0)
            hitColliders = GetComponentsInChildren<Collider2D>(true);
    }
}

