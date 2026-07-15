using UnityEngine;

[CreateAssetMenu(fileName = "StatIncrementSO", menuName = "ScriptableObject/StatIncrementSO")]
public class StatIncrementSO : ScriptableObject
{
    [Header("스탯 1당 상승 기본치 (Raw Base Increments)")]
    [Tooltip("공격력 1당 상승치 (ATK)")]
    public float atk = 0.5f;

    [Tooltip("체력 1당 상승치 (HP)")]
    public float hp = 5f;

    [Tooltip("이동속도 1당 상승치 (Speed)")]
    public float speed = 0.15f;

    [Tooltip("공격속도 1당 상승치 (AtkSpeed)")]
    public float atkSpeed = 0.05f; // +5%

    [Tooltip("장전속도 1당 상승치 (ReloadSpeed - 쿨타임 감소량)")]
    public float reloadSpeed = -0.06f; // +6% 장전속도 (장전 쿨타임 -6% 효과)

    [Tooltip("탄퍼짐(정밀도) 1당 상승치")]
    public float bulletSpread = -1f;

    [Tooltip("스킬 쿨타임 1당 상승치")]
    public float skillCoolTime = -0.25f;

    [Tooltip("치명타 1당 상승치")]
    public float critical = 5f;

    [Tooltip("장탄수 1당 상승치")]
    public float ammoMax = 2f;
}
