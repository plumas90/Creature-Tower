using UnityEngine;

public class HauntedCrystalBallGhost : MonoBehaviour
{
    private Vector2 direction;
    private float speed;
    private float damage;
    private bool hasHit = false;

    public void Initialize(Vector2 dir, float spd, float dmg)
    {
        direction = dir.normalized;
        speed = spd;
        damage = dmg;
        hasHit = false;
    }

    private void Update()
    {
        if (hasHit)
            return;

        // 직선 이동
        transform.position += (Vector3)(direction * speed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (hasHit)
            return;

        // 플레이어 충돌 체크
        PlayerStatControl playerStat = collision.GetComponent<PlayerStatControl>();
        if (playerStat == null)
            playerStat = collision.GetComponentInParent<PlayerStatControl>();

        if (playerStat != null)
        {
            // 플레이어에게 데미지
            playerStat.TryApplyContactDamage(damage, gameObject.GetInstanceID());
            hasHit = true;
            Destroy(gameObject);
            return;
        }

        // 벽 충돌 체크
        if (collision.gameObject.layer == LayerMask.NameToLayer("Wall"))
        {
            hasHit = true;
            Destroy(gameObject);
        }
    }

    private void OnBecameInvisible()
    {
        // 화면 밖으로 나가면 삭제
        Destroy(gameObject, 0.5f);
    }
}
