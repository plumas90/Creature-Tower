using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class A0221 : MonoBehaviour
{
    private TopDownCharacterController controller;
    private PlayerStatControl stats;
    private CoolTimeController coolTimeController;
    private WeaponSystem _ws;

    private void Awake()
    {
            controller = GetComponent<TopDownCharacterController>();
            stats = GetComponent<PlayerStatControl>();
            coolTimeController = GetComponent<CoolTimeController>();
            _ws = GetComponent<WeaponSystem>();
            if (_ws.weaponType != WeaponSystem.WeaponType.Charging)
            {
                _ws.weaponType = WeaponSystem.WeaponType.Charging;
                SetCharge();
            }
    }

    private void SetCharge()
    {
        /*
        controller.OnAttackKeepEvent += coolTimeController.TimeCount;
        controller.OnAttackEvent -= coolTimeController.AttackCoolTime;
        controller.OnAttackEvent -= _ws.Shooting;
        controller.OnChargeAttackEvent += _ws.Charging;
        controller.playerStatHandler.IsChargeAttack = true;
        */
    }
}
