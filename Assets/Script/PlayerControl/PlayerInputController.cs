using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class PlayerInputController : TopDownCharacterController
{
    private bool IsAtking = false;

    private Camera _camera;
    public PlayerInput playerInput;
    //public int atkPercent; ����Ȯ���ε� ���ֵ� �ɵ�
    public bool IsMove = false;
    PlayerStatControl playerStatControl;
    //PlayerResultController playerResultController; �������� Ŭ����� ���� ����
    [HideInInspector]public bool siegeMode;
    [HideInInspector] public bool Flash;
    [HideInInspector] public bool cantMove;
    [HideInInspector] public bool cantSpacebar;

    private void Awake()
    {
        // �߰���
        coolTimeController = GetComponent<CoolTimeController>();

        playerStatControl = GetComponent<PlayerStatControl>();
        //playerResultController = GetComponent<PlayerResultController>();
        //playerStatControl.OnDieEvent += InputOff; ������ ���� �¿���
        //playerStatControl.OnRegenEvent += InputOn;
        //atkPercent = 100;
        siegeMode = false;
        Flash = false;
        cantMove = false;
        cantSpacebar = false;

        playerInput = GetComponent<PlayerInput>();
        playerInput.actions.FindAction("Move2").Disable(); // �ݴ�� ���� ��Ȱ��ȭ
        playerInput.actions.FindAction("Move").Enable();
        //playerInput.actions.FindAction("SiegeMode").Disable(); ������ ��Ȱ��ȭ 
        _camera = Camera.main;


        /*if (!GetComponent<PhotonView>().IsMine) ����䰡 ������ �ƴ϶�� ���� ó�� ���� ��Ƽ�� �����̵� ó���ε� �̱���ȯ�� �ʿ�x
        {
            Destroy(GetComponent<PlayerInputController>());
        }*/
    }
    public void ResetSetting()// �̵� ����Ǽ� ó��
    {
            if (cantMove)//������ ����ó�� 
            {
                playerInput.actions.FindAction("Move2").Disable();
                playerInput.actions.FindAction("Move").Disable();
        }
            else if (playerStatControl.isNoramlMove)
            {
                playerInput.actions.FindAction("Move2").Disable();
                playerInput.actions.FindAction("Move").Enable();
            }
            else
            {
                playerInput.actions.FindAction("Move2").Enable();
                playerInput.actions.FindAction("Move").Disable();
        }

            if (playerStatControl.isCanSkill) // ��ų ó��
            {
                playerInput.actions.FindAction("Skill").Enable();
            }
            else
            {
                playerInput.actions.FindAction("Skill").Disable();
            }

            if (playerStatControl.isCanAtk) // ���� ó��
            {
                playerInput.actions.FindAction("Attack").Enable();
            }
            else
            {
                playerInput.actions.FindAction("Attack").Disable();
            }

            if (cantSpacebar) // ������ ó�� 
            {
                playerInput.actions.FindAction("SiegeMode").Disable();
                playerInput.actions.FindAction("Roll").Enable();
                playerInput.actions.FindAction("Flash").Disable();
            }
            else if (siegeMode)
            {
                playerInput.actions.FindAction("SiegeMode").Enable();
                playerInput.actions.FindAction("Roll").Disable();
                playerInput.actions.FindAction("Flash").Disable();
            }
            else if (Flash)
            {
                playerInput.actions.FindAction("SiegeMode").Disable();
                playerInput.actions.FindAction("Roll").Disable();
                playerInput.actions.FindAction("Flash").Enable();
            }
    }
    public void OnMove(InputValue value) // ������ 
    {
        //Debug.Log("OnMove" + value.ToString());
        Vector2 moveInput = value.Get<Vector2>().normalized;
        CallMoveEvent(moveInput);
    }
    public void OnIsMove() // �����̴� �� ó��
    {
        if (playerInput.actions["IsMove"].ReadValue<float>() == 1)
        {
            playerStatHandler.MoveStartCall();
        }
        else
        {
            playerStatHandler.MoveEndCall();
        }
    }
    public void OnMove2(InputValue value) // �ݴ� ������ ó�� 
    {
        Vector2 moveInput = value.Get<Vector2>().normalized;
        CallMoveEvent(moveInput);
    }

    public void OnLook(InputValue value) // ���� ���� ó��
    {
        // Debug.Log("OnLook" + value.ToString());
        Vector2 newAim = value.Get<Vector2>();
        Vector2 worldPos = _camera.ScreenToWorldPoint(newAim);
        newAim = (worldPos - (Vector2)transform.position).normalized;

        CallLookEvent(newAim);
    }

    public void OnAttack(InputValue value)
    {
        int random = Random.Range(0, 100);
        //if (atkPercent >= random)
        //{
            //Debug.Log("OnAttack" + value.ToString());
            if (EventSystem.current != null)
            {
                //playerInput.actions["Attack"].ReadValue<float>()���콺 �����°� Ȯ���ϴ� ����
                if (!IsAtking && !EventSystem.current.IsPointerOverGameObject() && playerInput.actions["Attack"].ReadValue<float>() == 1)
                {
                CallAttackEvent(true);
                    //�߰���
                    CallAttackKeepEvent(true);
                }
                else
                {
                    CallAttackEvent(false);
                    //�߰���
                    CallAttackKeepEvent(false);
                }
            }
        //}
        else
        {
            CallAttackEvent(false);
        }
    }

    public void OnSkill(InputValue value)
    {
        //Debug.Log("OnSkill" + value.ToString());
        CallSkillEvent();
    }

    public void OnRoll(InputValue value)
    {
        // if (GetComponent<A3103> != null)
        //{
        //    CallSeizeEvent();
        //    return;
        //}
        //Debug.Log("OnRoll" + value.ToString());
        CallRollEvent();
    }

    public void OnSiegeMode(InputValue value)
    {
        CallSiegeModeEvent();
    }
    public void OnFlash(InputValue value)
    {
        CallFlashEvent();
    }
    public void OnAugmentCheck(InputValue value)
    {
        CallAugmentCheck();
    }

    public void OnReload(InputValue value)
    {
        //Debug.Log("OnReload" + value.ToString());
        CallReloadEvent();
    }

    public void InputOff()
    {
        playerInput.DeactivateInput();
    }

    public void InputOn()
    {
        playerInput.ActivateInput();
        ResetSetting();
    }

}
