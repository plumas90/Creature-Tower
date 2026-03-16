using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TopDownCharacterController : MonoBehaviour
{
    public event Action<Vector2> OnMoveEvent;
    public event Action<Vector2> OnLookEvent;
    public event Action OnAttackEvent;
    public event Action OnEndAttackEvent;
    public event Action OnSkillEvent;
    public event Action OnEndSkillEvent;
    public event Action OnRollEvent;
    public event Action OnEndRollEvent;
    public event Action OnReloadEvent;
    public event Action OnEndReloadEvent;
    //public event Action OnStartSkillEvent;
    public event Action OnSiegeModeEvent;
    public event Action OnFlashEvent;
    public event Action OnAugmentcheck;

    public event Action SkillMinusEvent;
    public event Action<bool> OnAttackKeepEvent;
    public event Action OnChargeAttackEvent;


    public PlayerStatControl playerStatHandler;
    public TopDownMoveBase topDownMovement;
    public CoolTimeController coolTimeController;

    public bool IsSkillRollActive { get; private set; }
    private bool isCompulsoryRollRunning;



    private bool AtkKeyhold = false;

    private void Awake()
    {
        //coolTimeController = GetComponent<CoolTimeController>();
    }
    private void Update()
    {
        if (AtkKeyhold)
        {
            if
                (
                !topDownMovement.isRoll
                && playerStatHandler.CurAmmo > 0
                && playerStatHandler.CanFire
                && !playerStatHandler.IsExternalFireBlocked
                && (playerStatHandler.CanReload)  // �Ϲݰ��� ���Ǻ�
                    //|| (!playerStatHandler.CanReload && GetComponent<CoolTimeController>().isKeepCount)) // ������ ���Ǻ�
                )
            {
                OnAttackEvent?.Invoke();

            }
            else
            {
                //Debug.Log("���� �� �� �����ϴ�");
            }
        }
        else
        {
            if (
                !topDownMovement.isRoll
                && playerStatHandler.CurAmmo >= 0
                && playerStatHandler.CanFire
                && !playerStatHandler.IsExternalFireBlocked
                && playerStatHandler.CanReload
                )
                //&& coolTimeController.bulletNum > 0
            {
                OnChargeAttackEvent?.Invoke();
            }
        }
    }

    public void CallMoveEvent(Vector2 direction)
    {
        OnMoveEvent?.Invoke(direction);
    }

    public void CallLookEvent(Vector2 direction)
    {
        OnLookEvent?.Invoke(direction);
    }

    public void CallAttackEvent(bool hold)
    {
        AtkKeyhold = hold;
    }

    public void CallAttackKeepEvent(bool hold)
    {
        OnAttackKeepEvent?.Invoke(hold);
    }

    public void CallAttackEndEvent()
    {
        OnEndAttackEvent?.Invoke();
    }

    public void CallSkillEvent()
    {
        if (!playerStatHandler.CanSkill)
        {
            return;
        }

        // 스킬 구르기 세션이 이미 진행 중이면 중복 시전을 막는다.
        if (isCompulsoryRollRunning)
        {
            return;
        }

        Debug.Log("CallSkillEvent triggered");
        OnSkillEvent?.Invoke();
    }
    public void SkillReset()
    {
        SkillMinusEvent?.Invoke();
    }
    public void CallEndSkillEvent()
    {
        OnEndSkillEvent?.Invoke();
    }

    public void CallRollEvent()
    {
        // 스킬 구르기 세션 중 일반 구르기 입력은 무시한다.
        if (isCompulsoryRollRunning)
        {
            return;
        }

        //A1207 a1207 = gameObject.GetComponent<A1207>();
        //if (a1207 != null)
        //{
        //    a1207.Change();
        //}
        //else 
        if (playerStatHandler.CanRoll)
        {
            OnRollEvent?.Invoke();
            playerStatHandler.CurRollStack -= 1;
            //Debug.Log($"������ ���� ���� : {playerStatHandler.CurRollStack} ����");
            playerStatHandler.CanRoll = false;
            playerStatHandler.Invincibility = true;
            Invoke("CallEndRollEvent", 0.8f);
        }
        else
        {
            //Debug.Log("������ ��Ÿ�� �Դϴ�");
        }
    }
    public void BeginSkillRoll()
    {
        IsSkillRollActive = true;
    }

    public void EndSkillRoll()
    {
        isCompulsoryRollRunning = false;
        IsSkillRollActive = false;
    }

    public void CompulsoryRoll()
    {
        // 한 세션에서 중복 강제 구르기를 막는다.
        if (isCompulsoryRollRunning)
        {
            return;
        }

        isCompulsoryRollRunning = true;
        IsSkillRollActive = true;
        OnRollEvent?.Invoke();
    }

    public void CompulsoryRollEnd()
    {
        if (!isCompulsoryRollRunning)
        {
            return;
        }

        isCompulsoryRollRunning = false;
        OnEndRollEvent?.Invoke();
        IsSkillRollActive = false;
    }
    public void CallEndRollEvent()
    {
        //Debug.Log("������ �� �̺�Ʈ");
        playerStatHandler.CanRoll = true;
        playerStatHandler.Invincibility = false;
        OnEndRollEvent?.Invoke();
    }
    public void CallSiegeModeEvent() // ��� ���� ��� üũ
    {
        OnSiegeModeEvent?.Invoke();
    }
    public void CallFlashEvent()// ��� ���� üũ 
    {

        if (playerStatHandler.CanRoll)
        {
            OnFlashEvent?.Invoke();
            playerStatHandler.CurRollStack -= 1;
            playerStatHandler.CanRoll = false;
            playerStatHandler.Invincibility = true;
            CallEndRollEvent();
        }
        else
        {
            Debug.Log("Flash is on cooldown");
        }
    }

    public void CallReloadEvent()
    {
        if (playerStatHandler.CanReload && playerStatHandler.CurAmmo != playerStatHandler.AmmoMax.total)
        {
            Debug.Log("Reload started");
            OnReloadEvent?.Invoke();
        }
    }
    public void CallAugmentCheck() // ��Ű ������ ���� ��� üũ 
    {
        OnAugmentcheck?.Invoke();
    }
    public void CallEndReloadEvent()
    {
        OnEndReloadEvent?.Invoke();
    }

}
