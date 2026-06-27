using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// DashEnemy를 상속받아 Animator가 없는 환경에서 상태별 프레임 기반 수동 스프라이트 애니메이션을 재생하는 로봇 돌진 적.
/// </summary>
public class DashEnemyRobot : DashEnemy
{
    [Header("Sprite Animation (Manual Fallback)")]
    [Tooltip("걸어다닐 때(Chase) 재생할 스프라이트 배열")]
    [SerializeField] private Sprite[] chaseSprites;
    [Tooltip("차징(Windup) 중 재생할 스프라이트 배열")]
    [SerializeField] private Sprite[] windupSprites;
    [Tooltip("돌진(Dashing) 중 재생할 스프라이트 배열")]
    [SerializeField] private Sprite[] dashingSprites;

    [Tooltip("걸어다닐 때 스프라이트 전환 프레임 간격 (기본 3)")]
    [SerializeField] private int chaseFrameInterval = 3;
    [Tooltip("차징 중 스프라이트 전환 프레임 간격 (기본 2)")]
    [SerializeField] private int windupFrameInterval = 2;
    [Tooltip("돌진 중 스프라이트 전환 프레임 간격 (기본 1)")]
    [SerializeField] private int dashingFrameInterval = 1;

    private int _animFrameCount = 0;
    private int _spriteIndex = 0;
    private SpriteRenderer _robotSr;

    protected override void Start()
    {
        base.Start();
        _robotSr = GetComponentInChildren<SpriteRenderer>();
    }

    protected override void Update()
    {
        base.Update();
        UpdateManualAnimation();
    }

    private void UpdateManualAnimation()
    {
        if (_robotSr == null) return;

        Sprite[] targetSprites = null;
        int interval = 3;

        switch (_state)
        {
            case DashState.Chase:
                if (_rb2d != null && _rb2d.linearVelocity.magnitude > 0.05f)
                {
                    targetSprites = chaseSprites;
                    interval = chaseFrameInterval;
                }
                else
                {
                    if (chaseSprites != null && chaseSprites.Length > 0)
                    {
                        _robotSr.sprite = chaseSprites[0];
                    }
                    return;
                }
                break;
            case DashState.Windup:
                targetSprites = windupSprites;
                interval = windupFrameInterval;
                break;
            case DashState.Dashing:
                targetSprites = dashingSprites;
                interval = dashingFrameInterval;
                break;
            case DashState.Cooldown:
                if (chaseSprites != null && chaseSprites.Length > 0)
                {
                    _robotSr.sprite = chaseSprites[0];
                }
                return;
        }

        if (targetSprites == null || targetSprites.Length == 0)
            return;

        _animFrameCount++;
        if (_animFrameCount >= interval)
        {
            _animFrameCount = 0;
            _spriteIndex = (_spriteIndex + 1) % targetSprites.Length;
            _robotSr.sprite = targetSprites[_spriteIndex];
        }
    }

    protected override void TransitionTo(DashState next)
    {
        if (_state != next)
        {
            base.TransitionTo(next);
            _animFrameCount = 0;
            _spriteIndex = 0;
        }
    }
}
