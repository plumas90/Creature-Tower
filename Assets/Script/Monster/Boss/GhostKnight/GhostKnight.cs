using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GhostKnight : BossBase
{
    [Header("SO Reference")]
    [SerializeField] private GhostKnightSO ghostKnightSO;

    [Header("Visuals")]
    [SerializeField] private SpriteRenderer bodyRenderer;

    private Coroutine idleCoroutine;
    private Vector3 spawnPosition;
    public Vector3 SpawnPosition => spawnPosition;

    // =========================================================
    // Unity 생명주기
    // =========================================================

    private Transform armTransform;
    private Coroutine armRotationCoroutine;

    protected override void Awake()
    {
        base.Awake();
        spawnPosition = transform.position;

        if (ghostKnightSO != null)
            MainSO = ghostKnightSO;

        if (bodyRenderer == null)
            bodyRenderer = GetComponent<SpriteRenderer>();

        armTransform = FindRecursive(transform, "arm");
        if (armTransform == null)
        {
            Debug.LogWarning("[GhostKnight] 'arm' child transform not found.");
        }
    }

    private Transform FindRecursive(Transform parent, string name)
    {
        if (parent.name.Equals(name, System.StringComparison.OrdinalIgnoreCase))
            return parent;

        foreach (Transform child in parent)
        {
            Transform result = FindRecursive(child, name);
            if (result != null)
                return result;
        }
        return null;
    }

    private void FixedUpdate()
    {
        TickBehaviorTree();
    }

    // =========================================================
    // StatSet (초기화)
    // =========================================================

    public override void StatSet()
    {
        if (ghostKnightSO == null && MainSO is GhostKnightSO soFromMain)
            ghostKnightSO = soFromMain;

        if (ghostKnightSO != null)
            MainSO = ghostKnightSO;

        if (ghostKnightSO == null)
        {
            Debug.LogError("[GhostKnight] GhostKnightSO is not assigned.");
            return;
        }

        base.StatSet();
        bossCount = Mathf.Max(1, MainSO != null ? MainSO.bossCount : 1);

        StartIdleAnimation();
    }

    // =========================================================
    // BT 트리 생성
    // =========================================================

    protected override BossBTNode CreateBehaviorTree()
    {
        return new BossSelectorNode(
            new BossSequenceNode(
                new BossConditionNode(() => live && !wait),
                new BTTask_GhostKnightWait(this),
                new BossRandomNonConsecutiveSelectorNode(
                    new BTTask_GhostKnightSwordPattern(this),
                    new BTTask_GhostKnightHexagonPattern(this),
                    new BTTask_GhostKnightTargetedPattern(this, BTTask_GhostKnightTargetedPattern.TargetedPatternType.Pattern3),
                    new BTTask_GhostKnightTargetedPattern(this, BTTask_GhostKnightTargetedPattern.TargetedPatternType.Pattern4),
                    new BTTask_GhostKnightEscortPattern(this)
                )
            ),
            new BossActionNode(() => BossBTState.Running)
        );
    }

    public void SetVisibility(bool visible)
    {
        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
                renderers[i].enabled = visible;
        }

        Collider2D[] colliders = GetComponentsInChildren<Collider2D>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null)
                colliders[i].enabled = visible;
        }
    }

    // =========================================================
    // 애니메이션 및 방향 제어
    // =========================================================

    private void StartIdleAnimation()
    {
        if (idleCoroutine != null)
            StopCoroutine(idleCoroutine);
        idleCoroutine = StartCoroutine(CoIdleAnimation());
    }

    private void StopIdleAnimation()
    {
        if (idleCoroutine != null)
        {
            StopCoroutine(idleCoroutine);
            idleCoroutine = null;
        }
    }

    private IEnumerator CoIdleAnimation()
    {
        if (ghostKnightSO == null || ghostKnightSO.idleSprites == null || ghostKnightSO.idleSprites.Length == 0)
            yield break;

        float fps = Mathf.Max(1f, ghostKnightSO.idleAnimFps);
        float delay = 1f / fps;
        int frameIndex = 0;
        int direction = 1;

        while (live && !isDead)
        {
            if (bodyRenderer != null)
                bodyRenderer.sprite = ghostKnightSO.idleSprites[frameIndex];

            yield return new WaitForSeconds(delay);

            if (ghostKnightSO.idleSprites.Length > 1)
            {
                frameIndex += direction;
                if (frameIndex >= ghostKnightSO.idleSprites.Length)
                {
                    frameIndex = ghostKnightSO.idleSprites.Length - 2;
                    direction = -1;
                }
                else if (frameIndex < 0)
                {
                    frameIndex = 1;
                    direction = 1;
                }
            }
        }
    }

    // =========================================================
    // 사망 및 비활성화
    // =========================================================

    public override void BossDie()
    {
        if (isDead) return;

        StopIdleAnimation();
        ResetArm();
        base.BossDie();
        gameObject.SetActive(false);
    }

    protected override void OnDisable()
    {
        StopIdleAnimation();
        ResetArm();
        base.OnDisable();
    }

    // =========================================================
    // 인트로 애니메이션
    // =========================================================

    protected override void OnBeforeIntroStart()
    {
        base.OnBeforeIntroStart();
        StartCoroutine(CoGhostKnightIntroAnimation(IntroTime));
    }

    private IEnumerator CoGhostKnightIntroAnimation(float introTime)
    {
        List<GameObject> introSwords = new List<GameObject>();

        // 1. Rotate arm up to 90 degrees over 1.0s (or clamp to introTime if introTime is short)
        float armDuration = Mathf.Min(1.0f, introTime);
        StartRotateArmUp(armDuration);
        yield return new WaitForSeconds(armDuration);

        // 2. Spawn 4 swords if introTime is long enough
        if (introTime > armDuration && ghostKnightSO != null && ghostKnightSO.swordPrefab != null)
        {
            Vector3 basePos = transform.position;
            Vector3[] localOffsets = new Vector3[]
            {
                new Vector3(-3.0f, 3.0f, 0f),  // Top-Left
                new Vector3(3.0f, 3.0f, 0f),   // Top-Right
                new Vector3(-5.0f, -3.0f, 0f), // Bottom-Left
                new Vector3(5.0f, -3.0f, 0f)   // Bottom-Right
            };

            for (int i = 0; i < localOffsets.Length; i++)
            {
                Vector3 offset = localOffsets[i];
                Vector3 spawnPos = basePos + offset;
                GameObject swordObj = Instantiate(ghostKnightSO.swordPrefab, spawnPos, Quaternion.identity);
                swordObj.transform.localScale = new Vector3(1.5f, 1.5f, 1f);

                GhostKnightSword sword = swordObj.GetComponent<GhostKnightSword>();
                if (sword == null)
                {
                    sword = swordObj.AddComponent<GhostKnightSword>();
                }

                bool flipSprite = offset.x > 0f;
                // Dummy center far away to prevent self-destruction
                Vector3 dummyCenter = spawnPos + Vector3.up * 1000f;
                sword.InitializeLinearInward(dummyCenter, spawnPos, 0f, 0f, 720f, 0f, flipSprite);

                introSwords.Add(swordObj);
            }

            // 3. Wait for the remaining time
            float remaining = introTime - armDuration;
            if (remaining > 0f)
            {
                yield return new WaitForSeconds(remaining);
            }
        }

        // 4. Cleanup
        for (int i = 0; i < introSwords.Count; i++)
        {
            if (introSwords[i] != null)
            {
                Destroy(introSwords[i]);
            }
        }
        introSwords.Clear();

        ResetArm();
    }

    // =========================================================
    // Arm 회전 제어
    // =========================================================

    public void StartRotateArmUp(float duration)
    {
        if (armTransform == null) return;
        if (armRotationCoroutine != null)
            StopCoroutine(armRotationCoroutine);
        armRotationCoroutine = StartCoroutine(CoRotateArm(90f, duration));
    }

    public void StartRotateArmDown(float duration)
    {
        if (armTransform == null) return;
        if (armRotationCoroutine != null)
            StopCoroutine(armRotationCoroutine);
        armRotationCoroutine = StartCoroutine(CoRotateArm(0f, duration));
    }

    public void ResetArm()
    {
        if (armTransform == null) return;
        if (armRotationCoroutine != null)
        {
            StopCoroutine(armRotationCoroutine);
            armRotationCoroutine = null;
        }
        armTransform.localRotation = Quaternion.Euler(0f, 0f, 0f);
    }

    private IEnumerator CoRotateArm(float targetZ, float duration)
    {
        if (duration <= 0f)
        {
            armTransform.localRotation = Quaternion.Euler(0f, 0f, targetZ);
            yield break;
        }

        float startZ = armTransform.localEulerAngles.z;
        if (startZ > 180f) startZ -= 360f;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float currentZ = Mathf.Lerp(startZ, targetZ, t);
            armTransform.localRotation = Quaternion.Euler(0f, 0f, currentZ);
            yield return null;
        }

        armTransform.localRotation = Quaternion.Euler(0f, 0f, targetZ);
        armRotationCoroutine = null;
    }
}
