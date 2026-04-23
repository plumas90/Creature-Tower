using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class CaptainCrabBubbleProjectile : MonoBehaviour
{
    [SerializeField] private string sortingLayerName = "World_Dynamic";
    [SerializeField] private int ySortBaseOrder = 1500;
    [SerializeField] private int ySortScale = 10;
    [SerializeField] private int ySortOrderOffset = 2;

    private Vector2 direction;
    private float speed;
    private float lifetime;
    private float damage;
    private float spawnedAt;
    private bool initialized;
    private SpriteRenderer spriteRenderer;

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

        if (Time.time - spawnedAt >= lifetime)
            Destroy(gameObject);
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
        if (!initialized || collision == null)
            return;

        PlayerStatControl playerStat = collision.GetComponent<PlayerStatControl>();
        if (playerStat == null)
            playerStat = collision.GetComponentInParent<PlayerStatControl>();
        if (playerStat != null)
        {
            playerStat.TryApplyContactDamage(damage, gameObject.GetInstanceID());
            Destroy(gameObject);
            return;
        }

        if (collision.gameObject.layer == LayerMask.NameToLayer("Wall"))
            Destroy(gameObject);
    }
}

