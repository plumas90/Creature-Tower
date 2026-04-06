using UnityEngine;

/// <summary>
/// TheWorm 전용 ScriptableObject
/// EnemySO의 기본 스탯 + Worm 전용 밸런스 값
/// </summary>
[CreateAssetMenu(fileName = "WormSO", menuName = "ScriptableObject/WormSO", order = 1)]
public class WormSO : EnemySO
{
    [Header("Worm Intro Settings")]
    [Tooltip("그림자 낙하 시간 (Intro)")]
    public float introFallDuration = 2.5f;
    
    [Tooltip("Intro 후 대기 시간")]
    public float postIntroDelay = 1.0f;

    [Header("Worm Charge Settings (1단계)")]
    [Tooltip("차지 지속 시간")]
    public float chargeDuration = 2.0f;
    
    [Tooltip("차지 중 회전 속도 (도/초)")]
    public float chargeRotationSpeed = 720f;
    
    [Tooltip("차지 중 색상")]
    public Color chargeColor = Color.red;

    [Header("Worm Aim Settings (2단계)")]
    [Tooltip("조준 고정 시간 (플레이어 회피 타이밍)")]
    public float aimDuration = 0.2f;

    [Header("Worm Launch Settings (3단계)")]
    [Tooltip("발사 힘 (AddForce)")]
    public float launchForce = 50f;
    
    [Tooltip("정지 판정 속도 (이하면 멈춘 것으로 간주)")]
    public float stopVelocityThreshold = 0.5f;
    
    [Tooltip("최대 발사 지속 시간 (초과 시 강제 종료)")]
    public float maxLaunchDuration = 2.0f;
    
    [Tooltip("발사 후 대기 시간")]
    public float idleTimeAfterLaunch = 1.0f;

    [Header("Worm Knockback Settings")]
    [Tooltip("플레이어 넉백 힘")]
    public float playerKnockbackForce = 10f;
}
