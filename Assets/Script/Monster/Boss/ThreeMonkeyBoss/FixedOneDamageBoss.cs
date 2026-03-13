using UnityEngine;

/// <summary>
/// 어떤 공격을 받아도 1의 피해만 반영하는 보스 베이스.
/// EnemySO/Intro/BT 게이트(wait, invincibility)는 BossBase 흐름을 그대로 사용한다.
/// </summary>
public class FixedOneDamageBoss : BossBase
{
    protected override float CalculateFinalDamage(float incomingDamage)
    {
        if (incomingDamage <= 0f) return 0f;
        return 1f;
    }
}
