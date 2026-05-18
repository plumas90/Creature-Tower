using UnityEngine;

/// <summary>
/// 원거리 일반 몬스터 전용 ScriptableObject.
/// EnemySO 공통 스탯(hp, atk, speed) 위에 원거리 전용 파라미터를 추가한다.
/// </summary>
[CreateAssetMenu(fileName = "RangedEnemySO", menuName = "ScriptableObject/RangedEnemySO", order = 2)]
public class RangedEnemySO : EnemySO
{
    [Header("원거리 전용")]
    [Tooltip("공격 사거리 (이 거리 안에 들어와야 발사)")] 
    public float attackRange = 8f;

    [Tooltip("최소 유지 거리 (이보다 가까우면 뒤로 물러남)")]
    public float keepDistance = 4f;

    [Tooltip("공격 딜레이 (초, 낮을수록 빨리 쏨)")]
    public float attackCooldown = 2f;

    [Tooltip("1회 발사당 투사체 수")]
    [Range(1, 12)]
    public int projectileCount = 1;

    [Tooltip("다탄두 시 탄 간격 각도 (degrees)")]
    public float spreadAngle = 15f;

    [Tooltip("투사체 속도")]
    public float projectileSpeed = 8f;

    [Tooltip("투사체 사거리(수명 초)")]
    public float projectileLifeTime = 2f;

    [Tooltip("투사체 프리팹 (Bullet 컴포넌트 필수)")]
    public GameObject projectilePrefab;
}
