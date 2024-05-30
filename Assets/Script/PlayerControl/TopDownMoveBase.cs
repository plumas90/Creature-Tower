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
        //if( _controller != null ) { Debug.Log("존재함"); }
        _rigidbody2D = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        _controller = GetComponent<TopDownCharacterController>();
        _controller.OnMoveEvent += Move;
        _controller.OnRollEvent += Roll;
        _controller.OnLookEvent += MousePos;
    }

    private void FixedUpdate()
    {
        if (!isRoll)
        {
            Debug.Log("무브테스트");
            ApplyMovment(_movemewtDirection);
        }
        else
        {
            ApplyRolling(mousePos);
        }
    }

    private void Move(Vector2 direction)
    {
        //Debug.Log("무브먼트무브테스트");
        //Debug.Log(direction);
        _movemewtDirection = direction;
    }

    private void ApplyMovment(Vector2 direction)
    {
        direction = direction * _controller.playerStatHandler.Speed.total;
        Debug.Log($" 속도는{_controller.playerStatHandler.Speed.total} 방향으 {direction}");
        _rigidbody2D.velocity = direction;
    }
    private void ApplyRolling(Vector2 direction)
    {
        direction = direction * _controller.playerStatHandler.Speed.total * 2f;
        _rigidbody2D.velocity = direction;
    }

    private void Roll()
    {
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

