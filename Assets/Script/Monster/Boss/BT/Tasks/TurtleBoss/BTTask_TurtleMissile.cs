using UnityEngine;
using System.Collections;

/// <summary>
/// 거북이 보스 미사일 공격 Task.
/// 플레이어를 향해 n발의 미사일을 순차적으로 발사한다.
/// </summary>
public class BTTask_TurtleMissile : BTTask
{
    private TurtleBoss turtleBoss;
    private TurtleBossSO so;

    private int firedCount;
    private float nextFireTime;

    public BTTask_TurtleMissile(TurtleBoss boss, TurtleBossSO so) : base(boss)
    {
        this.turtleBoss = boss;
        this.so = so;
    }

    protected override void OnEnter()
    {
        firedCount = 0;
        nextFireTime = Time.time; // 즉시 첫 발 발사
    }

    protected override BossBTState OnTick()
    {
        if (!IsBossValid()) return BossBTState.Failure;

        Transform playerTr = GetPlayerTransform();
        if (playerTr == null) return BossBTState.Failure;

        if (firedCount < so.missileCount && Time.time >= nextFireTime)
        {
            FireMissile(playerTr);
            firedCount++;
            nextFireTime = Time.time + so.missileFireInterval;
        }

        return firedCount >= so.missileCount ? BossBTState.Success : BossBTState.Running;
    }

    private void FireMissile(Transform playerTarget)
    {
        if (so.missileBulletPrefab == null) return;

        Vector3 dir = (playerTarget.position - turtleBoss.transform.position).normalized;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        Quaternion rotation = Quaternion.Euler(0f, 0f, angle);
        Vector3 spawnPos = turtleBoss.transform.position;

        GameObject bulletObj = Object.Instantiate(so.missileBulletPrefab, spawnPos, rotation);
        Bullet bullet = bulletObj.GetComponent<Bullet>();
        if (bullet != null)
        {
            bullet.ATK = turtleBoss.atk;
            bullet.BulletSpeed = so.missileSpeed;
            bullet.BulletLifeTime = so.missileLifetime;
            bullet.IsDamage = true;
            bullet._direction = dir;
            if (bullet.targets == null)
                bullet.targets = new System.Collections.Generic.Dictionary<string, int>();
            bullet.targets["Player"] = (int)BulletTarget.Player;
        }
    }
}
