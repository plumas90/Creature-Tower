using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(SpriteRenderer))]
public class PeaPodDeathPea : MonoBehaviour
{
    [Header("Optional References")]
    [SerializeField] private Transform shadowTransform;
    [SerializeField] private SpriteRenderer shadowRenderer;
    [SerializeField] private SpriteRenderer groundFxRenderer;

    [Header("Ground FX Sorting")]
    [SerializeField] private string groundFxSortingLayer = "World_GroundFX";
    [SerializeField] private int groundFxSortingOrder = 0;

    [Header("Dynamic Y Sorting")]
    [SerializeField] private string dynamicSortingLayer = "World_Dynamic";
    [SerializeField] private int ySortBaseOrder = 1500;
    [SerializeField] private int ySortScale = 10;
    [SerializeField] private int ySortOrderOffset = 0;

    [Header("Shadow Sorting")]
    [SerializeField] private string shadowSortingLayer = "World_GroundFX";
    [SerializeField] private int shadowSortingOrder = 0;

    private float flightDuration;
    private float riseDuration;
    private float fallDuration;
    private float arcHeight;
    private float landedWaitDuration;
    private float redWarningDuration;
    private float explosionDamage;
    private float explosionRadius;
    private float groundFxRadiusMultiplier;

    private Vector2 startPos;
    private Vector2 targetPos;
    private float startedAt;
    private bool landed;
    private float landedAt;
    private bool enteredRedWarning;
    private float redWarningStartedAt;
    private SpriteRenderer peaRenderer;
    private Color originalColor;
    private float peaBottomOffset;
    private readonly HashSet<PlayerStatControl> trackedPlayers = new HashSet<PlayerStatControl>();
    private PeaPodGroundFxTrigger groundFxTrigger;
    private CapsuleCollider2D groundFxCapsule;

    public void Initialize(
        Vector2 destination,
        float inRiseDuration,
        float inFallDuration,
        float inArcHeight,
        float inLandedWaitDuration,
        float inRedWarningDuration,
        float inExplosionDamage,
        float inExplosionRadius,
        float inGroundFxRadiusMultiplier)
    {
        startPos = transform.position;
        targetPos = destination;
        riseDuration = Mathf.Max(0.05f, inRiseDuration);
        fallDuration = Mathf.Max(0.05f, inFallDuration);
        flightDuration = riseDuration + fallDuration;
        arcHeight = Mathf.Max(0f, inArcHeight);
        landedWaitDuration = Mathf.Max(0f, inLandedWaitDuration);
        redWarningDuration = Mathf.Max(0.05f, inRedWarningDuration);
        explosionDamage = Mathf.Max(0f, inExplosionDamage);
        explosionRadius = Mathf.Max(0.05f, inExplosionRadius);
        groundFxRadiusMultiplier = Mathf.Max(1f, inGroundFxRadiusMultiplier);
        startedAt = Time.time;
        landed = false;
        enteredRedWarning = false;
        redWarningStartedAt = 0f;
        trackedPlayers.Clear();

        peaRenderer = GetComponent<SpriteRenderer>();
        if (peaRenderer != null)
        {
            originalColor = peaRenderer.color;
            float spriteHeight = peaRenderer.sprite != null ? peaRenderer.sprite.bounds.size.y : 1f;
            peaBottomOffset = Mathf.Abs(spriteHeight * transform.lossyScale.y * 0.5f);
        }
        else
        {
            peaBottomOffset = 0.5f;
        }

        SetupGroundFxVisual();
    }

    private void Awake()
    {
        peaRenderer = GetComponent<SpriteRenderer>();
        if (peaRenderer != null)
        {
            originalColor = peaRenderer.color;
            float spriteHeight = peaRenderer.sprite != null ? peaRenderer.sprite.bounds.size.y : 1f;
            peaBottomOffset = Mathf.Abs(spriteHeight * transform.lossyScale.y * 0.5f);
        }
        else
        {
            peaBottomOffset = 0.5f;
        }

        if (shadowRenderer != null)
        {
            shadowRenderer.sortingLayerName = shadowSortingLayer;
            shadowRenderer.sortingOrder = shadowSortingOrder;
        }
    }

    private void Update()
    {
        UpdateDynamicSorting();

        if (!landed)
        {
            UpdateFlight();
            return;
        }

        float landedElapsed = Time.time - landedAt;
        if (!enteredRedWarning)
        {
            if (landedElapsed >= landedWaitDuration)
            {
                enteredRedWarning = true;
                redWarningStartedAt = Time.time;
                SetGroundFxVisible(true);
            }

            return;
        }

        float warningT = redWarningDuration > 0f
            ? Mathf.Clamp01((Time.time - redWarningStartedAt) / redWarningDuration)
            : 1f;

        if (peaRenderer != null)
            peaRenderer.color = Color.Lerp(originalColor, Color.red, warningT);

        if (warningT >= 1f)
            Explode();
    }

    private void UpdateFlight()
    {
        float elapsed = Time.time - startedAt;
        float t = Mathf.Clamp01(elapsed / flightDuration);
        Vector2 flatPos = Vector2.Lerp(startPos, targetPos, t);
        float verticalOffset = Mathf.Sin(t * Mathf.PI) * arcHeight;

        transform.position = new Vector3(flatPos.x, flatPos.y + verticalOffset, transform.position.z);

        if (shadowTransform != null)
        {
            shadowTransform.position = new Vector3(flatPos.x, flatPos.y, shadowTransform.position.z);
        }

        // 하강 구간에서 완두 바닥이 그림자 중심(y)와 닿는 순간 착지 처리.
        if (elapsed >= riseDuration)
        {
            float shadowCenterY = shadowTransform != null ? shadowTransform.position.y : flatPos.y;
            float peaBottomY = transform.position.y - peaBottomOffset;
            if (peaBottomY <= shadowCenterY + 0.0001f)
            {
                EnterWarningState();
                return;
            }
        }

        if (t >= 1f)
            EnterWarningState();
    }

