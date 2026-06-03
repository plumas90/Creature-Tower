using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class CaptainCrabBubbleProjectile : MonoBehaviour
{
    [SerializeField] private string sortingLayerName = "World_Dynamic";
    [SerializeField] private int ySortBaseOrder = 1500;
    [SerializeField] private int ySortScale = 10;
    [SerializeField] private int ySortOrderOffset = 2;

    [Header("Bubble Animation Sprites")]
    [SerializeField] private Sprite[] idleSprites;
    [SerializeField] private Sprite[] boomSprites;
    [SerializeField] private float animationFps = 10f;

    private Vector2 direction;
    private float speed;
    private float lifetime;
    private float damage;
    private float spawnedAt;
    private bool initialized;
    private SpriteRenderer spriteRenderer;

    private Coroutine animCoroutine;
    private bool isExploding = false;

    public void Initialize(Vector2 moveDirection, float moveSpeed, float life, float hitDamage)
    {
        direction = moveDirection.sqrMagnitude > 0.0001f ? moveDirection.normalized : Vector2.down;
        speed = Mathf.Max(0f, moveSpeed);
        lifetime = Mathf.Max(0.05f, life);
        damage = Mathf.Max(0f, hitDamage);
        spawnedAt = Time.time;
        initialized = true;

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>(true);

        isExploding = false;
        
        // Start idle animation
        if (animCoroutine != null)
            StopCoroutine(animCoroutine);
        animCoroutine = StartCoroutine(CoPlayIdleAnimation());
    }

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>(true);
    }

    private void Update()
    {
        if (!initialized)
            return;

        transform.position += (Vector3)(direction * speed * Time.deltaTime);
        ApplyYSort();

        // If not exploding, check if lifetime has expired
        if (!isExploding && Time.time - spawnedAt >= lifetime)
        {
            TriggerExplosion();
        }
    }

    private void ApplyYSort()
    {
        if (spriteRenderer == null)
            return;

        if (!string.IsNullOrEmpty(sortingLayerName))
            spriteRenderer.sortingLayerName = sortingLayerName;

        int order = ySortBaseOrder - Mathf.RoundToInt(transform.position.y * ySortScale) + ySortOrderOffset;
        spriteRenderer.sortingOrder = order;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!initialized || collision == null || isExploding)
            return;

        PlayerStatControl playerStat = collision.GetComponent<PlayerStatControl>();
        if (playerStat == null)
            playerStat = collision.GetComponentInParent<PlayerStatControl>();
        if (playerStat != null)
        {
            playerStat.TryApplyContactDamage(damage, gameObject.GetInstanceID());
            TriggerExplosion();
            return;
        }

        if (collision.gameObject.layer == LayerMask.NameToLayer("Wall"))
        {
            TriggerExplosion();
        }
    }

    private void TriggerExplosion()
    {
        if (isExploding) return;
        isExploding = true;

        speed = 0f;

        // Disable collider so it doesn't collide with anything else
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.enabled = false;
        }

        if (animCoroutine != null)
            StopCoroutine(animCoroutine);
        animCoroutine = StartCoroutine(CoPlayBoomAnimation());
    }

    private IEnumerator CoPlayIdleAnimation()
    {
        int index = 0;
        float delay = 1f / animationFps;
        while (!isExploding)
        {
            if (idleSprites != null && idleSprites.Length > 0)
            {
                if (idleSprites[index] != null)
                {
                    spriteRenderer.sprite = idleSprites[index];
                }
                index = (index + 1) % idleSprites.Length;
            }
            yield return new WaitForSeconds(delay);
        }
    }

    private IEnumerator CoPlayBoomAnimation()
    {
        if (boomSprites != null && boomSprites.Length > 0)
        {
            float delay = 1f / animationFps;
            for (int i = 0; i < boomSprites.Length; i++)
            {
                if (boomSprites[i] != null)
                {
                    spriteRenderer.sprite = boomSprites[i];
                }
                yield return new WaitForSeconds(delay);
            }
        }
        Destroy(gameObject);
    }
}


