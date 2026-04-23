using UnityEngine;

[DisallowMultipleComponent]
public class CaptainCrabProjectileBlocker : MonoBehaviour
{
    [SerializeField] private CaptainCrabBoss owner;

    public void Bind(CaptainCrabBoss boss)
    {
        owner = boss;
    }

    private void Awake()
    {
        if (owner == null)
            owner = GetComponentInParent<CaptainCrabBoss>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        TryDeletePlayerBullet(collision);
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        TryDeletePlayerBullet(collision);
    }

    private void TryDeletePlayerBullet(Collider2D collision)
    {
        if (owner == null || collision == null)
            return;

        Bullet bullet = collision.GetComponent<Bullet>();
        if (bullet == null)
            bullet = collision.GetComponentInParent<Bullet>();
        if (bullet == null || bullet.targets == null)
            return;

        if (!bullet.targets.ContainsValue((int)BulletTarget.Enemy))
            return;

        bullet.Destroy();
    }
}

