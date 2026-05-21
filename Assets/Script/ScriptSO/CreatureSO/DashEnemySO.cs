using UnityEngine;

/// <summary>
/// 돌진형 몬스터 전용 ScriptableObject.
/// 플레이어에게 다가가다 사거리에 진입하면 N초 조준 대기 후 고속 돌진한다.
/// </summary>
[CreateAssetMenu(fileName = "DashEnemySO", menuName = "ScriptableObject/DashEnemySO", order = 4)]
public class DashEnemySO : EnemySO
{
    [Header("돌진 행동 설정")]

    [Tooltip("돌진을 시작하는 플레이어와의 거리 (이 이내 진입 시 조준 대기 시작)")]
    public float dashRange = 3.5f;

    [Tooltip("조준 대기 시간 (초). 이 동안 빨간 예고선을 표시하며 플레이어 방향을 계속 추적함")]
    public float windupTime = 1.2f;

    [Tooltip("돌진 속도 (일반 speed보다 훨씬 빠르게 설정 권장)")]
    public float dashSpeed = 14f;

    [Tooltip("최대 돌진 거리. 이 거리를 초과하면 충돌 없이도 자동 정지됨 (무한 돌진 방지)")]
    public float dashMaxDistance = 15f;

    [Tooltip("돌진 후 기절(Cooldown) 대기 시간 (초)")]
    public float dashCooldown = 1.5f;
}
