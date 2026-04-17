using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(BoxCollider2D))]
public class PeaPodVineSegment : MonoBehaviour
{
    private float damage;
    private float growthSpeed;
    private float maxLength;
    private float lifeAfterGrowth;
    private float colliderHeightRatio;
    private float currentLength;
    private bool completed;
    private float completedAt;
    private Vector3 anchorWorldPosition;
    private Vector3 initialLocalScale;
    private float spriteBaseWidth = 1f;
    private float spriteBaseHeight = 1f;
    private bool despawnScheduled;
    private float despawnAt;
    private float despawnDelaySeconds = 0.5f;
    private LayerMask wallMask;

    private SpriteRenderer sr;
    private BoxCollider2D hitbox;
    private readonly Collider2D[] overlapResults = new Collider2D[8];
    private ContactFilter2D overlapFilter;

    public bool IsFullyGrown => completed;
    public Vector2 TipWorldPosition => anchorWorldPosition + transform.right * currentLength;

    public void Initialize(float finalDamage, float speed, float targetLength, float lifetimeAfterGrow, float hitboxHeightRatio)
    {
        damage = Mathf.Max(0f, finalDamage);
        growthSpeed = Mathf.Max(0.01f, speed);
        maxLength = Mathf.Max(0.05f, targetLength);
        lifeAfterGrowth = Mathf.Max(0.05f, lifetimeAfterGrow);
        colliderHeightRatio = Mathf.Clamp(hitboxHeightRatio, 0.1f, 1f);
        currentLength = 0.01f;
        completed = false;
        anchorWorldPosition = transform.position;
        despawnScheduled = false;
        despawnAt = 0f;
        int wallLayer = LayerMask.NameToLayer("Wall");
        wallMask = wallLayer >= 0 ? (1 << wallLayer) : 0;

        sr = GetComponent<SpriteRenderer>();
        hitbox = GetComponent<BoxCollider2D>();
        initialLocalScale = transform.localScale;

        if (sr != null && sr.sprite != null)
        {
            spriteBaseWidth = Mathf.Max(0.01f, sr.sprite.bounds.size.x);
            spriteBaseHeight = Mathf.Max(0.01f, sr.sprite.bounds.size.y);
        }

        if (hitbox != null)
            hitbox.isTrigger = false;
        overlapFilter = new ContactFilter2D
        {
            useLayerMask = false,
            useTriggers = true
        };

        ApplyVisualAndCollider();
    }

    private void Update()
    {
        if (despawnScheduled && Time.time >= despawnAt)
        {
            Destroy(gameObject);
            return;
        }

        if (!completed)
        {
            currentLength = Mathf.Min(maxLength, currentLength + growthSpeed * Time.deltaTime);
            ApplyVisualAndCollider();

            if (currentLength >= maxLength - 0.0001f)
            {
                completed = true;
                completedAt = Time.time;
            }

            return;
        }

        if (Time.time - completedAt >= lifeAfterGrowth)
            Destroy(gameObject);
    }

    private void FixedUpdate()
    {
        if (hitbox == null || damage <= 0f)
            return;

        // 벽 접촉 시는 즉시 제거를 유지한다.
        if (IsVineTouchingWall())
        {
            Destroy(gameObject);
            return;
        }

        int count = hitbox.Overlap(overlapFilter, overlapResults);
        for (int i = 0; i < count; i++)
        {
            TryApplyDamage(overlapResults[i]);
        }
    }

    private void ApplyVisualAndCollider()
    {
        // 세그먼트 시작점(anchor)은 고정하고, 중심만 오른쪽으로 이동시켜
        // "중앙에서 피는" 느낌이 아니라 "앞으로 뻗는" 느낌을 만든다.
        transform.position = anchorWorldPosition + transform.right * (currentLength * 0.5f);

        if (sr != null)
        {
            // 스프라이트 월드 길이를 currentLength와 일치시키기 위해
            // 로컬 x 스케일은 "목표 길이 / 원본 스프라이트 길이"로 맞춘다.
            Vector3 localScale = initialLocalScale;
            float xSign = Mathf.Sign(localScale.x == 0f ? 1f : localScale.x);
            localScale.x = xSign * (currentLength / spriteBaseWidth);
            transform.localScale = localScale;
        }

        if (hitbox != null)
        {
            Vector2 size = hitbox.size;
            // 콜라이더 로컬 크기는 원본 스프라이트 기준으로 유지하고,
            // transform 스케일에 의해 함께 커지게 해서 스프라이트와 정확히 동기화한다.
            size.x = spriteBaseWidth;
            size.y = Mathf.Max(0.05f, spriteBaseHeight * colliderHeightRatio);
            hitbox.size = size;
            hitbox.offset = Vector2.zero;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision == null)
            return;

        TryApplyDamage(collision.collider);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (collision == null)
            return;

        TryApplyDamage(collision.collider);
    }

    private void TryApplyDamage(Collider2D targetCollider)
    {
        if (targetCollider == null)
            return;

        PlayerStatControl playerStat = targetCollider.GetComponent<PlayerStatControl>();
        if (playerStat == null)
            playerStat = targetCollider.GetComponentInParent<PlayerStatControl>();

        if (playerStat != null)
        {
            bool applied = playerStat.TryApplyContactDamage(damage, gameObject.GetInstanceID());
            // 플레이어 히트 성공 시 해당 줄기는 0.5초 후 제거한다.
            if (applied)
                ScheduleDespawn();
        }
    }

    private bool IsVineTouchingWall()
    {
        if (hitbox == null || wallMask.value == 0)
            return false;

        return hitbox.IsTouchingLayers(wallMask);
    }

    private void ScheduleDespawn()
    {
        if (despawnScheduled)
            return;

        despawnScheduled = true;
        despawnAt = Time.time + Mathf.Max(0f, despawnDelaySeconds);
    }
}
