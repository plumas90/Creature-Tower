using UnityEngine;

/// <summary>
/// 가장 기본적인 근접 공격형 일반 몬스터.
/// - 매 프레임 플레이어를 향해 물리 이동한다.
/// - 플레이어와의 충돌 데미지는 CreatureBase → PlayerStatControl.TryApplyContactDamage()로 위임.
/// - 애니메이터와 스프라이트 반전은 선택적으로 동작하며 없어도 정상 작동한다.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class NormalMeleeEnemy : EnemyBase
{
    private Animator _animator;
    private SpriteRenderer _sr;
    private static readonly int AnimIsWalk = Animator.StringToHash("IsWalking");

    protected override void Start()
    {
        base.Start();
        _animator = GetComponentInChildren<Animator>();
        _sr       = GetComponentInChildren<SpriteRenderer>();
    }

    // ─── AI ───────────────────────────────────────────────────
    protected override void OnTick()
    {
        if (Player == null)
        {
            ResolvePlayer();
            _rb2d.linearVelocity = Vector2.zero;
            SetWalk(false);
            return;
        }

        Vector2 dir = (Player.transform.position - transform.position).normalized;

        _rb2d.linearVelocity = dir * speed;

        // 좌우 반전
        if (_sr != null && Mathf.Abs(dir.x) > 0.01f)
            _sr.flipX = dir.x < 0f;

        SetWalk(true);
    }

    // ─── 사망 처리 ────────────────────────────────────────────
    protected override void Die()
    {
        _rb2d.linearVelocity = Vector2.zero;
        SetWalk(false);
        base.Die();
    }

    // ─── 유틸 ─────────────────────────────────────────────────
    private void SetWalk(bool value)
    {
        if (_animator == null) return;
        _animator.SetBool(AnimIsWalk, value);
    }
}
