using UnityEngine;

/// <summary>
/// 거북이 보스 Phase 2 무한 구르기 Task.
/// HP 30% 이하에서 발동, 멈추지 않고 계속 구른다.
/// 반사 시마다 가시를 발사한다.
/// </summary>
public class BTTask_TurtleInfinityRolling : BTTask
{
    private TurtleBoss turtleBoss;
    private TurtleBossSO so;

    private BallisticMovementComponent ballisticComp;
    private Vector2 previousDirection;

    public BTTask_TurtleInfinityRolling(TurtleBoss boss, TurtleBossSO so) : base(boss)
    {
        this.turtleBoss = boss;
        this.so = so;
    }

    protected override void OnEnter()
    {
        // BallisticMovementComponent 가져오거나 추가
        ballisticComp = boss.GetComponent<BallisticMovementComponent>();
        if (ballisticComp == null)
            ballisticComp = boss.gameObject.AddComponent<BallisticMovementComponent>();

        // 플레이어 방향으로 구르기 시작
        Transform playerTr = GetPlayerTransform();
        if (playerTr != null)
        {
            Vector2 dir = ((Vector2)playerTr.position - (Vector2)boss.transform.position).normalized;
            ballisticComp.CurrentDirection = dir;
        }
        else
        {
            ballisticComp.CurrentDirection = Vector2.right;
        }

        previousDirection = ballisticComp.CurrentDirection;

        // 구르기 애니메이션 시작
        turtleBoss.SetRollingAnim(true);

        // 빠른 구르기 속도 (Phase 2는 더 빠름)
        ballisticComp.SpeedMultiplier = 1.4f;

        // 반사 시 콜백
        ballisticComp.OnCastPlayerHit = (playerStat) =>
        {
            OnBounce();
        };
    }

    protected override BossBTState OnTick()
    {
        if (boss == null || !boss.live) return BossBTState.Failure;
        if (ballisticComp == null) return BossBTState.Failure;

        // 이동 수행
        ballisticComp.MoveBallistic(so.rollingSpeed);

        // 방향 변화 감지 (반사 발생 체크)
        Vector2 currentDir = ballisticComp.CurrentDirection;
        float dot = Vector2.Dot(previousDirection.normalized, currentDir.normalized);

        if (dot < 0.85f)
        {
            OnBounce();
            previousDirection = currentDir;
        }

        // Phase 2: 항상 Running (보스가 죽을 때까지 계속 구름)
        return BossBTState.Running;
    }

    protected override void OnExit()
    {
        turtleBoss.SetRollingAnim(false);

        if (ballisticComp != null)
        {
            ballisticComp.SpeedMultiplier = 1f;
            ballisticComp.OnCastPlayerHit = null;
        }
    }

    private void OnBounce()
    {
        // Phase 2: 반사 시마다 가시 발사 (thornOnBounce 무조건)
        FireThornOnBounce();
    }

    private void FireThornOnBounce()
    {
        if (so.thornBulletPrefab == null) return;

        int directions = Mathf.Max(2, so.thornDirections);
        float angleStep = 360f / directions;

        for (int i = 0; i < directions; i++)
        {
            float angle = angleStep * i;
            Quaternion rotation = Quaternion.Euler(0f, 0f, angle);
            GameObject bulletObj = Object.Instantiate(so.thornBulletPrefab, boss.transform.position, rotation);
            Bullet bullet = bulletObj.GetComponent<Bullet>();
            if (bullet != null)
            {
                bullet.ATK = boss.atk;
                bullet.BulletSpeed = so.thornBulletSpeed;
                bullet.BulletLifeTime = so.thornBulletLifetime;
                bullet.IsDamage = true;
                bullet._direction = rotation * Vector2.right;
                if (bullet.targets == null)
                    bullet.targets = new System.Collections.Generic.Dictionary<string, int>();
                bullet.targets["Player"] = (int)BulletTarget.Player;
            }
        }
    }
}
