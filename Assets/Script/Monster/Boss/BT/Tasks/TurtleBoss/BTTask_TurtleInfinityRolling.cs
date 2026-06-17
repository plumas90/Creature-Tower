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

    // Phase 2 지속 가시 발사용 변수
    private float nextContinuousThornTime;
    private float continuousThornAngle;
    private const float THORN_FIRE_INTERVAL = 0.2f; // 0.2초 간격 발사
    private const float THORN_ANGLE_STEP = 15f;      // 시계방향 15도 회전

    public BTTask_TurtleInfinityRolling(TurtleBoss boss, TurtleBossSO so) : base(boss)
    {
        this.turtleBoss = boss;
        this.so = so;
    }

    protected override void OnEnter()
    {
        Debug.Log($"[BTTask_TurtleInfinityRolling] OnEnter, frame={Time.frameCount}, Time.time={Time.time}");
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

        // 지속 가시 발사 타이밍 초기화
        nextContinuousThornTime = 0f;
        continuousThornAngle = 0f;

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

        // Phase 2는 항상 구르기 상태여야 하므로, 만약 다른 태스크의 OnExit 등에 의해 구르기가 풀렸다면 다시 활성화합니다.
        if (!turtleBoss.IsRollingState)
        {
            Debug.Log($"[BTTask_TurtleInfinityRolling] isRollingState was false! Restoring to true at Time={Time.time}");
            turtleBoss.SetRollingAnim(true);
        }

        // 이동 수행
        ballisticComp.MoveBallistic(so.rollingSpeed);

        // 무한 구르기 중 지속적으로 시계방향 회전하며 가시 뿜기
        if (Time.time >= nextContinuousThornTime)
        {
            Debug.Log($"[BTTask_TurtleInfinityRolling] Firing continuous thorn at Time={Time.time}, nextTime={nextContinuousThornTime}, angle={continuousThornAngle}");
            FireContinuousThorn();
            nextContinuousThornTime = Time.time + THORN_FIRE_INTERVAL;
        }

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
        Debug.Log($"[BTTask_TurtleInfinityRolling] OnExit, frame={Time.frameCount}");
        turtleBoss.SetRollingAnim(false);

        if (ballisticComp != null)
        {
            ballisticComp.SpeedMultiplier = 1f;
            ballisticComp.OnCastPlayerHit = null;
        }
    }

    private void OnBounce()
    {
        Debug.Log($"[BTTask_TurtleInfinityRolling] OnBounce, frame={Time.frameCount}");
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
                bullet.IsDamage = true;
                bullet._direction = rotation * Vector2.right;
                if (bullet.targets == null)
                    bullet.targets = new System.Collections.Generic.Dictionary<string, int>();
                bullet.targets["Player"] = (int)BulletTarget.Player;
                bullet.Init();
                bullet.BulletLifeTime = so.thornBulletLifetime;
            }
        }
    }

    private void FireContinuousThorn()
    {
        if (so.thornBulletPrefab == null) return;

        // 시계방향으로 회전하기 위해 각도를 뺌
        continuousThornAngle -= THORN_ANGLE_STEP;
        if (continuousThornAngle <= -360f)
            continuousThornAngle += 360f;

        Quaternion rotation = Quaternion.Euler(0f, 0f, continuousThornAngle);
        GameObject bulletObj = Object.Instantiate(so.thornBulletPrefab, boss.transform.position, rotation);
        Bullet bullet = bulletObj.GetComponent<Bullet>();
        if (bullet != null)
        {
            bullet.ATK = boss.atk * 0.5f; // 지속 가시이므로 기본 대미지 대비 감소
            bullet.BulletSpeed = so.thornBulletSpeed * 0.8f; // 속도도 약간 감소
            bullet.IsDamage = true;
            bullet._direction = rotation * Vector2.right;
            if (bullet.targets == null)
                bullet.targets = new System.Collections.Generic.Dictionary<string, int>();
            bullet.targets["Player"] = (int)BulletTarget.Player;
            bullet.Init();
            bullet.BulletLifeTime = so.thornBulletLifetime;
        }
    }
}
