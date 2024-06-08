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
    //public int atkPercent; 공격확률인데 없애도 될듯
    public bool IsMove = false;
    PlayerStatControl playerStatControl;
    //PlayerResultController playerResultController; 스테이지 클리어시 보상 선택
    [HideInInspector]public bool siegeMode;
    [HideInInspector] public bool Flash;
    [HideInInspector] public bool cantMove;
    [HideInInspector] public bool cantSpacebar;

    private void Awake()
    {
        // 추가함
        coolTimeController = GetComponent<CoolTimeController>();

        playerStatControl = GetComponent<PlayerStatControl>();
        //playerResultController = GetComponent<PlayerResultController>();
        //playerStatControl.OnDieEvent += InputOff; 죽을시 조작 온오프
        //playerStatControl.OnRegenEvent += InputOn;
        //atkPercent = 100;
        siegeMode = false;
        Flash = false;
        cantMove = false;
        cantSpacebar = false;

        playerInput = GetComponent<PlayerInput>();
        playerInput.actions.FindAction("Move2").Disable(); // 반대로 조작 비활성화
        playerInput.actions.FindAction("Move").Enable();
        //Debug.Log("시작세 완료");
        //playerInput.actions.FindAction("SiegeMode").Disable(); 시즈모드 비활성화 
        _camera = Camera.main;


        /*if (!GetComponent<PhotonView>().IsMine) 포톤뷰가 내것이 아니라면 제거 처리 포톤 멀티시 다중이동 처리인데 싱글전환후 필요x
        {
            Destroy(GetComponent<PlayerInputController>());
        }*/
    }
    public void ResetSetting()// 이동 경우의수 처리
    {
            if (cantMove)//움직임 관련처리 
            {
                playerInput.actions.FindAction("Move2").Disable();
                playerInput.actions.FindAction("Move").Disable();
            Debug.Log("비정상");
        }
            else if (playerStatControl.isNoramlMove)
            {
                playerInput.actions.FindAction("Move2").Disable();
                playerInput.actions.FindAction("Move").Enable();
            Debug.Log("정상");
            }
            else
            {
                playerInput.actions.FindAction("Move2").Enable();
                playerInput.actions.FindAction("Move").Disable();
            Debug.Log("비정상");
        }

            if (playerStatControl.isCanSkill) // 스킬 처리
            {
                playerInput.actions.FindAction("Skill").Enable();
            }
            else
            {
                playerInput.actions.FindAction("Skill").Disable();
            }

            if (playerStatControl.isCanAtk) // 공격 처리
            {
                playerInput.actions.FindAction("Attack").Enable();
            }
            else
            {
                playerInput.actions.FindAction("Attack").Disable();
            }

            if (cantSpacebar) // 구르기 처리 
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
    public void OnMove(InputValue value) // 움직임 
    {
        //Debug.Log("OnMove" + value.ToString());
        Vector2 moveInput = value.Get<Vector2>().normalized;
        CallMoveEvent(moveInput);
    }
    public void OnIsMove() // 움직이는 중 처리
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
    public void OnMove2(InputValue value) // 반대 움직임 처리 
    {
        Vector2 moveInput = value.Get<Vector2>().normalized;
        CallMoveEvent(moveInput);
    }

    public void OnLook(InputValue value) // 무기 에임 처리
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
                //playerInput.actions["Attack"].ReadValue<float>()마우스 눌리는거 확인하는 변수
                if (!IsAtking && !EventSystem.current.IsPointerOverGameObject() && playerInput.actions["Attack"].ReadValue<float>() == 1)
                {
                CallAttackEvent(true);
                    //추가함
                    CallAttackKeepEvent(true);
                }
                else
                {
                    CallAttackEvent(false);
                    //추가함
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
        Debug.Log("OnSkill" + value.ToString());
        CallSkillEvent();
    }

    public void OnRoll(InputValue value)
    {
        // if (GetComponent<A3103> != null)
        //{
        //    CallSeizeEvent();
        //    return;
        //}
        Debug.Log("OnRoll" + value.ToString());
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
        Debug.Log("OnReload" + value.ToString());
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
