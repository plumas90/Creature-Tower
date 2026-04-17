using UnityEngine;

[CreateAssetMenu(fileName = "PeaPodBossSO", menuName = "ScriptableObjects/Boss/PeaPodBoss")]
public class PeaPodBossSO : EnemySO
{
    [Header("Vine Chain Attack")]
    [Tooltip("줄기 세그먼트 프리팹")]
    public GameObject vineSegmentPrefab;

    [Tooltip("세그먼트 성장 속도 (unit/s)")]
    public float vineGrowthSpeed = 3.5f;

    [Tooltip("세그먼트 최대 길이")]
    public float vineMaxLength = 1.8f;

    [Tooltip("세그먼트 유지 시간 (완성 후)")]
    public float vineLifetime = 4f;

    [Tooltip("연속 생성 가능한 최대 세그먼트 수")]
    public int maxVineSegments = 8;

    [Tooltip("세그먼트마다 꺾이는 각도 (도)")]
    public float curveDegreesPerSegment = 16f;

    [Tooltip("랜덤 곡률 방향 사용 여부")]
    public bool randomizeCurveDirection = true;

    [Tooltip("랜덤을 끈 경우의 곡률 방향 (+1 또는 -1)")]
    public int fixedCurveDirectionSign = 1;

    [Tooltip("다음 체인 공격까지 대기 시간")]
    public float attackInterval = 1.2f;

    [Tooltip("줄기 끝 벽 판정 반경")]
    public float vineWallProbeRadius = 0.15f;

    [Tooltip("줄기 끝 벽 판정 레이어")]
    public LayerMask vineWallMask;

    [Tooltip("각 줄기 생성 간 대기 시간")]
    public float vineChainCooldown = 0.35f;

    [Tooltip("다음 줄기가 플레이어 방향으로 꺾일 수 있는 최대 각도(도)")]
    public float maxTurnTowardPlayerDegrees = 20f;

    [Header("Vine Damage")]
    [Tooltip("줄기 접촉 데미지 배율 (최종: 보스 atk * 배율)")]
    public float vineDamageMultiplier = 1f;

    [Tooltip("스프라이트 높이 대비 콜라이더 높이 배율 (1이면 동일)")]
    public float vineColliderHeightRatio = 0.9f;

    [Header("Death Pea Explosion")]
    [Tooltip("사망 시 생성되는 완두콩 프리팹")]
    public GameObject deathPeaPrefab;

    [Tooltip("사망 시 생성 개수")]
    public int deathPeaCount = 3;

    [Tooltip("완두콩 상승 시간 (포물선 전반)")]
    public float deathPeaRiseDuration = 1.5f;

    [Tooltip("완두콩 하강 시간 (포물선 후반)")]
    public float deathPeaFallDuration = 1.5f;

    [Tooltip("완두콩 체공 높이")]
    public float deathPeaArcHeight = 1.6f;

    [Tooltip("착지 후 대기 시간")]
    public float deathPeaLandedWaitDuration = 3f;

    [Tooltip("폭발 직전 빨간색 경고 시간")]
    public float deathPeaRedWarningDuration = 1f;

    [Tooltip("폭발 데미지")]
    public float deathPeaExplosionDamage = 25f;

    [Tooltip("폭발 반경")]
    public float deathPeaExplosionRadius = 1.8f;

    [Tooltip("GroundFX 반경 배율 (그림자 대비 크게)")]
    public float deathPeaGroundFxRadiusMultiplier = 1.35f;
}
