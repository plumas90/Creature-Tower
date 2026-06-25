using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 무기를 소지하고 실시간 조준(Aiming)하는 고품질 원거리 몬스터 예제.
/// - NormalRangedEnemy의 이동/거리 유지/부채꼴 발사 로직을 상속받아 그대로 사용합니다.
/// - 플레이어 위치를 향해 무기 피벗(weaponPivot)을 실시간으로 회전 조준시킵니다.
/// - 무기 끝 총구(muzzlePoint) 트랜스폼에서 투사체가 스폰되도록 처리합니다.
/// - 조준 각도가 왼쪽을 향할 때 무기 스프라이트가 거꾸로 뒤집히는 것을 방지하기 위해
///   무기 피벗의 로컬 Y 스케일을 자동으로 반전(Upside-down 보정) 처리합니다.
/// </summary>
public class RangedWeaponEnemy : NormalRangedEnemy
{
    [Header("Weapon Configurations")]
    [Tooltip("무기의 회전 중심이 되는 피벗 Transform 입니다.")]
    [SerializeField] private Transform weaponPivot;

    [Tooltip("투사체가 생성되어 나갈 총구 Transform 입니다.")]
    [SerializeField] private Transform muzzlePoint;

    [Tooltip("조준 방향이 왼쪽일 때 무기 스프라이트가 뒤집어지지 않도록 로컬 Y 스케일을 반전할지 여부입니다.")]
    [SerializeField] private bool flipWeaponY = true;

    [Header("Weapon Aim Calibration")]
    [Tooltip("조준 회전 시 추가로 더해줄 각도 보정값입니다. (기본 0)")]
    [SerializeField] private float weaponAngleOffset = 0f;

    [Tooltip("조준할 때 무기 피벗의 기본 Y 스케일 배율입니다. (보통 1)")]
    [SerializeField] private float weaponBaseScaleY = 1f;

    [Header("Sprite Animation (Fallback if no Animator)")]
    [SerializeField] private Sprite[] walkSprites;
    [SerializeField] private Sprite idleSprite;
    [SerializeField] private float walkFrameRate = 0.2f;

    private int _currentWalkFrame = 0;
    private float _walkAnimTimer = 0f;
    private bool _isWalkingState = false;

    private Animator _rangedWeaponAnimator;
    private SpriteRenderer _rangedWeaponSr;

    protected override void Start()
    {
        base.Start();

        _rangedWeaponAnimator = GetComponentInChildren<Animator>();
        _rangedWeaponSr       = GetComponentInChildren<SpriteRenderer>();

        if (idleSprite == null && _rangedWeaponSr != null)
        {
            idleSprite = _rangedWeaponSr.sprite;
        }

        // 방어 코드: 인스펙터에서 깜빡하고 무기 피벗을 설정하지 않은 경우
        if (weaponPivot == null)
        {
            weaponPivot = transform.Find("WeaponPivot");
            if (weaponPivot == null)
            {
                // 차선책으로 하위에서 "Weapon"이 들어간 오브젝트를 찾음
                foreach (Transform child in GetComponentsInChildren<Transform>())
                {
                    if (child.name.ToLower().Contains("weapon") && child != transform)
                    {
                        weaponPivot = child;
                        break;
                    }
                }
            }
        }

        // 방어 코드: 총구 위치 설정 확인
        if (muzzlePoint == null && weaponPivot != null)
        {
            muzzlePoint = weaponPivot.Find("Muzzle");
            if (muzzlePoint == null)
            {
                // 없으면 무기 피벗 하위에 있는 아무 자식이나 찾음
                if (weaponPivot.childCount > 0)
                {
                    muzzlePoint = weaponPivot.GetChild(0);
                }
                else
                {
                    muzzlePoint = weaponPivot;
                }
            }
        }
    }

    protected override void Update()
    {
        base.Update();

        // 실시간 무기 조준 및 반전 보정
        AimAtPlayer();

        // 걷기 애니메이션 처리 (애니메이터가 없고 walkSprites가 지정되어 있을 때)
        if (_rangedWeaponAnimator == null && _isWalkingState && walkSprites != null && walkSprites.Length > 0 && _rangedWeaponSr != null)
        {
            _walkAnimTimer += Time.deltaTime;
            if (_walkAnimTimer >= walkFrameRate)
            {
                _walkAnimTimer = 0f;
                _currentWalkFrame = (_currentWalkFrame + 1) % walkSprites.Length;
                _rangedWeaponSr.sprite = walkSprites[_currentWalkFrame];
            }
        }
    }

    // 걷기 상태 전이 시 스프라이트 동기화
    private void SetWalk(bool value)
    {
        _isWalkingState = value;
        if (_rangedWeaponAnimator != null)
        {
            // 부모의 Animator 변수가 private이므로 Animator가 발견되었을 때만 처리
            _rangedWeaponAnimator.SetBool("IsWalking", value);
        }

        if (_rangedWeaponAnimator == null && _rangedWeaponSr != null)
        {
            if (!value && idleSprite != null)
            {
                _rangedWeaponSr.sprite = idleSprite;
            }
        }
    }

    // 부모의 SetWalk 덮어쓰기 지원을 위해 override 틱 연동
    protected override void OnTick()
    {
        base.OnTick();
        
        // 이동 여부에 따른 수동 걷기 상태 동기화
        if (_rb2d != null)
        {
            bool isMoving = _rb2d.linearVelocity.magnitude > 0.05f;
            SetWalk(isMoving);
        }
    }

    /// <summary>
    /// 플레이어를 실시간으로 매끄럽게 조준하도록 무기 피벗을 회전시키고
    /// 좌우 조준 상황에 맞추어 스프라이트 뒤집힘을 보정합니다.
    /// </summary>
    private void AimAtPlayer()
    {
        if (Player == null || weaponPivot == null || isDead || !live)
            return;
 
        Vector3 targetPos = Player.transform.position;
        Vector3 dir = (targetPos - weaponPivot.position).normalized;
        
        // Atan2를 사용하여 조준할 회전 각도를 계산 (오른쪽이 0도 기준)
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
 
        // 조준 방향에 따른 오프셋 부호 보정 (대칭 보정)
        float currentOffset = weaponAngleOffset;
        if (flipWeaponY && dir.x < 0f)
        {
            currentOffset = -weaponAngleOffset; // 왼쪽 조준 시 각도 오프셋 부호 반전
        }
 
        // 무기 피벗의 Z축 회전을 업데이트 (보정 각도 반영)
        weaponPivot.rotation = Quaternion.Euler(0f, 0f, angle + currentOffset);
 
        // 조준 방향에 따른 무기 상하 반전 보정
        if (flipWeaponY)
        {
            Vector3 localScale = weaponPivot.localScale;
            
            // 플레이어가 몬스터 기준 왼쪽(dir.x < 0)에 있는 경우 Y축 스케일을 음수로 만들어 뒤집어줌
            if (dir.x < 0f)
            {
                localScale.y = -Mathf.Abs(weaponBaseScaleY);
            }
            else
            {
                localScale.y = Mathf.Abs(weaponBaseScaleY);
            }
            
            weaponPivot.localScale = localScale;
        }
    }

    /// <summary>
    /// Bullet이 스폰될 정확한 위치를 총구(muzzlePoint)의 위치로 오버라이드합니다.
    /// </summary>
    protected override Vector3 GetBulletSpawnPosition()
    {
        if (muzzlePoint != null)
        {
            return muzzlePoint.position;
        }
        return base.GetBulletSpawnPosition();
    }
}
