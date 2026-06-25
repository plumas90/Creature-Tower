using UnityEngine;

public enum MeleeAttackType
{
    Thrust, // 찌르기
    Swing   // 휘두르기
}

/// <summary>
/// 무기를 휘두르는/찌르는 타입의 근접 몬스터 전용 ScriptableObject.
/// </summary>
[CreateAssetMenu(fileName = "MeleeSwingEnemySO", menuName = "ScriptableObject/MeleeSwingEnemySO", order = 3)]
public class MeleeSwingEnemySO : EnemySO
{
    [Header("근접 공격 설정")]
    [Tooltip("근접 공격 타입 (찌르기 또는 휘두르기)")]
    public MeleeAttackType attackType = MeleeAttackType.Thrust;

    [Tooltip("무기 공격 사거리 (이 거리 이내로 접근 시 경고 및 타격 개시)")]
    public float attackRange = 1.6f;

    [Tooltip("공격 쿨타임 (휘두른 후 대기 시간, 초)")]
    public float attackCooldown = 2.0f;

    [Tooltip("공격 전 빨간색 경고 궤적 실선/범위 노출 시간 (초)")]
    public float attackWarningTime = 0.8f;

    [Header("스윙 및 찌르기 상세 오프셋 설정")]
    [Tooltip("스윙 공격 시 회전 중심축을 몸 바깥쪽으로 밀어내는 오프셋 거리 (플레이어 방향으로 이동, 기본값 0.4f)")]
    public float swingPivotOffset = 0.4f;

    [Tooltip("스윙 공격 시 휘두르는 반경(반지름)에 더해지는 추가 거리 (기본값 0.3f)")]
    public float swingRadiusBonus = 0.3f;

    [Tooltip("찌르기 공격 시 대기하는 오프셋 거리 (음수면 뒤로 당김, 양수면 앞으로 내밈, 기본값 -0.15f)")]
    public float thrustPrepOffset = -0.15f;
}
