using UnityEngine;
using UnityEngine.Tilemaps;

public class HauntedCrystalBallGhost : MonoBehaviour
{
    [SerializeField] private bool destroyOnTilemapCollider = true;
    private Vector2 direction;
    private float speed;
    private float damage;
    private bool hasHit = false;

    [Header("Sprite Animation")]
    [SerializeField] private Sprite[] idleSprites;
    [SerializeField] private float animationFps = 10f;
    private SpriteRenderer spriteRenderer;
    private float animTimer;
    private int currentFrame;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    public void Initialize(Vector2 dir, float spd, float dmg)
    {
        direction = dir.normalized;
        speed = spd;
        damage = dmg;
        hasHit = false;

        if (direction.sqrMagnitude > 0.0001f)
        {
            float targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            targetAngle = Mathf.DeltaAngle(0f, targetAngle);

            float scaleX = 1f;
            if (targetAngle > 90f || targetAngle < -90f)
            {
                scaleX = -1f;
            }

            float finalRotationAngle = targetAngle;
            if (scaleX < 0f)
            {
                finalRotationAngle = targetAngle + 180f;
            }

            transform.rotation = Quaternion.Euler(0f, 0f, finalRotationAngle);
            transform.localScale = new Vector3(scaleX, 1f, 1f);
        }

        if (idleSprites != null && idleSprites.Length > 0 && spriteRenderer != null)
        {
            currentFrame = 0;
            animTimer = 0f;
            spriteRenderer.sprite = idleSprites[0];
        }
    }

    private void Update()
    {
        if (hasHit)
            return;

        // 직선 이동
        transform.position += (Vector3)(direction * speed * Time.deltaTime);

        // 프레임 애니메이션 재생
        if (idleSprites != null && idleSprites.Length > 0 && spriteRenderer != null)
        {
            animTimer += Time.deltaTime;
            float frameDelay = 1f / animationFps;
            if (animTimer >= frameDelay)
            {
                animTimer -= frameDelay;
                currentFrame = (currentFrame + 1) % idleSprites.Length;
                spriteRenderer.sprite = idleSprites[currentFrame];
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Debug.Log($"[HauntedCrystalBallGhost] OnTriggerEnter2D with {collision.gameObject.name}");
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

        // 벽/타일맵 벽 충돌 체크
        if (IsWallCollision(collision))
        {
            hasHit = true;
            Destroy(gameObject);
        }
    }

    private bool IsWallCollision(Collider2D collision)
    {
        if (collision == null)
            return false;

        int wallLayer = LayerMask.NameToLayer("Wall");
        if (wallLayer >= 0)
        {
            Transform cur = collision.transform;
            while (cur != null)
            {
                if (cur.gameObject.layer == wallLayer)
                    return true;
                cur = cur.parent;
            }
        }

        if (!destroyOnTilemapCollider)
            return false;

        TilemapCollider2D tilemapCol = collision.GetComponent<TilemapCollider2D>();
        if (tilemapCol == null)
            tilemapCol = collision.GetComponentInParent<TilemapCollider2D>();

        return tilemapCol != null;
    }

    private void OnBecameInvisible()
    {
        // 화면 밖으로 나가면 삭제
        Destroy(gameObject, 0.5f);
    }
}
