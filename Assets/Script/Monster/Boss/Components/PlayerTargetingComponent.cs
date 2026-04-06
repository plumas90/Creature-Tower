using UnityEngine;

/// <summary>
/// 플레이어 추적 컴포넌트: 플레이어 탐지, 거리 계산, 방향 계산 등을 담당.
/// 여러 보스에서 재사용 가능한 독립 컴포넌트.
/// </summary>
public class PlayerTargetingComponent : MonoBehaviour
{
    [Header("Target Settings")]
    [Tooltip("추적 대상 (비어있으면 GameManager에서 자동 탐색)")]
    [SerializeField] private GameObject targetPlayer;

    [Header("Range Settings")]
    [Tooltip("추적 시작 거리 (이 거리 안에 들어오면 추적)")]
    [SerializeField] [Min(0f)] private float detectionRange = 15f;

    [Tooltip("추적 중단 거리 (이 거리 밖으로 벗어나면 추적 중단)")]
    [SerializeField] [Min(0f)] private float loseTargetRange = 20f;

    [Header("Debug")]
    [SerializeField] private bool enableDebug = false;

    // 내부 상태
    private Transform cachedPlayerTransform;
    private bool isTargetValid;

    /// <summary>
    /// 현재 타겟이 유효한지 (null이 아니고 활성화되어 있는지)
    /// </summary>
    public bool IsTargetValid => isTargetValid;

    /// <summary>
    /// 현재 타겟 GameObject
    /// </summary>
    public GameObject Target => targetPlayer;

    /// <summary>
    /// 현재 타겟 Transform
    /// </summary>
    public Transform TargetTransform => cachedPlayerTransform;

    /// <summary>
    /// 탐지 범위
    /// </summary>
    public float DetectionRange => detectionRange;

    /// <summary>
    /// 타겟 상실 범위
    /// </summary>
    public float LoseTargetRange => loseTargetRange;

    private void Awake()
    {
        UpdateTargetCache();
    }

    private void Update()
    {
        UpdateTargetCache();
    }

    /// <summary>
    /// 타겟 캐시를 업데이트한다.
    /// </summary>
    private void UpdateTargetCache()
    {
        // 타겟이 없으면 GameManager에서 찾기
        if (targetPlayer == null)
        {
            if (GameManager.Instance != null)
            {
                targetPlayer = GameManager.Instance.playerOBJ;
                if (enableDebug && targetPlayer != null)
                    Debug.Log($"[PlayerTargeting] Found player from GameManager: {targetPlayer.name}");
            }
        }

        // 타겟 유효성 체크
        if (targetPlayer != null && targetPlayer.activeInHierarchy)
        {
            if (cachedPlayerTransform == null)
                cachedPlayerTransform = targetPlayer.transform;

            isTargetValid = true;
        }
        else
        {
            cachedPlayerTransform = null;
            isTargetValid = false;
            
            if (enableDebug)
                Debug.LogWarning($"[PlayerTargeting] Player target invalid or inactive!");
        }
    }

    /// <summary>
    /// 타겟까지의 거리를 반환한다.
    /// </summary>
    public float GetDistanceToTarget()
    {
        if (!isTargetValid)
            return float.MaxValue;

        return Vector2.Distance(transform.position, cachedPlayerTransform.position);
    }

    /// <summary>
    /// 타겟까지의 거리 제곱을 반환한다. (거리 비교 시 sqrt 계산 생략)
    /// </summary>
    public float GetSqrDistanceToTarget()
    {
        if (!isTargetValid)
            return float.MaxValue;

        return ((Vector2)cachedPlayerTransform.position - (Vector2)transform.position).sqrMagnitude;
    }

    /// <summary>
    /// 타겟 방향 벡터를 반환한다 (정규화됨).
    /// </summary>
    public Vector2 GetDirectionToTarget()
    {
        if (!isTargetValid)
            return Vector2.zero;

        return ((Vector2)cachedPlayerTransform.position - (Vector2)transform.position).normalized;
    }

    /// <summary>
    /// 타겟 방향 벡터를 반환한다 (정규화 안 됨).
    /// </summary>
    public Vector2 GetVectorToTarget()
    {
        if (!isTargetValid)
            return Vector2.zero;

        return (Vector2)cachedPlayerTransform.position - (Vector2)transform.position;
    }

    /// <summary>
    /// 타겟이 탐지 범위 안에 있는지 확인한다.
    /// </summary>
    public bool IsTargetInRange()
    {
        if (!isTargetValid)
            return false;

        float sqrDistance = GetSqrDistanceToTarget();
        return sqrDistance <= detectionRange * detectionRange;
    }

    /// <summary>
    /// 타겟이 특정 범위 안에 있는지 확인한다.
    /// </summary>
    public bool IsTargetInRange(float range)
    {
        if (!isTargetValid)
            return false;

        float sqrDistance = GetSqrDistanceToTarget();
        return sqrDistance <= range * range;
    }

    /// <summary>
    /// 타겟이 범위 밖으로 벗어났는지 확인한다.
    /// </summary>
    public bool IsTargetOutOfRange()
    {
        if (!isTargetValid)
            return true;

        float sqrDistance = GetSqrDistanceToTarget();
        return sqrDistance > loseTargetRange * loseTargetRange;
    }

    /// <summary>
    /// 타겟을 향해 회전할 각도를 반환한다 (Degree).
    /// </summary>
    public float GetAngleToTarget()
    {
        if (!isTargetValid)
            return 0f;

        Vector2 direction = GetDirectionToTarget();
        return Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
    }

    /// <summary>
    /// 타겟을 수동으로 설정한다.
    /// </summary>
    public void SetTarget(GameObject target)
    {
        targetPlayer = target;
        UpdateTargetCache();
    }

    /// <summary>
    /// 타겟을 제거한다.
    /// </summary>
    public void ClearTarget()
    {
        targetPlayer = null;
        cachedPlayerTransform = null;
        isTargetValid = false;
    }

    /// <summary>
    /// 디버그용 Gizmo 그리기
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (!enableDebug)
            return;

        // 탐지 범위 (녹색)
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // 타겟 상실 범위 (빨간색)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, loseTargetRange);

        // 타겟으로의 선
        if (isTargetValid && cachedPlayerTransform != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, cachedPlayerTransform.position);
        }
    }
}
