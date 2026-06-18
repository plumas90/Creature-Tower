using UnityEngine;

[CreateAssetMenu(fileName = "GhostKnightSO", menuName = "ScriptableObjects/Boss/GhostKnight")]
public class GhostKnightSO : EnemySO
{
    [Header("Animation")]
    [Tooltip("기본 대기 FPS")]
    public float idleAnimFps = 8f;

    [Tooltip("대기 스프라이트 배열")]
    public Sprite[] idleSprites;

    [Header("Sword Pattern Settings")]
    [Tooltip("검 프리팹")]
    public GameObject swordPrefab;

    [Tooltip("검 공격 공격력")]
    public float swordDamage = 15f;

    [Tooltip("진자 운동 1왕복 주기(초)")]
    public float swordSwingPeriod = 3f;

    [Tooltip("검 자체 Z축 회전 속도 (도/초)")]
    public float swordRotationSpeed = 360f;

    [Tooltip("검 왕복(좌우 이동) 횟수")]
    public int swordSwingCount = 3;

    [Header("Hexagon Pattern Settings (Pattern 2)")]
    [Tooltip("헥사곤 패턴 시작 거리 (반지름)")]
    public float hexagonPatternRadius = 4f;

    [Tooltip("헥사곤 검 이동 속도")]
    public float hexagonSwordSpeed = 4f;

    [Tooltip("헥사곤 검 회전 속도 (도/초)")]
    public float hexagonSwordRotationSpeed = 360f;

    [Tooltip("헥사곤 검 공격력")]
    public float hexagonSwordDamage = 15f;

    [Tooltip("헥사곤 패턴 발동 횟수 (웨이브 수)")]
    public int hexagonPatternCount = 4;

    [Tooltip("웨이브 소환 간격(초)")]
    public float hexagonWaveInterval = 1.5f;

    [Tooltip("헥사곤 검의 XY 스케일 값")]
    public float hexagonSwordScale = 1.5f;

    [Tooltip("헥사곤 패턴 다각형 꼭짓점 수 (N각형)")]
    public int hexagonVertexCount = 10;

    [Header("Targeted Pattern Settings (Pattern 3)")]
    [Tooltip("타겟 패턴 시작 거리 (반지름)")]
    public float targetedPatternRadius = 3f;

    [Tooltip("타겟 검 발사 대기 시간 (초)")]
    public float targetedSwordLaunchDelay = 1.5f;

    [Tooltip("타겟 검 이동 속도")]
    public float targetedSwordSpeed = 8f;

    [Tooltip("타겟 검 공격력")]
    public float targetedSwordDamage = 15f;

    [Tooltip("한 클러스터당 검의 수")]
    public int targetedSwordCount = 3;

    [Tooltip("검 사이의 각도 차이")]
    public float targetedAngleStep = 10f;

    [Tooltip("타겟 패턴 발동 횟수 (웨이브 수)")]
    public int targetedPatternCount = 3;

    [Tooltip("웨이브 소환 간격(초)")]
    public float targetedWaveInterval = 2.0f;

    [Header("Targeted Pattern Settings (Pattern 4)")]
    [Tooltip("패턴 4 시작 거리 (반지름)")]
    public float targetedPatternRadius4 = 3f;

    [Tooltip("패턴 4 검 발사 대기 시간 (초)")]
    public float targetedSwordLaunchDelay4 = 1.5f;

    [Tooltip("패턴 4 검 이동 속도")]
    public float targetedSwordSpeed4 = 8f;

    [Tooltip("패턴 4 검 공격력")]
    public float targetedSwordDamage4 = 15f;

    [Tooltip("패턴 4 한 클러스터당 검의 수")]
    public int targetedSwordCount4 = 3;

    [Tooltip("패턴 4 검 사이의 각도 차이")]
    public float targetedAngleStep4 = 10f;

    [Tooltip("패턴 4 패턴 발동 횟수 (웨이브 수)")]
    public int targetedPatternCount4 = 3;

    [Tooltip("패턴 4 웨이브 소환 간격(초)")]
    public float targetedWaveInterval4 = 2.0f;

    [Header("Global Cooldown Setting")]
    [Tooltip("패턴 간 전역 재사용 대기시간(초)")]
    public float globalCooldown = 2.0f;

    [Header("Escort Pattern Settings (Pattern 5)")]
    [Tooltip("에스코트 패턴 검 배리어 반지름")]
    public float escortPatternRadius = 3.5f;

    [Tooltip("에스코트 패턴 검 배리어 개수")]
    public int escortSwordCount = 8;

    [Tooltip("에스코트 패턴 보스 이동 속도")]
    public float escortBossSpeed = 3.5f;

    [Tooltip("에스코트 패턴 이동 횟수")]
    public int escortMoveCount = 3;

    [Tooltip("에스코트 패턴 검 공격력")]
    public float escortSwordDamage = 15f;

    [Tooltip("에스코트 패턴 순간이동 후 대기 경고 시간(초)")]
    public float escortWarningDuration = 1.0f;

    [Tooltip("에스코트 패턴 각 이동 사이 전환 시간(초)")]
    public float escortTransitionDuration = 0.5f;

    [Tooltip("에스코트 패턴 맵 테두리 안전 거리 (패딩)")]
    public float escortMapPadding = 1.5f;

    [Tooltip("에스코트 패턴 검의 XY 스케일 값")]
    public float escortSwordScale = 1.5f;
}
