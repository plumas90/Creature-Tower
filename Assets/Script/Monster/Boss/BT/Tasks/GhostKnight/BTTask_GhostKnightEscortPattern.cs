using System.Collections.Generic;
using UnityEngine;

public class BTTask_GhostKnightEscortPattern : BTTask
{
    private enum EscortState
    {
        Positioning,
        Warning,
        Moving,
        Transitioning
    }

    private EscortState currentState;
    private int currentMove;
    private int maxMoves;
    private Vector3 startPos;
    private Vector3 endPos;
    private Vector3 moveDir;
    private float timer;

    private readonly List<GhostKnightSword> activeSwords = new List<GhostKnightSword>();
    private readonly List<Vector3> swordOffsets = new List<Vector3>();
    private bool armStopWarningStarted;

    public BTTask_GhostKnightEscortPattern(BossBase boss) : base(boss)
    {
    }

    protected override void OnEnter()
    {
        activeSwords.Clear();
        swordOffsets.Clear();
        currentMove = 0;

        if (boss is GhostKnight gkRef)
        {
            gkRef.ResetArm();
        }

        GhostKnightSO so = boss.MainSO as GhostKnightSO;
        if (so == null)
        {
            Debug.LogError("[BTTask_GhostKnightEscortPattern] Boss MainSO is not GhostKnightSO.");
            return;
        }

        maxMoves = so.escortMoveCount;
        currentState = EscortState.Positioning;
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

        switch (currentState)
        {
            case EscortState.Positioning:
                SetupEscortMove(so);
                break;

            case EscortState.Warning:
                UpdateSwordPositions();
                if (Time.time - timer >= so.escortWarningDuration)
                {
                    armStopWarningStarted = false;
                    currentState = EscortState.Moving;
                }
                break;

            case EscortState.Moving:
                // Move boss
                boss.transform.position = Vector3.MoveTowards(boss.transform.position, endPos, so.escortBossSpeed * Time.deltaTime);
                UpdateSwordPositions();

                // Trigger arm animation 1.0s before arrival to warn player
                float distance = Vector3.Distance(boss.transform.position, endPos);
                float speed = so.escortBossSpeed;
                float remainingTime = speed > 0f ? distance / speed : 0f;
                if (remainingTime <= 1.0f && !armStopWarningStarted)
                {
                    armStopWarningStarted = true;
                    if (boss is GhostKnight gk)
                    {
                        gk.StartRotateArmUp(remainingTime);
                    }
                }

                // Check arrival
                if (Vector3.Distance(boss.transform.position, endPos) < 0.05f)
                {
                    boss.transform.position = endPos;
                    DestroySwords();
                    currentMove++;

                    if (currentMove >= maxMoves)
                    {
                        return BossBTState.Success;
                    }
                    else
                    {
                        timer = Time.time;
                        currentState = EscortState.Transitioning;
                    }
                }
                break;

            case EscortState.Transitioning:
                if (Time.time - timer >= so.escortTransitionDuration)
                {
                    currentState = EscortState.Positioning;
                }
                break;
        }

        return BossBTState.Running;
    }

    protected override void OnExit()
    {
        Debug.Log("[BTTask_GhostKnightEscortPattern] Cleaning up escort pattern.");
        DestroySwords();

        if (boss is GhostKnight gk)
        {
            gk.ResetArm();
        }

        // Return boss to its original spawn position
        Vector3 spawnPos = boss.transform.position;
        if (boss is GhostKnight gk2)
        {
            spawnPos = gk2.SpawnPosition;
        }
        else if (boss.StageOwner != null)
        {
            spawnPos = boss.StageOwner.GetZoneCenter();
        }
        boss.transform.position = spawnPos;
    }

