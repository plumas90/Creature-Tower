using UnityEngine;

[CreateAssetMenu(fileName = "HauntedCrystalBallSO", menuName = "ScriptableObjects/Boss/HauntedCrystalBall")]
public class HauntedCrystalBallSO : EnemySO
{
    [Header("Boss Damage")]
    [Tooltip("보스가 받는 데미지 (1로 고정)")]
    public float incomingDamageOverride = 1f;

    [Header("Pattern 1 - Cross Ghost (+ 방향)")]
    [Tooltip("십자 방향 유령 데미지")]
    public float pattern1Damage = 10f;

    [Tooltip("십자 유령 이동 속도")]
    public float pattern1GhostSpeed = 5f;

    [Header("Pattern 2 - X Ghost (X 방향)")]
    [Tooltip("X자 방향 유령 데미지")]
    public float pattern2Damage = 12f;

    [Tooltip("X자 유령 이동 속도")]
    public float pattern2GhostSpeed = 5f;

    [Header("Pattern 3 - Rotating Circles")]
    [Tooltip("회전구 데미지")]
    public float pattern3Damage = 15f;

    [Tooltip("회전구 최소 거리")]
    public float pattern3SpawnDistanceMin = 1.5f;

    [Tooltip("회전구 최대 거리")]
    public float pattern3SpawnDistanceMax = 3f;

    [Tooltip("회전구 소환 후 대기 시간 (초)")]
    public float pattern3WaitTime = 0.5f;

    [Tooltip("회전구 회전 속도 (도/초)")]
    public float pattern3RotationSpeed = 180f;

    [Header("Pattern 4 - Random Tiles")]
    [Tooltip("타일 데미지")]
    public float pattern4Damage = 20f;

    [Tooltip("타일 생성 개수")]
    public int pattern4TileCount = 2;

    [Tooltip("타일 경고 시간 (초, 스프라이트 변경 전)")]
    public float pattern4WarningTime = 1f;

    [Tooltip("타일 데미지 활성 시간 (초)")]
    public float pattern4ActiveTime = 0.5f;

    [Header("Prefab References")]
    [Tooltip("유령 발사체 프리팹 (패턴 1, 2)")]
    public GameObject ghostPrefab;

    [Tooltip("회전 유령구 프리팹 (패턴 3)")]
    public GameObject ghostCirclePrefab;

    [Tooltip("타일 프리팹 (패턴 4)")]
    public GameObject tilePrefab;
}
