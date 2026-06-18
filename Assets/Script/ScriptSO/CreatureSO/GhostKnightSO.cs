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
}
