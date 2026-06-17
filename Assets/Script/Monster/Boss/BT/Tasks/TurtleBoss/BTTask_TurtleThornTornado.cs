using UnityEngine;
using System.Collections;

/// <summary>
/// 거북이 보스 가시 토네이도 공격 Task.
/// 8방향으로 가시를 발사하고, 잠시 후 22.5도 회전한 위치에 2차 발사.
/// </summary>
public class BTTask_TurtleThornTornado : BTTask
{
    private TurtleBoss turtleBoss;
    private TurtleBossSO so;

    private float startTime;
    private bool secondWaveFired;
    private bool done;

    public BTTask_TurtleThornTornado(TurtleBoss boss, TurtleBossSO so) : base(boss)
    {
        this.turtleBoss = boss;
        this.so = so;
    }

    protected override void OnEnter()
    {
        startTime = Time.time;
        secondWaveFired = false;
        done = false;

        // 1차 발사 (즉시)
        FireThornWave(0f);
    }

    protected override BossBTState OnTick()
    {
        if (!IsBossValid()) return BossBTState.Failure;

        // 2차 발사 대기
        if (!secondWaveFired && Time.time >= startTime + so.thornSecondWaveDelay)
        {
            secondWaveFired = true;
            FireThornWave(22.5f);
            done = true;
        }

        return done ? BossBTState.Success : BossBTState.Running;
    }

    private void FireThornWave(float angleOffset)
    {
        if (so.thornBulletPrefab == null) return;

        int directions = Mathf.Max(2, so.thornDirections);
        float angleStep = 360f / directions;

        for (int i = 0; i < directions; i++)
        {
            float angle = angleOffset + angleStep * i;
            Quaternion rotation = Quaternion.Euler(0f, 0f, angle);
            Vector3 spawnPos = turtleBoss.transform.position;

            GameObject bulletObj = Object.Instantiate(so.thornBulletPrefab, spawnPos, rotation);
            Bullet bullet = bulletObj.GetComponent<Bullet>();
            if (bullet != null)
            {
                bullet.ATK = turtleBoss.atk;
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
