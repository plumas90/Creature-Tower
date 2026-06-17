using System.Collections.Generic;
using UnityEngine;

public class BTTask_GhostKnightSwordPattern : BTTask
{
    private float startTime;
    private readonly List<GhostKnightSword> activeSwords = new List<GhostKnightSword>();
    private const float START_DELAY = 2.0f; // 2 seconds delay before movement

    public BTTask_GhostKnightSwordPattern(BossBase boss) : base(boss)
    {
    }

    protected override void OnEnter()
    {
        activeSwords.Clear();

        GhostKnightSO so = boss.MainSO as GhostKnightSO;
        if (so == null)
        {
            Debug.LogError("[BTTask_GhostKnightSwordPattern] Boss MainSO is not GhostKnightSO.");
            return;
        }

        if (so.swordPrefab == null)
        {
            Debug.LogError("[BTTask_GhostKnightSwordPattern] swordPrefab is not assigned in GhostKnightSO.");
            return;
        }

        Vector3 center = boss.transform.position;
        if (boss is GhostKnight gk)
        {
            center = gk.SpawnPosition;
        }

        // Randomly select one index out of 0, 1, 2, 3 to not spawn (the safe zone)
        int safeIndex = Random.Range(0, 4);

        for (int i = 0; i < 4; i++)
        {
            if (i == safeIndex)
                continue;

            // Use distances 3, 5, 7, 9 instead of 2, 4, 6, 8
            float radius = 3f + i * 2f;

            // Spawn Left Sword
            Vector3 leftSpawnPos = center + new Vector3(-radius, 0f, 0f);
            GameObject leftSwordObj = Object.Instantiate(so.swordPrefab, leftSpawnPos, Quaternion.identity);
            GhostKnightSword leftSword = leftSwordObj.GetComponent<GhostKnightSword>();
            if (leftSword == null)
            {
                leftSword = leftSwordObj.AddComponent<GhostKnightSword>();
            }
            leftSword.Initialize(center, radius, true, so.swordDamage, so.swordSwingPeriod, so.swordRotationSpeed, START_DELAY, so.swordSwingCount);
            activeSwords.Add(leftSword);

            // Spawn Right Sword
            Vector3 rightSpawnPos = center + new Vector3(radius, 0f, 0f);
            GameObject rightSwordObj = Object.Instantiate(so.swordPrefab, rightSpawnPos, Quaternion.identity);
            GhostKnightSword rightSword = rightSwordObj.GetComponent<GhostKnightSword>();
            if (rightSword == null)
            {
                rightSword = rightSwordObj.AddComponent<GhostKnightSword>();
            }
            rightSword.Initialize(center, radius, false, so.swordDamage, so.swordSwingPeriod, so.swordRotationSpeed, START_DELAY, so.swordSwingCount);
            activeSwords.Add(rightSword);
        }

        startTime = Time.time;
        Debug.Log($"[BTTask_GhostKnightSwordPattern] Spawning sword pairs with safe index {safeIndex + 1} for {so.swordSwingCount} swings.");
    }

    protected override BossBTState OnTick()
    {
        if (!IsBossValid())
        {
            return BossBTState.Failure;
        }

        if (activeSwords.Count == 0)
        {
            return BossBTState.Success;
        }

        bool allFinished = true;
        for (int i = 0; i < activeSwords.Count; i++)
        {
            if (activeSwords[i] != null && !activeSwords[i].IsFinished)
            {
                allFinished = false;
                break;
            }
        }

        if (allFinished)
        {
            return BossBTState.Success;
        }

        return BossBTState.Running;
    }

    protected override void OnExit()
    {
        Debug.Log("[BTTask_GhostKnightSwordPattern] Cleaning up spawned swords.");
        for (int i = 0; i < activeSwords.Count; i++)
        {
            if (activeSwords[i] != null)
            {
                Object.Destroy(activeSwords[i].gameObject);
            }
        }
        activeSwords.Clear();
    }
}
