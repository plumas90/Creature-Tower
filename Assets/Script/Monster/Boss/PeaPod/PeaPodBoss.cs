using UnityEngine;

public class PeaPodBoss : BossBase
{
    [Header("SO Reference")]
    [SerializeField] private PeaPodBossSO peaPodSO;

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

            pea.Initialize(
                target,
                peaPodSO.deathPeaRiseDuration,
                peaPodSO.deathPeaFallDuration,
                peaPodSO.deathPeaArcHeight,
                peaPodSO.deathPeaLandedWaitDuration,
                peaPodSO.deathPeaRedWarningDuration,
                peaPodSO.deathPeaExplosionDamage,
                peaPodSO.deathPeaExplosionRadius,
                peaPodSO.deathPeaGroundFxRadiusMultiplier
            );
        }
    }
}