    private void SetupEscortMove(GhostKnightSO so)
    {
        if (boss is GhostKnight gkRef)
        {
            gkRef.ResetArm();
        }

        Bounds bounds = boss.StageOwner != null ? boss.StageOwner.GetZoneBounds() : new Bounds(boss.transform.position, new Vector3(10f, 10f, 0f));
        
        float padding = so.escortMapPadding <= 0f ? 1.5f : so.escortMapPadding;
        float minX = bounds.min.x + padding;
        float maxX = bounds.max.x - padding;
        float minY = bounds.min.y + padding;
        float maxY = bounds.max.y - padding;

        // Safety clamp if bounds are too small
        if (minX >= maxX)
        {
            minX = bounds.center.x - 1f;
            maxX = bounds.center.x + 1f;
        }
        if (minY >= maxY)
        {
            minY = bounds.center.y - 1f;
            maxY = bounds.center.y + 1f;
        }

        startPos = new Vector3(Random.Range(minX, maxX), Random.Range(minY, maxY), 0f);
        
        // Find a destination that is sufficiently far away
        int attempts = 0;
        endPos = new Vector3(Random.Range(minX, maxX), Random.Range(minY, maxY), 0f);
        while (Vector3.Distance(startPos, endPos) < 3.0f && attempts < 15)
        {
            endPos = new Vector3(Random.Range(minX, maxX), Random.Range(minY, maxY), 0f);
            attempts++;
        }

        boss.transform.position = startPos;
        moveDir = (endPos - startPos).normalized;

        float radius = so.escortPatternRadius;

        // Teleport player behind the boss in the safe zone
        if (boss.Player != null)
        {
            boss.Player.transform.position = startPos - moveDir * (radius * 0.5f);
            Rigidbody2D prb = boss.Player.GetComponent<Rigidbody2D>();
            if (prb != null)
            {
                prb.linearVelocity = Vector2.zero;
            }

            MainCamera mc = Camera.main != null ? Camera.main.GetComponent<MainCamera>() : null;
            if (mc != null)
            {
                mc.FocusOnPlayerInstant();
            }
        }

        // Spawn ring of swords facing the move direction
        int swordCount = so.escortSwordCount <= 0 ? 8 : so.escortSwordCount;
        activeSwords.Clear();
        swordOffsets.Clear();

        for (int i = 0; i < swordCount; i++)
        {
            float angle = i * (360f / swordCount) * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(radius * Mathf.Cos(angle), radius * Mathf.Sin(angle), 0f);
            Vector3 spawnPos = startPos + offset;

            GameObject swordObj = Object.Instantiate(so.swordPrefab, spawnPos, Quaternion.identity);
            swordObj.transform.localScale = new Vector3(so.escortSwordScale, so.escortSwordScale, 1f);
            GhostKnightSword sword = swordObj.GetComponent<GhostKnightSword>();
            if (sword == null)
            {
                sword = swordObj.AddComponent<GhostKnightSword>();
            }

            // delaySec = 9999f to keep it controlled by this task without firing on its own
            sword.InitializeTargetedLaunch(spawnPos, moveDir, so.escortSwordDamage, 0f, 9999f);

            activeSwords.Add(sword);
            swordOffsets.Add(offset);
        }

        timer = Time.time;
        currentState = EscortState.Warning;

        Debug.Log($"[BTTask_GhostKnightEscortPattern] Wave {currentMove + 1}/{maxMoves} positioning: start={startPos}, end={endPos}, dir={moveDir}");
    }

    private void UpdateSwordPositions()
    {
        for (int i = 0; i < activeSwords.Count; i++)
        {
            if (activeSwords[i] != null)
            {
                activeSwords[i].UpdateStartPoint(boss.transform.position + swordOffsets[i]);
            }
        }
    }

    private void DestroySwords()
    {
        for (int i = 0; i < activeSwords.Count; i++)
        {
            if (activeSwords[i] != null)
            {
                Object.Destroy(activeSwords[i].gameObject);
            }
        }
        activeSwords.Clear();
        swordOffsets.Clear();
    }
}
