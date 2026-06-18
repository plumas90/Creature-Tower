using System.Collections.Generic;
using UnityEngine;

public class BTTask_GhostKnightTargetedPattern : BTTask
{
    public enum TargetedPatternType { Pattern3, Pattern4 }
    private TargetedPatternType patternType;

    private int currentWave;
    private int maxWaves;
    private float lastWaveSpawnTime;

    private readonly List<GhostKnightSword> activeSwords = new List<GhostKnightSword>();

    public BTTask_GhostKnightTargetedPattern(BossBase boss, TargetedPatternType type) : base(boss)
    {
        this.patternType = type;
    }

    protected override void OnEnter()
    {
        activeSwords.Clear();

        GhostKnightSO so = boss.MainSO as GhostKnightSO;
        if (so == null)
        {
            Debug.LogError("[BTTask_GhostKnightTargetedPattern] Boss MainSO is not GhostKnightSO.");
            return;
        }

        currentWave = 1;
        maxWaves = (patternType == TargetedPatternType.Pattern3) ? so.targetedPatternCount : so.targetedPatternCount4;
        lastWaveSpawnTime = Time.time;

        SpawnWave(so);
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

        activeSwords.RemoveAll(s => s == null);

        float waveInterval = (patternType == TargetedPatternType.Pattern3) ? so.targetedWaveInterval : so.targetedWaveInterval4;

        // Continuous wave spawning at regular intervals
        if (currentWave < maxWaves)
        {
            if (Time.time - lastWaveSpawnTime >= waveInterval)
            {
                SpawnWave(so);
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
        Debug.Log($"[BTTask_GhostKnightTargetedPattern] Cleaning up targeted swords for {patternType}.");
        for (int i = 0; i < activeSwords.Count; i++)
        {
            if (activeSwords[i] != null)
            {
                Object.Destroy(activeSwords[i].gameObject);
            }
        }
        activeSwords.Clear();
    }

    private void SpawnWave(GhostKnightSO so)
    {
        if (so.swordPrefab == null)
        {
            Debug.LogError("[BTTask_GhostKnightTargetedPattern] swordPrefab is null in GhostKnightSO.");
            return;
        }

        if (boss.Player == null)
        {
            Debug.LogWarning("[BTTask_GhostKnightTargetedPattern] Player is null, cannot spawn targeted wave.");
            return;
        }

        Vector3 playerPos = boss.Player.transform.position;
        
        float radius = (patternType == TargetedPatternType.Pattern3) ? so.targetedPatternRadius : so.targetedPatternRadius4;
        float launchDelay = (patternType == TargetedPatternType.Pattern3) ? so.targetedSwordLaunchDelay : so.targetedSwordLaunchDelay4;
        float speed = (patternType == TargetedPatternType.Pattern3) ? so.targetedSwordSpeed : so.targetedSwordSpeed4;
        float damage = (patternType == TargetedPatternType.Pattern3) ? so.targetedSwordDamage : so.targetedSwordDamage4;
        int count = (patternType == TargetedPatternType.Pattern3) ? so.targetedSwordCount : so.targetedSwordCount4;
        float angleStep = (patternType == TargetedPatternType.Pattern3) ? so.targetedAngleStep : so.targetedAngleStep4;

        if (count <= 0) count = 3;

        // Choose a random base angle for the center of the cluster
        float baseAngle = Random.Range(0f, 360f);
        float startOffset = -(count - 1) * 0.5f;

        for (int i = 0; i < count; i++)
        {
            float angleOffset = (startOffset + i) * angleStep;
            float angle = baseAngle + angleOffset;
            float angleRad = angle * Mathf.Deg2Rad;

            Vector3 spawnPos = playerPos + new Vector3(radius * Mathf.Cos(angleRad), radius * Mathf.Sin(angleRad), 0f);
            GameObject swordObj = Object.Instantiate(so.swordPrefab, spawnPos, Quaternion.identity);

            GhostKnightSword sword = swordObj.GetComponent<GhostKnightSword>();
            if (sword == null)
            {
                sword = swordObj.AddComponent<GhostKnightSword>();
            }

            Vector3 dirToPlayer = (playerPos - spawnPos).normalized;

            sword.InitializeTargetedLaunch(spawnPos, dirToPlayer, damage, speed, launchDelay);
            activeSwords.Add(sword);
        }

        Debug.Log($"[BTTask_GhostKnightTargetedPattern] Spawning Wave {currentWave}/{maxWaves} for {patternType} targeting player position {playerPos}");
    }
}
