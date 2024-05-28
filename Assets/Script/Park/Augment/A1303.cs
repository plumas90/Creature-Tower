using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class A1303 : MonoBehaviour
{
    private TopDownCharacterController controller;
    private PlayerStatControl stats;
    private CoolTimeController coolTimeController;
    private WeaponSystem _ws;
    private float damageCoeff;

    private void Awake()
    {

            controller = GetComponent<TopDownCharacterController>();
            stats = GetComponent<PlayerStatControl>();
            coolTimeController = GetComponent<CoolTimeController>();
            _ws = GetComponent<WeaponSystem>();
            if(_ws.weaponType != WeaponSystem.WeaponType.Charging)
            {
                _ws.weaponType = WeaponSystem.WeaponType.Charging;
                SetCharge();
            }

            //_ws.OnFinalDamageEvent += FinalAttackBonus;
            damageCoeff = 0.5f;

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

    private void FinalAttackBonus()
    {
        if (stats.CurAmmo == 0)
        {
            //
            //_ws.finalAttackCoeff += damageCoeff;
            Debug.Log($"계수 추가 완료 : {damageCoeff}");
        }        
    }
}
