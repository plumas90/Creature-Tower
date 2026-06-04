using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(BoxCollider2D))]
public class PeaPodVineSegment : MonoBehaviour
{
    [Header("Vine Sprites")]
    [SerializeField] private Sprite[] growSprites; // size 7 (grow1~7)
    [SerializeField] private Sprite growEndSprite; // grow_end
    [SerializeField] private Sprite[] dieSprites;   // size 5 (die1~5)
    [SerializeField] private float vineScale = 0.5f; // Scale multiplier (default 0.5)

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
    private float despawnDelaySeconds = 0f; // Start die animation immediately on hit
    private LayerMask wallMask;

    private bool isDying;
    private float dyingStartedAt;
    private float dieFrameDuration = 0.08f; // 0.08s per frame * 5 frames = 0.4s total

    private SpriteRenderer sr;
    private BoxCollider2D hitbox;
    private readonly Collider2D[] overlapResults = new Collider2D[8];
    private ContactFilter2D overlapFilter;

    public bool IsFullyGrown => completed && !isDying;
    
    public Vector2 TipWorldPosition
    {
        get
        {
            float width = 0f;
            if (sr != null && sr.sprite != null)
                width = sr.sprite.bounds.size.x;
            else
                width = spriteBaseWidth;

            return anchorWorldPosition + transform.right * (width * Mathf.Abs(transform.localScale.x));
        }
    }

    public void Initialize(float finalDamage, float speed, float targetLength, float lifetimeAfterGrow, float hitboxHeightRatio)
    {
        damage = Mathf.Max(0f, finalDamage);
        growthSpeed = Mathf.Max(0.01f, speed);
        lifeAfterGrowth = Mathf.Max(0.05f, lifetimeAfterGrow);
        colliderHeightRatio = Mathf.Clamp(hitboxHeightRatio, 0.1f, 1f);
        completed = false;
        anchorWorldPosition = transform.position;
        despawnScheduled = false;
        despawnAt = 0f;
        despawnDelaySeconds = 0f; // Start die animation immediately on hit
        isDying = false;
        dyingStartedAt = 0f;

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

        // Set maxLength to the native width of the grow_end sprite to ensure scaled size
        float baseWidth = (growEndSprite != null) ? growEndSprite.bounds.size.x : spriteBaseWidth;
        maxLength = baseWidth * vineScale;
        currentLength = 0.01f;

        if (hitbox != null)
            hitbox.isTrigger = false;
        overlapFilter = new ContactFilter2D
        {
            useLayerMask = false,
            useTriggers = true
        };

        // Determine if we need to flip Y axis to keep the vine upright when it grows left
        float targetAngle = Mathf.DeltaAngle(0f, transform.eulerAngles.z);
        float flipY = 1f;
        if (targetAngle > 90f || targetAngle < -90f)
        {
            flipY = -1f;
        }

        transform.localScale = new Vector3(initialLocalScale.x * vineScale, initialLocalScale.y * vineScale * flipY, initialLocalScale.z);

        ApplyVisualAndCollider();
    }

    private void Update()
    {
        if (isDying)
        {
            float elapsed = Time.time - dyingStartedAt;
            float totalDuration = dieFrameDuration * 5f;
            if (elapsed >= totalDuration)
            {
                Destroy(gameObject);
                return;
            }

            int frameIndex = Mathf.Clamp(Mathf.FloorToInt(elapsed / dieFrameDuration), 0, 4);
            if (dieSprites != null && frameIndex < dieSprites.Length)
            {
                if (sr != null && dieSprites[frameIndex] != null)
                {
                    sr.sprite = dieSprites[frameIndex];
                    transform.position = anchorWorldPosition - transform.right * (sr.sprite.bounds.min.x * transform.localScale.x);
                }
            }
            return;
        }

        if (despawnScheduled && Time.time >= despawnAt)
        {
            StartDying();
            return;
        }

        if (!completed)
        {
            currentLength = Mathf.Min(maxLength, currentLength + growthSpeed * Time.deltaTime);

            if (currentLength >= maxLength - 0.0001f)
            {
                completed = true;
                completedAt = Time.time;
            }

            ApplyVisualAndCollider();
            return;
        }

        if (Time.time - completedAt >= lifeAfterGrowth)
        {
            StartDying();
        }
    }

    private void FixedUpdate()
    {
        if (hitbox == null || damage <= 0f || isDying)
            return;

        // 벽 접촉 시는 즉시 제거를 유지한다.
        if (IsVineTouchingWall())
        {
            StartDying();
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
        if (isDying)
            return;

        if (sr != null)
        {
            if (!completed)
            {
                float progress = currentLength / maxLength;
                if (growSprites != null && growSprites.Length > 0)
                {
                    int frameIndex = Mathf.Clamp(Mathf.FloorToInt(progress * growSprites.Length), 0, growSprites.Length - 1);
                    sr.sprite = growSprites[frameIndex];
                }
            }
            else
            {
                if (growEndSprite != null)
                    sr.sprite = growEndSprite;
            }

            // Align left edge of the sprite to anchorWorldPosition based on current pivot bounds
            if (sr.sprite != null)
            {
                transform.position = anchorWorldPosition - transform.right * (sr.sprite.bounds.min.x * transform.localScale.x);
            }
        }

        if (hitbox != null && sr != null && sr.sprite != null)
        {
            Vector3 spriteSize = sr.sprite.bounds.size;
            Vector3 spriteCenter = sr.sprite.bounds.center;
            hitbox.size = new Vector2(spriteSize.x, spriteSize.y * colliderHeightRatio);
            hitbox.offset = new Vector2(spriteCenter.x, spriteCenter.y);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision == null || isDying)
            return;

        TryApplyDamage(collision.collider);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (collision == null || isDying)
            return;

        TryApplyDamage(collision.collider);
    }

    private void TryApplyDamage(Collider2D targetCollider)
    {
        if (targetCollider == null || isDying)
            return;

        PlayerStatControl playerStat = targetCollider.GetComponent<PlayerStatControl>();
        if (playerStat == null)
            playerStat = targetCollider.GetComponentInParent<PlayerStatControl>();

        if (playerStat != null)
        {
            bool applied = playerStat.TryApplyContactDamage(damage, gameObject.GetInstanceID());
            // 플레이어 히트 성공 시 줄기는 즉시/딜레이 후 소멸 단계를 시작한다.
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

    private void StartDying()
    {
        if (isDying)
            return;

        isDying = true;
        dyingStartedAt = Time.time;

        if (hitbox != null)
            hitbox.enabled = false;
    }
}
