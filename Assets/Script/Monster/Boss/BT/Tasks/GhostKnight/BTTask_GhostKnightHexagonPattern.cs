using System.Collections.Generic;
using UnityEngine;

public class BTTask_GhostKnightHexagonPattern : BTTask
{
    private int currentWave;
    private int maxWaves;
    private float lastWaveSpawnTime;
    private const float START_DELAY = 2.0f; // 2 seconds delay before movement

    private readonly List<GhostKnightSword> activeSwords = new List<GhostKnightSword>();

    public BTTask_GhostKnightHexagonPattern(BossBase boss) : base(boss)
    {
    }

    protected override void OnEnter()
    {
        activeSwords.Clear();

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

        SpawnWave(center, so);
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

        activeSwords.RemoveAll(s => s == null);

        // Continuous wave spawning at regular intervals
        if (currentWave < maxWaves)
        {
            if (Time.time - lastWaveSpawnTime >= so.hexagonWaveInterval)
            {
                SpawnWave(center, so);
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

    private void SpawnWave(Vector3 center, GhostKnightSO so)
    {
        if (so.swordPrefab == null)
        {
            Debug.LogError("[BTTask_GhostKnightHexagonPattern] swordPrefab is null in GhostKnightSO.");
            return;
        }

        int vertexCount = so.hexagonVertexCount <= 0 ? 10 : so.hexagonVertexCount;
        // Choose one index from 0 to vertexCount - 1 to omit (fully randomized safe zone)
        int safeIndex = Random.Range(0, vertexCount);
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

        Debug.Log($"[BTTask_GhostKnightHexagonPattern] Spawning Wave {currentWave}/{maxWaves} (N={vertexCount}) with safe index {safeIndex}");
    }
}