    private void EnterWarningState()
    {
        if (landed)
            return;

        landed = true;
        landedAt = Time.time;

        Vector3 landedPos;
        if (shadowTransform != null)
            landedPos = new Vector3(shadowTransform.position.x, shadowTransform.position.y + peaBottomOffset, transform.position.z);
        else
            landedPos = new Vector3(targetPos.x, targetPos.y + peaBottomOffset, transform.position.z);

        transform.position = landedPos;
        if (shadowTransform != null)
            shadowTransform.position = new Vector3(landedPos.x, landedPos.y - peaBottomOffset, shadowTransform.position.z);
        if (peaRenderer != null)
            peaRenderer.color = originalColor;
        SetGroundFxVisible(false);
    }

    private void Explode()
    {
        PlayerStatControl[] victims = new PlayerStatControl[trackedPlayers.Count];
        trackedPlayers.CopyTo(victims);
        for (int i = 0; i < victims.Length; i++)
        {
            PlayerStatControl playerStat = victims[i];
            if (playerStat == null)
                continue;

            playerStat.TryApplyContactDamage(explosionDamage, gameObject.GetInstanceID());
        }

        Destroy(gameObject);
    }

    private void SetupGroundFxVisual()
    {
        float fxRadius = explosionRadius * groundFxRadiusMultiplier;

        if (groundFxRenderer != null)
        {
            groundFxRenderer.sortingLayerName = groundFxSortingLayer;
            groundFxRenderer.sortingOrder = groundFxSortingOrder;
            Color c = groundFxRenderer.color;
            c.a = Mathf.Clamp01(c.a <= 0f ? 0.35f : c.a);
            groundFxRenderer.color = c;

            EnsureGroundFxTrigger();
            ApplyGroundFxSizeFromCapsule(fxRadius * 2f);
            SetGroundFxVisible(false);
        }

        if (shadowRenderer != null)
        {
            Color sc = shadowRenderer.color;
            sc.a = Mathf.Clamp01(sc.a <= 0f ? 0.5f : sc.a);
            shadowRenderer.color = sc;
        }
    }

    private void EnsureGroundFxTrigger()
    {
        if (groundFxRenderer == null)
            return;

        GameObject fxObject = groundFxRenderer.gameObject;
        if (fxObject == null)
            return;

        CapsuleCollider2D capsule = fxObject.GetComponent<CapsuleCollider2D>();
        if (capsule == null)
            capsule = fxObject.AddComponent<CapsuleCollider2D>();
        capsule.isTrigger = true;
        capsule.direction = CapsuleDirection2D.Horizontal;
        capsule.offset = Vector2.zero;
        if (capsule.size.x <= 0f || capsule.size.y <= 0f)
            capsule.size = new Vector2(1f, 1f);
        groundFxCapsule = capsule;

        groundFxTrigger = fxObject.GetComponent<PeaPodGroundFxTrigger>();
        if (groundFxTrigger == null)
            groundFxTrigger = fxObject.AddComponent<PeaPodGroundFxTrigger>();
        groundFxTrigger.Initialize(this);
    }

    private void ApplyGroundFxSizeFromCapsule(float targetDiameter)
    {
        if (groundFxCapsule == null)
            return;

        float clampedDiameter = Mathf.Max(0.1f, targetDiameter);
        groundFxCapsule.size = new Vector2(clampedDiameter, clampedDiameter);

        if (groundFxRenderer != null)
        {
            // GroundFX 시각 크기를 캡슐 콜라이더 크기와 동일하게 유지한다.
            groundFxRenderer.drawMode = SpriteDrawMode.Sliced;
            groundFxRenderer.size = groundFxCapsule.size;
            groundFxRenderer.transform.localScale = Vector3.one;
        }
    }

    private void SetGroundFxVisible(bool visible)
    {
        if (groundFxRenderer != null)
            groundFxRenderer.enabled = visible;
    }

    private void UpdateDynamicSorting()
    {
        if (peaRenderer == null)
            return;

        float pivotY = shadowTransform != null ? shadowTransform.position.y : transform.position.y;
        int order = ySortBaseOrder - Mathf.RoundToInt(pivotY * ySortScale) + ySortOrderOffset;
        peaRenderer.sortingLayerName = dynamicSortingLayer;
        peaRenderer.sortingOrder = order;
    }

    public void RegisterPlayerInGroundFx(Collider2D col)
    {
        if (col == null)
            return;

        PlayerStatControl player = col.GetComponent<PlayerStatControl>();
        if (player == null)
            player = col.GetComponentInParent<PlayerStatControl>();

        if (player != null)
            trackedPlayers.Add(player);
    }

    public void UnregisterPlayerInGroundFx(Collider2D col)
    {
        if (col == null)
            return;

        PlayerStatControl player = col.GetComponent<PlayerStatControl>();
        if (player == null)
            player = col.GetComponentInParent<PlayerStatControl>();

        if (player != null)
            trackedPlayers.Remove(player);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(targetPos == Vector2.zero ? (Vector2)transform.position : targetPos, explosionRadius);
    }
}

public class PeaPodGroundFxTrigger : MonoBehaviour
{
    private PeaPodDeathPea owner;

    public void Initialize(PeaPodDeathPea deathPea)
    {
        owner = deathPea;
    }

    private void Awake()
    {
        if (owner == null)
            owner = GetComponentInParent<PeaPodDeathPea>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (owner != null)
            owner.RegisterPlayerInGroundFx(collision);
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (owner != null)
            owner.UnregisterPlayerInGroundFx(collision);
    }
}
