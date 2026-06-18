using System.Collections.Generic;
using UnityEngine;

public class BTTask_GhostKnightHexagonPattern : BTTask
{
    private int currentWave;
    private int maxWaves;
    private float lastWaveSpawnTime;
    private const float START_DELAY = 2.0f; // 2 seconds delay before movement
    private int previousSafeIndex = -1;

    private readonly List<GhostKnightSword> activeSwords = new List<GhostKnightSword>();

    public BTTask_GhostKnightHexagonPattern(BossBase boss) : base(boss)
    {
    }

    protected override void OnEnter()
    {
        activeSwords.Clear();
        previousSafeIndex = -1;

        if (boss is GhostKnight gkRef)
        {
            gkRef.ResetArm();
        }

        GhostKnightSO so = boss.MainSO as GhostKnightSO;
        if (so == null)
        {
            Debug.LogError("[BTTask_GhostKnightHexagonPattern] Boss MainSO is not GhostKnightSO.");
            return;
        }

        Vector3 center = boss.transform.position;
        if (boss.StageOwner != null)
        {
            center = boss.StageOwner.GetZoneCenter();
        }
        else if (boss is GhostKnight gk)
        {
            center = gk.SpawnPosition;
        }

        if (boss is GhostKnight gkVis)
        {
            gkVis.SetVisibility(false);
        }
        boss.invincibility = true;

        // 1. Force teleport player to center (boss start y - 3)
        if (boss.Player != null)
        {
            boss.Player.transform.position = center;
            Rigidbody2D rb = boss.Player.GetComponent<Rigidbody2D>();
            if (rb != null)
                rb.linearVelocity = Vector2.zero;

            MainCamera mc = Camera.main != null ? Camera.main.GetComponent<MainCamera>() : null;
            if (mc != null)
                mc.FocusOnPlayerInstant();
        }

        currentWave = 1;
        maxWaves = so.hexagonPatternCount;
        lastWaveSpawnTime = Time.time;

        SpawnWave(center, so, 0);
    }

    protected override BossBTState OnTick()
    {
        if (!IsBossValid())
        {
            return BossBTState.Failure;
        }

        GhostKnightSO so = boss.MainSO as GhostKnightSO;
        if (so == null)
            return BossBTState.Failure;

        Vector3 center = boss.transform.position;
        if (boss.StageOwner != null)
        {
            center = boss.StageOwner.GetZoneCenter();
        }
        else if (boss is GhostKnight gk)
        {
            center = gk.SpawnPosition;
        }

        // Check if player has escaped the pattern radius (radius + 0.5f)
        if (boss.Player != null)
        {
            float playerDist = Vector3.Distance(boss.Player.transform.position, center);
            if (playerDist > so.hexagonPatternRadius + 0.5f)
            {
                Debug.Log($"[BTTask_GhostKnightHexagonPattern] Player escaped pattern radius (dist: {playerDist:F2} > {so.hexagonPatternRadius + 0.5f:F2}). Early terminating task.");
                return BossBTState.Success;
            }
        }

        activeSwords.RemoveAll(s => s == null);

        // Continuous wave spawning at regular intervals
        if (currentWave < maxWaves)
        {
            if (Time.time - lastWaveSpawnTime >= so.hexagonWaveInterval)
            {
                SpawnWave(center, so, currentWave);
                currentWave++;
                lastWaveSpawnTime = Time.time;
            }
        }

        // Finish when all waves spawned and all swords are gone
        if (currentWave >= maxWaves && activeSwords.Count == 0)
        {
            return BossBTState.Success;
        }

        return BossBTState.Running;
    }

    protected override void OnExit()
    {
        if (boss is GhostKnight gk)
        {
            gk.SetVisibility(true);
        }
        boss.invincibility = false;

        Debug.Log("[BTTask_GhostKnightHexagonPattern] Cleaning up hexagon swords.");
        for (int i = 0; i < activeSwords.Count; i++)
        {
            if (activeSwords[i] != null)
            {
                Object.Destroy(activeSwords[i].gameObject);
            }
        }
        activeSwords.Clear();
    }

    private int ChooseSafeIndex(int waveIndex, int totalWaves, int vertexCount, int prevSafeIndex)
    {
        bool isLastWave = (waveIndex == totalWaves - 1);
        List<int> candidates = new List<int>();

        for (int i = 0; i < vertexCount; i++)
        {
            // Last wave constraint: cannot be 0 (12 o'clock)
            if (isLastWave && i == 0)
                continue;

            // Distance constraint from 2nd wave onwards
            if (waveIndex > 0 && prevSafeIndex != -1)
            {
                int diff = Mathf.Abs(i - prevSafeIndex);
                int dist = Mathf.Min(diff, vertexCount - diff);
                if (dist > 3)
                    continue;
            }

            candidates.Add(i);
        }

        // Fallback in case candidates list is empty (should not happen normally)
        if (candidates.Count == 0)
        {
            for (int i = 0; i < vertexCount; i++)
            {
                if (isLastWave && i == 0) continue;
                candidates.Add(i);
            }
        }

        return candidates[Random.Range(0, candidates.Count)];
    }

    private void SpawnWave(Vector3 center, GhostKnightSO so, int waveIndex)
    {
        if (so.swordPrefab == null)
        {
            Debug.LogError("[BTTask_GhostKnightHexagonPattern] swordPrefab is null in GhostKnightSO.");
            return;
        }

        int vertexCount = so.hexagonVertexCount <= 0 ? 10 : so.hexagonVertexCount;
        
        int safeIndex = ChooseSafeIndex(waveIndex, maxWaves, vertexCount, previousSafeIndex);
        previousSafeIndex = safeIndex;

        float radius = so.hexagonPatternRadius;

        for (int i = 0; i < vertexCount; i++)
        {
            if (i == safeIndex)
                continue;

            // angle pointing upwards: 90 - i * (360 / N)
            float angleDegrees = 90f - i * (360f / vertexCount);
            float angleRad = angleDegrees * Mathf.Deg2Rad;

            Vector3 spawnPos = center + new Vector3(radius * Mathf.Cos(angleRad), radius * Mathf.Sin(angleRad), 0f);
            GameObject swordObj = Object.Instantiate(so.swordPrefab, spawnPos, Quaternion.identity);
            swordObj.transform.localScale = new Vector3(so.hexagonSwordScale, so.hexagonSwordScale, 1f);

            GhostKnightSword sword = swordObj.GetComponent<GhostKnightSword>();
            if (sword == null)
            {
                sword = swordObj.AddComponent<GhostKnightSword>();
            }

            // Flip logic: flip right-side vertices (xOffset > 0) and bottom-most vertex (xOffset is 0 and yOffset < 0)
            float xOffset = spawnPos.x - center.x;
            float yOffset = spawnPos.y - center.y;
            bool shouldFlip = (xOffset > 0f) || (Mathf.Abs(xOffset) < 0.001f && yOffset < 0f);

            sword.InitializeLinearInward(center, spawnPos, so.hexagonSwordDamage, so.hexagonSwordSpeed, so.hexagonSwordRotationSpeed, START_DELAY, shouldFlip);
            activeSwords.Add(sword);
        }

        Debug.Log($"[BTTask_GhostKnightHexagonPattern] Spawning Wave {waveIndex + 1}/{maxWaves} (N={vertexCount}) with safe index {safeIndex}");
    }
}
