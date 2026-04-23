using UnityEngine;

[CreateAssetMenu(fileName = "CaptainCrabBossSO", menuName = "ScriptableObjects/Boss/CaptainCrabBoss")]
public class CaptainCrabBossSO : EnemySO
{
    [Header("Core")]
    public float faceDamageMultiplier = 1f;

    [Header("References")]
    public GameObject bubbleBeamProjectilePrefab;
    public GameObject bubbleBombPrefab;

    [Header("Pattern Loop")]
    [Tooltip("패턴 시작 간 최소 간격")]
    public float patternInterval = 1.2f;
    [Tooltip("같은 패턴 연속 허용 여부")]
    public bool allowSamePatternRepeat = false;
    [Tooltip("같은 패턴 최대 연속 횟수")]
    public int maxConsecutiveSamePattern = 1;

    [Header("Pattern Weights")]
    public float guardWeight = 1f;
    public float clawSweepWeight = 1f;
    public float bubbleBeamWeight = 1f;

    [Header("Guard Pattern")]
    [Tooltip("팔이 막기 자세로 이동하는 시간")]
    public float guardMoveDuration = 0.2f;
    [Tooltip("막기 시 팔이 내려오는 Y 오프셋(로컬)")]
    public float guardLowerYOffset = 0.2f;
    [Tooltip("막기 시 양팔을 중앙(0축)으로 당기는 거리(절댓값, 로컬)")]
    public float guardCoverCenterX = 0.22f;
    public float guardDuration = 1.1f;
    public float faceExposeDuration = 1.4f;

    [Header("Claw Sweep Pattern")]
    public float clawSweepTelegraphTime = 0.45f;
    public float clawSweepActiveTime = 0.8f;
    public float clawSweepRecoverTime = 0.2f;
    [Tooltip("휘두르기 시작 전 뒤로 당기는 각도(도)")]
    public float clawSweepWindupAngle = 18f;
    [Tooltip("휘두르는 목표 각도(도)")]
    public float clawSweepStrikeAngle = 48f;
    [Tooltip("복귀 후 양팔을 중앙으로 추가 수렴시키는 거리")]
    public float clawSweepClashInwardDistance = 0.35f;
    [Tooltip("복귀 후 중앙 수렴에 걸리는 시간")]
    public float clawSweepClashDuration = 0.15f;
    [Tooltip("서로 부딪힌 자세를 유지하는 시간")]
    public float clawSweepClashHoldTime = 0.08f;
    public float clawSweepDamage = 14f;

    [Header("Bubble Beam Pattern")]
    public float beamTelegraphTime = 0.35f;
    public int beamShotCount = 5;
    public float beamShotInterval = 0.12f;
    public float beamSpreadAngle = 12f;
    public float beamProjectileSpeed = 8.5f;
    public float beamProjectileLifetime = 4f;
    public float beamProjectileDamage = 10f;

    [Header("Ambient Bubble Bomb")]
    public float bombSpawnInterval = 1.8f;
    public int maxActiveBombs = 5;
    public float bombAirMoveSpeed = 3.6f;
    public float bombDescendSpeed = 3.2f;
    public float bombVerticalSweepSpeed = 2.7f;
    public float bombRollSpeed = 3.5f;
    public float bombDamage = 12f;
    public float bombLifetime = 14f;
    public float bombShadowContactThreshold = 0.03f;

    [Header("Bounds Offsets")]
    public float leftEdgeOffset = 0f;
    public float rightEdgeOffset = 0f;
    public float topEdgeOffset = 0f;
    public float bottomEdgeOffset = 0f;
}

