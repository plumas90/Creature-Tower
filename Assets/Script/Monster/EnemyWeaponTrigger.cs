using UnityEngine;

/// <summary>
/// 몬스터의 무기/타격 궤적 오브젝트에 부착하여 플레이어와의 충돌을 감지하는 컴포넌트.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class EnemyWeaponTrigger : MonoBehaviour
{
    private MeleeSwingEnemy _owner;
    private Collider2D _collider;

    public void Init(MeleeSwingEnemy owner)
    {
        _owner = owner;
        _collider = GetComponent<Collider2D>();
        if (_collider != null)
        {
            _collider.isTrigger = true;
            _collider.enabled = false; // 평소에는 꺼둠
        }
    }

    public void SetActiveTrigger(bool active)
    {
        if (_collider != null)
        {
            _collider.enabled = active;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_owner == null) return;

        // 플레이어 태그 확인 및 무적 처리를 적용하기 위한 데미지 가함
        if (other.CompareTag("Player"))
        {
            _owner.OnWeaponHit(other.gameObject);
        }
    }
}
