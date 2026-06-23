using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TopDownMoveBase : MonoBehaviour
{
    TopDownCharacterController _controller;

    private Vector2 _movemewtDirection = Vector2.zero;
    private Rigidbody2D _rigidbody2D;
    private Vector2 mousePos;

    [HideInInspector] public bool isRoll = false;

    private void Awake()
    {
        //_controller = GetComponent<TopDownCharacterController>();
        //if( _controller != null ) { Debug.Log("������"); }
        _rigidbody2D = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        _controller = GetComponent<TopDownCharacterController>();
        _controller.OnMoveEvent += Move;
        _controller.OnRollEvent += Roll;
        _controller.OnLookEvent += MousePos;
    }

    [HideInInspector] public bool isKnockback = false;

    private void FixedUpdate()
    {
        if (isKnockback) return; // 넉백 중에는 이동 속도 강제 지정을 건너뜀

        if (!isRoll)
        {
            ApplyMovment(_movemewtDirection);
        }
        else
        {
            ApplyRolling(mousePos);
        }
    }

    public void ApplyKnockback(Vector2 direction, float force, float duration)
    {
        if (_rigidbody2D == null) _rigidbody2D = GetComponent<Rigidbody2D>();
        
        isKnockback = true;
        _rigidbody2D.linearVelocity = Vector2.zero; // 이전 움직임 속도 리셋
        _rigidbody2D.AddForce(direction.normalized * force, ForceMode2D.Impulse);
        
        CancelInvoke("EndKnockback");
        Invoke("EndKnockback", duration);
    }

    private void EndKnockback()
    {
        isKnockback = false;
    }

    private void Move(Vector2 direction)
    {
        _movemewtDirection = direction;
    }

    private void ApplyMovment(Vector2 direction)
    {
        direction = direction * _controller.playerStatHandler.Speed.total;
        //Debug.Log($" �ӵ���{_controller.playerStatHandler.Speed.total} ������ {direction}");
        _rigidbody2D.linearVelocity = direction;
    }
    private void ApplyRolling(Vector2 direction)
    {
        direction = direction * _controller.playerStatHandler.Speed.total * 2f;
        _rigidbody2D.linearVelocity = direction;
    }

    private void Roll()
    {
        CancelInvoke("EndRoll");
        isRoll = true;
        Invoke("EndRoll", 0.6f);
    }

    public void ForceRoll()
    {
        CancelInvoke("EndRoll");
        isRoll = true;
        Invoke("EndRoll", 0.6f);
    }

    private void EndRoll()
    {
        isRoll = false;
    }

    private void MousePos(Vector2 _mousePos)
    {
        if (!isRoll)
        {
            mousePos = _mousePos.normalized;
        }
    }
}

