using UnityEngine;
using System.Collections;

public class PeaPodBoss : BossBase
{
    [Header("SO Reference")]
    [SerializeField] private PeaPodBossSO peaPodSO;

    [Header("Visuals & Sprites")]
    [SerializeField] private SpriteRenderer angryHeadSr;
    [SerializeField] private SpriteRenderer sadHeadSr;
    [SerializeField] private SpriteRenderer normalHeadSr;
    [SerializeField] private SpriteRenderer hitboxInSr;
    [SerializeField] private SpriteRenderer outSr;

    [Header("Animation Frames")]
    [SerializeField] private Sprite[] angryHeadSprites; // size 4
    [SerializeField] private Sprite[] sadHeadSprites;   // size 4
    [SerializeField] private Sprite[] normalHeadSprites; // size 3
    [SerializeField] private Sprite[] caseInSprites;     // size 8
    [SerializeField] private Sprite[] caseOutSprites;    // size 8

    private Coroutine headAnimCoroutine;
    private int currentHeadState = 0; // 0: angry, 1: sad, 2: normal

    protected override void Awake()
    {
        base.Awake();

        if (peaPodSO != null)
            MainSO = peaPodSO;
    }

    public override void StatSet()
    {
        // 프리팹 필드 누락 시 MainSO에서 역으로 복구한다.
        if (peaPodSO == null && MainSO is PeaPodBossSO soFromMain)
            peaPodSO = soFromMain;

        if (peaPodSO != null)
            MainSO = peaPodSO;

        if (peaPodSO == null)
        {
            Debug.LogError("[PeaPodBoss] PeaPodBossSO is not assigned.");
            return;
        }

        base.StatSet();
        bossCount = Mathf.Max(1, MainSO != null ? MainSO.bossCount : 1);
    }

    private void FixedUpdate()
    {
        TickBehaviorTree();
    }

    protected override BossBTNode CreateBehaviorTree()
    {
        PeaPodBossSO so = peaPodSO != null ? peaPodSO : MainSO as PeaPodBossSO;
        if (so == null)
        {
            Debug.LogError("[PeaPodBoss] CreateBehaviorTree failed: PeaPodBossSO is null.");
            return new BossActionNode(() => BossBTState.Running);
        }

        if (so.vineSegmentPrefab == null)
        {
            Debug.LogError("[PeaPodBoss] CreateBehaviorTree failed: vineSegmentPrefab is not assigned.");
            return new BossActionNode(() => BossBTState.Running);
        }

        return new BossSelectorNode(
            new BossSequenceNode(
                new BossConditionNode(() => live && !wait),
                new BTTask_PeaPodGrowVineChain(this, so),
                new BTTask_Wait(this, so.attackInterval)
            ),
            new BossActionNode(() => BossBTState.Running)
        );
    }

    public override void BossDie()
    {
        if (isDead)
            return;

        SpawnDeathPeas();
        base.BossDie();
        gameObject.SetActive(false);
    }

    private void SpawnDeathPeas()
    {
        if (peaPodSO == null || peaPodSO.deathPeaPrefab == null || StageOwner == null)
            return;

        int count = Mathf.Max(0, peaPodSO.deathPeaCount);
        for (int i = 0; i < count; i++)
        {
            Vector2 target = StageOwner.GetRandomPositionInZone();
            GameObject peaObj = Instantiate(peaPodSO.deathPeaPrefab, transform.position, Quaternion.identity);
            PeaPodDeathPea pea = peaObj.GetComponent<PeaPodDeathPea>();
            if (pea == null)
                pea = peaObj.AddComponent<PeaPodDeathPea>();

            // 스프라이트를 순서대로 지정 (angry, sad, normal)
            Sprite bombSprite = null;
            int type = i % 3;
            if (type == 0 && angryHeadSprites != null && angryHeadSprites.Length > 0)
                bombSprite = angryHeadSprites[0];
            else if (type == 1 && sadHeadSprites != null && sadHeadSprites.Length > 0)
                bombSprite = sadHeadSprites[0];
            else if (type == 2 && normalHeadSprites != null && normalHeadSprites.Length > 0)
                bombSprite = normalHeadSprites[0];

            pea.Initialize(
                target,
                peaPodSO.deathPeaRiseDuration,
                peaPodSO.deathPeaFallDuration,
                peaPodSO.deathPeaArcHeight,
                peaPodSO.deathPeaLandedWaitDuration,
                peaPodSO.deathPeaRedWarningDuration,
                peaPodSO.deathPeaExplosionDamage,
                peaPodSO.deathPeaExplosionRadius,
                peaPodSO.deathPeaGroundFxRadiusMultiplier,
                bombSprite
            );
        }
    }

