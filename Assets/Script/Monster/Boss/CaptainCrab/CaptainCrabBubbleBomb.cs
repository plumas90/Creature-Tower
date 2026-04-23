using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class CaptainCrabBubbleBomb : MonoBehaviour
{
    private enum BombState
    {
        FlyLeftAir,
        DescendToShadow,
        SweepVertical,
        RollRight,
    }

    [Header("Optional References")]
    [SerializeField] private Transform shadowTransform;
    [SerializeField] private SpriteRenderer shadowRenderer;

    [Header("Sorting")]
    [SerializeField] private string dynamicSortingLayer = "World_Dynamic";
    [SerializeField] private string groundFxSortingLayer = "World_GroundFX";
    [SerializeField] private int ySortBaseOrder = 1500;
    [SerializeField] private int ySortScale = 10;
    [SerializeField] private int ySortOrderOffset = 0;
    [SerializeField] private int shadowSortingOrder = 0;

    private CaptainCrabBossSO so;
    private Bounds zoneBounds;
    private BombState state;
    private float spawnedAt;
    private float damage;
    private float shadowContactThreshold;
    private float minX;
    private float maxX;
    private float minY;
    private float maxY;
    private float bottomOffset;
    private float turnToRightAtY;
    private SpriteRenderer bodyRenderer;

    public void Initialize(CaptainCrabBossSO soData, Bounds zone)
    {
        so = soData;
        zoneBounds = zone;
        state = BombState.FlyLeftAir;
        spawnedAt = Time.time;
        damage = so != null ? Mathf.Max(0f, so.bombDamage) : 0f;
        shadowContactThreshold = so != null ? Mathf.Max(0f, so.bombShadowContactThreshold) : 0.03f;

        float leftOffset = so != null ? so.leftEdgeOffset : 0f;
        float rightOffset = so != null ? so.rightEdgeOffset : 0f;
        float topOffset = so != null ? so.topEdgeOffset : 0f;
        float bottomOffsetValue = so != null ? so.bottomEdgeOffset : 0f;
        minX = zoneBounds.min.x + leftOffset;
        maxX = zoneBounds.max.x - rightOffset;
        maxY = zoneBounds.max.y - topOffset;
        minY = zoneBounds.min.y + bottomOffsetValue;
        float halfY = minY + ((maxY - minY) * 0.5f);
        int minTurnY = Mathf.CeilToInt(minY);
        int maxTurnY = Mathf.FloorToInt(halfY);
        if (maxTurnY >= minTurnY)
            turnToRightAtY = Random.Range(minTurnY, maxTurnY + 1);
        else
            turnToRightAtY = halfY;

        if (bodyRenderer == null)
            bodyRenderer = GetComponent<SpriteRenderer>();
        if (bodyRenderer == null)
            bodyRenderer = GetComponentInChildren<SpriteRenderer>(true);

        if (bodyRenderer != null)
        {
            float spriteHeight = bodyRenderer.sprite != null ? bodyRenderer.sprite.bounds.size.y : 1f;
            bottomOffset = Mathf.Abs(spriteHeight * transform.lossyScale.y * 0.5f);
            bodyRenderer.sortingLayerName = dynamicSortingLayer;
        }
        else
        {
            bottomOffset = 0.5f;
        }

        if (shadowRenderer != null)
        {
            shadowRenderer.sortingLayerName = groundFxSortingLayer;
            shadowRenderer.sortingOrder = shadowSortingOrder;
        }
    }

    private void Awake()
    {
        bodyRenderer = GetComponent<SpriteRenderer>();
        if (bodyRenderer == null)
            bodyRenderer = GetComponentInChildren<SpriteRenderer>(true);
    }

    private void Update()
    {
        if (so == null)
            return;

        if (Time.time - spawnedAt >= Mathf.Max(0.1f, so.bombLifetime))
        {
            Destroy(gameObject);
            return;
        }

        StepState();
        ApplyYSort();
    }

    private void StepState()
    {
        Vector3 pos = transform.position;
        Vector3 shadowPos = shadowTransform != null ? shadowTransform.position : new Vector3(pos.x, pos.y - bottomOffset, pos.z);

        if (state == BombState.FlyLeftAir)
        {
            pos.x -= Mathf.Max(0f, so.bombAirMoveSpeed) * Time.deltaTime;
            shadowPos.x = pos.x;
            if (pos.x <= minX)
            {
                pos.x = minX;
                shadowPos.x = minX;
                state = BombState.DescendToShadow;
            }
        }
        else if (state == BombState.DescendToShadow)
        {
            shadowPos.x = minX;
            if (shadowPos.y < maxY)
                shadowPos.y = maxY;

            float targetBottomY = shadowPos.y;
            float nextY = pos.y - Mathf.Max(0f, so.bombDescendSpeed) * Time.deltaTime;
            pos.y = Mathf.Max(nextY, targetBottomY + bottomOffset);
            pos.x = minX;

            float peaBottomY = pos.y - bottomOffset;
            if (peaBottomY <= targetBottomY + shadowContactThreshold)
            {
                pos.y = targetBottomY + bottomOffset;
                state = BombState.SweepVertical;
            }
        }
        else if (state == BombState.SweepVertical)
        {
            pos.x = minX;
            shadowPos.x = minX;

            float step = Mathf.Max(0f, so.bombVerticalSweepSpeed) * Time.deltaTime;
            shadowPos.y -= step;
            if (shadowPos.y <= turnToRightAtY)
            {
                shadowPos.y = turnToRightAtY;
                state = BombState.RollRight;
            }

            pos.y = shadowPos.y + bottomOffset;
        }
        else
        {
            pos.x += Mathf.Max(0f, so.bombRollSpeed) * Time.deltaTime;
            shadowPos.x = pos.x;
            if (pos.x >= maxX)
            {
                Destroy(gameObject);
                return;
            }
        }

        transform.position = pos;
        if (shadowTransform != null)
            shadowTransform.position = new Vector3(shadowPos.x, shadowPos.y, shadowTransform.position.z);
    }

    private void ApplyYSort()
    {
        if (bodyRenderer == null)
            return;

        if (!string.IsNullOrEmpty(dynamicSortingLayer))
            bodyRenderer.sortingLayerName = dynamicSortingLayer;

        float pivotY = shadowTransform != null ? shadowTransform.position.y : transform.position.y;
        int order = ySortBaseOrder - Mathf.RoundToInt(pivotY * ySortScale) + ySortOrderOffset;
        bodyRenderer.sortingOrder = order;
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
        if (collision == null || damage <= 0f)
            return;

        PlayerStatControl playerStat = collision.GetComponent<PlayerStatControl>();
        if (playerStat == null)
            playerStat = collision.GetComponentInParent<PlayerStatControl>();

        if (playerStat != null)
            playerStat.TryApplyContactDamage(damage, gameObject.GetInstanceID());
    }
}

