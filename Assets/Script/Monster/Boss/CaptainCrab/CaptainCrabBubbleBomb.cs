using System.Collections;
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

    [Header("Bubble Bomb Animation Sprites")]
    [SerializeField] private Sprite[] idleSprites;
    [SerializeField] private Sprite[] boomSprites;
    [SerializeField] private float animationFps = 10f;

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

    private Coroutine animCoroutine;
    private bool isExploding = false;

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
            shadowRenderer.enabled = true;
        }

        isExploding = false;

        if (animCoroutine != null)
            StopCoroutine(animCoroutine);
        animCoroutine = StartCoroutine(CoPlayIdleAnimation());
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

        if (!isExploding && Time.time - spawnedAt >= Mathf.Max(0.1f, so.bombLifetime))
        {
            TriggerExplosion();
            return;
        }

        if (!isExploding)
        {
            StepState();
        }
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
                TriggerExplosion();
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
        if (collision == null || isExploding)
            return;

        PlayerStatControl playerStat = collision.GetComponent<PlayerStatControl>();
        if (playerStat == null)
            playerStat = collision.GetComponentInParent<PlayerStatControl>();

        if (playerStat != null)
        {
            if (damage > 0f)
            {
                playerStat.TryApplyContactDamage(damage, gameObject.GetInstanceID());
            }
            TriggerExplosion();
        }
    }

    private void TriggerExplosion()
    {
        if (isExploding) return;
        isExploding = true;

        if (shadowRenderer != null)
        {
            shadowRenderer.enabled = false;
        }

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
                if (idleSprites[index] != null && bodyRenderer != null)
                {
                    bodyRenderer.sprite = idleSprites[index];
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
                if (boomSprites[i] != null && bodyRenderer != null)
                {
                    bodyRenderer.sprite = boomSprites[i];
                }
                yield return new WaitForSeconds(delay);
            }
        }
        Destroy(gameObject);
    }
}