    protected override float ResolveIntroTime()
    {
        return 2f; // 인트로 2초
    }

    public override void OnBossActivatedBeforeIntro()
    {
        base.OnBossActivatedBeforeIntro();
        ResetHeadsToDefault();
    }

    protected override void OnBeforeIntroStart()
    {
        base.OnBeforeIntroStart();
        StartCoroutine(CoIntroAnimation());
    }

    public override void First()
    {
        base.First();
        
        if (headAnimCoroutine != null)
            StopCoroutine(headAnimCoroutine);
        headAnimCoroutine = StartCoroutine(CoHeadAnimationRoutine());
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        if (headAnimCoroutine != null)
        {
            StopCoroutine(headAnimCoroutine);
            headAnimCoroutine = null;
        }
    }

    private IEnumerator CoIntroAnimation()
    {
        if (hitboxInSr != null && caseInSprites != null && caseInSprites.Length > 0)
            hitboxInSr.sprite = caseInSprites[0];
        if (outSr != null && caseOutSprites != null && caseOutSprites.Length > 0)
            outSr.sprite = caseOutSprites[0];

        // 12fps 속도로 8프레임 재생
        float frameDelay = 1f / 12f;
        int maxFrames = 8;

        for (int i = 0; i < maxFrames; i++)
        {
            if (hitboxInSr != null && caseInSprites != null && i < caseInSprites.Length)
                hitboxInSr.sprite = caseInSprites[i];

            if (outSr != null && caseOutSprites != null && i < caseOutSprites.Length)
                outSr.sprite = caseOutSprites[i];

            yield return new WaitForSeconds(frameDelay);
        }

        // 인트로 완료 후 마지막 프레임 고정
        if (hitboxInSr != null && caseInSprites != null && caseInSprites.Length >= 8)
            hitboxInSr.sprite = caseInSprites[7];
        if (outSr != null && caseOutSprites != null && caseOutSprites.Length >= 8)
            outSr.sprite = caseOutSprites[7];
    }

    private IEnumerator CoHeadAnimationRoutine()
    {
        ResetHeadsToDefault();
        currentHeadState = 0; // 0: angry, 1: sad, 2: normal

        while (live && !isDead)
        {
            yield return new WaitForSeconds(2f);

            if (currentHeadState == 0)
            {
                yield return StartCoroutine(CoPlayAngryHead());
                currentHeadState = 1;
            }
            else if (currentHeadState == 1)
            {
                yield return StartCoroutine(CoPlaySadHead());
                currentHeadState = 2;
            }
            else if (currentHeadState == 2)
            {
                yield return StartCoroutine(CoPlayNormalHead());
                currentHeadState = 0;
            }
        }
    }

    private void ResetHeadsToDefault()
    {
        if (angryHeadSr != null && angryHeadSprites != null && angryHeadSprites.Length > 0)
            angryHeadSr.sprite = angryHeadSprites[0];
        if (sadHeadSr != null && sadHeadSprites != null && sadHeadSprites.Length > 0)
            sadHeadSr.sprite = sadHeadSprites[0];
        if (normalHeadSr != null && normalHeadSprites != null && normalHeadSprites.Length > 0)
            normalHeadSr.sprite = normalHeadSprites[0];
    }

    private IEnumerator CoPlayAngryHead()
    {
        if (angryHeadSr == null || angryHeadSprites == null || angryHeadSprites.Length < 4) yield break;

        float delay = 1f / 10f; // 10fps
        for (int i = 0; i < 4; i++)
        {
            angryHeadSr.sprite = angryHeadSprites[i];
            yield return new WaitForSeconds(delay);
        }
        angryHeadSr.sprite = angryHeadSprites[0];
    }

    private IEnumerator CoPlaySadHead()
    {
        if (sadHeadSr == null || sadHeadSprites == null || sadHeadSprites.Length < 4) yield break;

        float delay = 1f / 10f; // 10fps
        for (int i = 0; i < 4; i++)
        {
            sadHeadSr.sprite = sadHeadSprites[i];
            yield return new WaitForSeconds(delay);
        }
        sadHeadSr.sprite = sadHeadSprites[0];
    }

    private IEnumerator CoPlayNormalHead()
    {
        if (normalHeadSr == null || normalHeadSprites == null || normalHeadSprites.Length < 3) yield break;

        float delay = 1f / 10f; // 10fps
        int[] frames = { 0, 1, 2, 1, 0 };
        for (int i = 0; i < frames.Length; i++)
        {
            normalHeadSr.sprite = normalHeadSprites[frames[i]];
            yield return new WaitForSeconds(delay);
        }
        normalHeadSr.sprite = normalHeadSprites[0];
    }
}
