using UnityEngine;

[CreateAssetMenu(fileName = "TurtleBossSO", menuName = "ScriptableObjects/Boss/TurtleBoss")]
public class TurtleBossSO : EnemySO
{
    // ────────────── Phase 전환 ──────────────
    [Header("Phase Settings")]
    [Tooltip("Phase 2로 전환되는 HP 비율 (0~1). 기본 0.3 = 30%")]
    public float phase2HpThreshold = 0.3f;

    // ────────────── 구르기 (Rolling) ──────────────
    [Header("Rolling Attack")]
    [Tooltip("구르기 시작 전 쿨다운 (초)")]
    public float rollingCooldown = 4f;

    [Tooltip("구르기 이동 속도")]
    public float rollingSpeed = 8f;

    [Tooltip("구르기 1회당 최대 벽 반사 횟수 (이 횟수 이후 구르기 종료)")]
    public int rollingBounceCount = 4;

    [Tooltip("구르기 데미지 배율 (boss.atk * multiplier)")]
    public float rollingDamageMultiplier = 2f;

    [Tooltip("구르기 중 벽 또는 플레이어 충돌 시 발사할 가시 패턴 여부 (Phase 1)")]
    public bool thornOnBounce = true;

    // ────────────── 가시 토네이도 (Thorn Tornado) ──────────────
    [Header("Thorn Tornado Attack")]
    [Tooltip("가시 공격 쿨다운 (초)")]
    public float thornTornadoCooldown = 5f;

    [Tooltip("방사형 가시 발사체 프리팹")]
    public GameObject thornBulletPrefab;

    [Tooltip("1번 가시 패턴 발사 방향 수 (기본 8방향)")]
    public int thornDirections = 8;

    [Tooltip("2번 패턴 발사 대기 시간 (초)")]
    public float thornSecondWaveDelay = 1f;

    [Tooltip("가시 비행 속도")]
    public float thornBulletSpeed = 6f;

    [Tooltip("가시 수명 (초)")]
    public float thornBulletLifetime = 3f;

    // ────────────── 미사일 (Missile) ──────────────
    [Header("Missile Attack")]
    [Tooltip("미사일 공격 쿨다운 (초)")]
    public float missileCooldown = 6f;

    [Tooltip("미사일 발사 개수 (Phase 1에서 순차 발사)")]
    public int missileCount = 3;

    [Tooltip("미사일 발사 간격 (초)")]
    public float missileFireInterval = 0.5f;

    [Tooltip("미사일 비행 속도")]
    public float missileSpeed = 5f;

    [Tooltip("미사일 수명 (초)")]
    public float missileLifetime = 4f;

    [Tooltip("미사일 발사체 프리팹")]
    public GameObject missileBulletPrefab;

    // ────────────── 애니메이션 ──────────────
    [Header("Animation")]
    [Tooltip("기본 걷기/대기 FPS")]
    public float idleAnimFps = 8f;

    [Tooltip("구르기 FPS")]
    public float rollingAnimFps = 12f;

    [Tooltip("걷기 스프라이트 배열 (스프라이트 시트에서 슬라이스한 프레임 0~4)")]
    public Sprite[] idleSprites;

    [Tooltip("구르기 스프라이트 배열 (스프라이트 시트에서 슬라이스한 프레임 5~11 등)")]
    public Sprite[] rollingSprites;
}
