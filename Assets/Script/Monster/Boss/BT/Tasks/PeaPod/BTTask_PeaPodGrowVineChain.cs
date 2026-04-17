using System.Collections.Generic;
using UnityEngine;

public class BTTask_PeaPodGrowVineChain : BTTask
{
    private readonly PeaPodBossSO so;
    private readonly List<PeaPodVineSegment> spawnedSegments = new List<PeaPodVineSegment>();
    private Vector2 direction;
    private int curveSign;
    private int spawnedCount;
    private bool finished;
    private float finishedAt;
    private float nextSpawnAllowedAt;
    private float chainStartedAt;

    public BTTask_PeaPodGrowVineChain(BossBase boss, PeaPodBossSO soData) : base(boss)
    {
        so = soData;
    }

    protected override void OnEnter()
    {
        if (so == null)
        {
            Debug.LogError("[BTTask_PeaPodGrowVineChain] SO is null.");
            return;
        }

        if (so.vineSegmentPrefab == null)
        {
            Debug.LogError("[BTTask_PeaPodGrowVineChain] vineSegmentPrefab is null.");
            return;
        }

        spawnedSegments.Clear();
        spawnedCount = 0;
        finished = false;
        finishedAt = 0f;
        nextSpawnAllowedAt = Time.time;
        chainStartedAt = Time.time;

        Transform playerTr = GetPlayerTransform();
        Vector2 toPlayer = playerTr != null
            ? (Vector2)(playerTr.position - boss.transform.position)
            : Vector2.right;
        direction = toPlayer.sqrMagnitude > 0.0001f ? toPlayer.normalized : Vector2.right;

        if (so.randomizeCurveDirection)
            curveSign = Random.value < 0.5f ? -1 : 1;
        else
            curveSign = so.fixedCurveDirectionSign >= 0 ? 1 : -1;

        TrySpawnNextSegment(GetFirstSegmentSpawnPosition());
        nextSpawnAllowedAt = Time.time + Mathf.Max(0f, so.vineChainCooldown);
        Debug.Log("[BTTask_PeaPodGrowVineChain] First vine segment spawn requested.");
    }

    protected override BossBTState OnTick()
    {
        if (!IsBossValid() || so == null || so.vineSegmentPrefab == null)
            return BossBTState.Failure;

        // 다음 "첫 줄기" 시작 타이밍은 체인 상태와 무관하게 쿨타임 기준으로만 제어한다.
        if (Time.time - chainStartedAt >= Mathf.Max(0.05f, so.attackInterval))
            return BossBTState.Success;

        if (finished)
        {
            if (Time.time - finishedAt >= Mathf.Max(0.05f, so.attackInterval))
                return BossBTState.Success;
            return BossBTState.Running;
        }

        if (spawnedSegments.Count == 0)
        {
            finished = true;
            finishedAt = Time.time;
            return BossBTState.Running;
        }

        PeaPodVineSegment last = spawnedSegments[spawnedSegments.Count - 1];
        if (last == null)
        {
            finished = true;
            finishedAt = Time.time;
            return BossBTState.Running;
        }

        if (!last.IsFullyGrown)
            return BossBTState.Running;

        if (Time.time < nextSpawnAllowedAt)
            return BossBTState.Running;

        Vector2 tip = last.TipWorldPosition;
        if (IsTipBlockedByWall(tip) || spawnedCount >= Mathf.Max(1, so.maxVineSegments))
        {
            finished = true;
            finishedAt = Time.time;
            return BossBTState.Running;
        }

        Vector2 toPlayer = Vector2.right;
        Transform playerTr = GetPlayerTransform();
        if (playerTr != null)
        {
            Vector2 dir = (Vector2)(playerTr.position - (Vector3)tip);
            if (dir.sqrMagnitude > 0.0001f)
                toPlayer = dir.normalized;
            else
                toPlayer = direction;
        }
        else
        {
            toPlayer = direction;
        }

        float baseTurn = so.curveDegreesPerSegment * curveSign;
        Vector2 baseDirection = Quaternion.Euler(0f, 0f, baseTurn) * direction;
        float towardDelta = Vector2.SignedAngle(baseDirection, toPlayer);
        float clampedToward = Mathf.Clamp(towardDelta, -Mathf.Abs(so.maxTurnTowardPlayerDegrees), Mathf.Abs(so.maxTurnTowardPlayerDegrees));
        direction = (Quaternion.Euler(0f, 0f, clampedToward) * baseDirection).normalized;
        TrySpawnNextSegment(tip);
        nextSpawnAllowedAt = Time.time + Mathf.Max(0f, so.vineChainCooldown);
        return BossBTState.Running;
    }

    private void TrySpawnNextSegment(Vector2 spawnPos)
    {
        if (so == null || so.vineSegmentPrefab == null)
            return;

        Quaternion rot = Quaternion.FromToRotation(Vector3.right, direction);
        GameObject segmentObj = Object.Instantiate(so.vineSegmentPrefab, spawnPos, rot);
        if (segmentObj == null)
        {
            Debug.LogError("[BTTask_PeaPodGrowVineChain] Instantiate returned null.");
            return;
        }

        if (segmentObj.name.Contains("PeaPodVineSegment") == false)
            segmentObj.name = "PeaPodVineSegment(Clone)";

        PeaPodVineSegment segment = segmentObj.GetComponent<PeaPodVineSegment>();
        if (segment == null)
            segment = segmentObj.AddComponent<PeaPodVineSegment>();

        float segmentDamage = Mathf.Max(0f, boss.atk * so.vineDamageMultiplier);
        segment.Initialize(segmentDamage, so.vineGrowthSpeed, so.vineMaxLength, so.vineLifetime, so.vineColliderHeightRatio);
        spawnedSegments.Add(segment);
        spawnedCount++;
    }

    private bool IsTipBlockedByWall(Vector2 tip)
    {
        LayerMask mask = so.vineWallMask.value != 0
            ? so.vineWallMask
            : (1 << LayerMask.NameToLayer("Wall"));

        Collider2D hit = Physics2D.OverlapCircle(tip, Mathf.Max(0.01f, so.vineWallProbeRadius), mask);
        return hit != null;
    }

    private Vector2 GetFirstSegmentSpawnPosition()
    {
        Vector2 basePos = boss.transform.position;
        Collider2D bossCol = boss.GetComponent<Collider2D>();
        if (bossCol != null)
            basePos.y = bossCol.bounds.min.y;

        return basePos;
    }
}
