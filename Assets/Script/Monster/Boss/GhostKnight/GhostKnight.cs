using System.Collections;
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

    protected override void Awake()
    {
        base.Awake();
        spawnPosition = transform.position;

        if (ghostKnightSO != null)
            MainSO = ghostKnightSO;

        if (bodyRenderer == null)
            bodyRenderer = GetComponent<SpriteRenderer>();
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
                new BTTask_GhostKnightSwordPattern(this),
                new BTTask_Wait(this, 3f),
                new BTTask_GhostKnightHexagonPattern(this),
                new BTTask_Wait(this, 3f)
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
        base.BossDie();
        gameObject.SetActive(false);
    }

    protected override void OnDisable()
    {
        StopIdleAnimation();
        base.OnDisable();
    }
}
