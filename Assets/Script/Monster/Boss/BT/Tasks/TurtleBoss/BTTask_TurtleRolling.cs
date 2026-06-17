using UnityEngine;

/// <summary>
/// 거북이 보스 구르기 공격 Task.
/// 플레이어 방향으로 구르기 시작하여 벽에 반사된다.
/// rollingBounceCount 횟수만큼 반사 후 구르기 종료.
/// Phase 1에서는 반사 시마다 가시를 발사한다 (thornOnBounce = true).
/// </summary>
public class BTTask_TurtleRolling : BTTask
{
    private TurtleBoss turtleBoss;
    private TurtleBossSO so;
    private bool isPhase2;

    private BallisticMovementComponent ballisticComp;
    private int bounceCount;
    private Vector2 previousDirection;

    public BTTask_TurtleRolling(TurtleBoss boss, TurtleBossSO so, bool isPhase2 = false) : base(boss)
    {
        this.turtleBoss = boss;
        this.so = so;
        this.isPhase2 = isPhase2;
    }

    protected override void OnEnter()
    {
        Debug.Log($"[BTTask_TurtleRolling] OnEnter, frame={Time.frameCount}");
        bounceCount = 0;

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

        // 반사 콜백 등록 (플레이어 충돌 시 데미지 처리)
        ballisticComp.OnCastPlayerHit = (playerStat) =>
        {
            // 플레이어와 충돌 시 카운트 증가 및 가시 발사
            OnBounce();
        };
    }

    protected override BossBTState OnTick()
    {
        if (!IsBossValid())
        {
            Debug.LogWarning($"[BTTask_TurtleRolling] Failure: IsBossValid is false. live={boss?.live}, wait={boss?.wait}");
            return BossBTState.Success; // Failure 방지 폴백
        }
        if (ballisticComp == null)
        {
            Debug.LogWarning("[BTTask_TurtleRolling] Failure: ballisticComp is null");
            return BossBTState.Success; // Failure 방지 폴백
        }

        // 이동 수행
        ballisticComp.MoveBallistic(so.rollingSpeed);

        // 방향 변화 감지 (반사 발생 체크)
        Vector2 currentDir = ballisticComp.CurrentDirection;
        float dot = Vector2.Dot(previousDirection.normalized, currentDir.normalized);

        if (dot < 0.85f) // 방향이 크게 바뀌었으면 반사로 판단
        {
            OnBounce();
            previousDirection = currentDir;
        }

        // Phase 2에서는 무한 구르기 (InfinityRolling 태스크가 별도)
        if (isPhase2)
            return BossBTState.Running;

        // Phase 1: 설정된 반사 횟수 이후 종료 (에셋 설정 오류로 0일 때 즉시 종료 방지를 위해 최소 1회 보장)
        int requiredBounce = so != null ? Mathf.Max(1, so.rollingBounceCount) : 4;
        if (bounceCount >= requiredBounce)
        {
            Debug.Log($"[BTTask_TurtleRolling] Success: bounceCount={bounceCount}, max={requiredBounce}");
            return BossBTState.Success;
        }

        return BossBTState.Running;
    }

    protected override void OnExit()
    {
        Debug.Log($"[BTTask_TurtleRolling] OnExit, frame={Time.frameCount}");
        // 만약 Phase 2로 전환된 상태라면 구르기를 풀거나 콜백을 해제하지 않습니다.
        if (turtleBoss != null && !turtleBoss.IsPhase2)
        {
            turtleBoss.SetRollingAnim(false);

            if (ballisticComp != null)
                ballisticComp.OnCastPlayerHit = null;
        }
    }

    private void OnBounce()
    {
        bounceCount++;
        Debug.Log($"[BTTask_TurtleRolling] OnBounce: bounceCount={bounceCount}, frame={Time.frameCount}");

        // Phase 1에서 가시 발사 (thornOnBounce 설정 시)
        if (!isPhase2 && so.thornOnBounce && so.thornBulletPrefab != null)
        {
            FireThornOnBounce();
        }
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
                bullet.ATK = boss.atk * so.rollingDamageMultiplier;
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
}
