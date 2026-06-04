using UnityEngine;
using System.Collections.Generic;

public class PeaPodVineChainController : MonoBehaviour
{
    private PeaPodBoss boss;
    private PeaPodBossSO so;
    private Vector2 direction;
    private int curveSign;
    private int spawnedCount;
    private float nextSpawnAllowedAt;
    private readonly List<PeaPodVineSegment> spawnedSegments = new List<PeaPodVineSegment>();

    public void StartChain(PeaPodBoss bossInstance, PeaPodBossSO soData, Vector2 startDirection, int initialCurveSign)
    {
        boss = bossInstance;
        so = soData;
        direction = startDirection;
        curveSign = initialCurveSign;
        spawnedCount = 0;
        nextSpawnAllowedAt = Time.time;

        Vector2 spawnPos = GetFirstSegmentSpawnPosition();
        TrySpawnNextSegment(spawnPos);
        nextSpawnAllowedAt = Time.time + Mathf.Max(0f, so.vineChainCooldown);
    }

    private void Update()
    {
        if (boss == null || !boss.live || so == null)
        {
            Destroy(gameObject);
            return;
        }

        if (spawnedCount >= Mathf.Max(1, so.maxVineSegments))
        {
            Destroy(gameObject);
            return;
        }

        if (spawnedSegments.Count == 0)
        {
            Destroy(gameObject);
            return;
        }

        PeaPodVineSegment last = spawnedSegments[spawnedSegments.Count - 1];
        if (last == null)
        {
            Destroy(gameObject);
            return;
        }

        if (!last.IsFullyGrown)
            return;

        if (Time.time < nextSpawnAllowedAt)
            return;

        Vector2 tip = last.TipWorldPosition;
        if (IsTipBlockedByWall(tip))
        {
            Destroy(gameObject);
            return;
        }

        Vector2 toPlayer = direction;
        Transform playerTr = boss.Player != null ? boss.Player.transform : null;
        if (playerTr != null)
        {
            Vector2 dir = (Vector2)(playerTr.position - (Vector3)tip);
            if (dir.sqrMagnitude > 0.0001f)
                toPlayer = dir.normalized;
        }

        float baseTurn = so.curveDegreesPerSegment * curveSign;
        Vector2 baseDirection = Quaternion.Euler(0f, 0f, baseTurn) * direction;
        float towardDelta = Vector2.SignedAngle(baseDirection, toPlayer);
        float clampedToward = Mathf.Clamp(towardDelta, -Mathf.Abs(so.maxTurnTowardPlayerDegrees), Mathf.Abs(so.maxTurnTowardPlayerDegrees));
        direction = (Quaternion.Euler(0f, 0f, clampedToward) * baseDirection).normalized;

        TrySpawnNextSegment(tip);
        nextSpawnAllowedAt = Time.time + Mathf.Max(0f, so.vineChainCooldown);
    }

    private void TrySpawnNextSegment(Vector2 spawnPos)
    {
        if (so == null || so.vineSegmentPrefab == null || boss == null)
            return;

        Quaternion rot = Quaternion.FromToRotation(Vector3.right, direction);
        GameObject segmentObj = Object.Instantiate(so.vineSegmentPrefab, spawnPos, rot);
        if (segmentObj == null)
            return;

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
