using UnityEngine;
using System.Collections;

/// <summary>
/// TheWorm 보스: 그림자 낙하 Intro → 차지/조준/발사 패턴 반복
/// Unity Square 스프라이트를 사용한 테스트 버전
/// </summary>
public class TheWorm : BossBase
{
    [Header("Worm SO Reference")]
    [SerializeField] private WormSO wormSO;

    [Header("Visual")]
    [SerializeField] private GameObject shadowObject; // 그림자 오브젝트 (Intro용)

    [Header("Debug")]
    [SerializeField] private bool verboseBTLog = false;

    // ⚠️ TheWorm은 AddForce를 사용하므로 Dynamic이어야 함!
    // Inspector에서 이 값을 확인하고 수동으로 변경 가능
    [Header("Worm Physics Override")]
    [SerializeField] private bool overrideForceKinematic = true;
    [SerializeField] private bool wormShouldBeDynamic = true;

    private WormLaunchComponent launchComponent;
    private PlayerTargetingComponent playerTargeting;
    private SpriteRenderer spriteRenderer;
    private Animator wormAnimator;

    [Header("Worm Animation Sprites")]
    [SerializeField] private Sprite wormIdleSprite;
    
    [Header("Intro Animation")]
    [SerializeField] private Sprite dropInAirSprite;
    [SerializeField] private Sprite[] dropEndSprites; // worm_drop_end1 ~ end4

    [Header("Charge Animation")]
    [SerializeField] private Sprite charge1Sprite; // worm_charge1
    [SerializeField] private Sprite charge2Sprite; // worm_charge2
    [SerializeField] private Sprite charge3Sprite; // worm_charge3

    [Header("Animation Settings")]
    [SerializeField] private float animationFps = 10f;

    private Coroutine activeAnimCoroutine;

    // SO에서 로드된 값들
    private float introFallDuration;
    private float postIntroDelay;
    private float chargeDuration;
    private float chargeRotationSpeed;
    private float aimDuration;
    private float maxLaunchDuration;
    private float idleTimeAfterLaunch;
    private Color chargeColor;

    protected override void Awake()
    {
        base.Awake();

        // wormSO를 MainSO에 자동 할당
        if (wormSO != null)
            MainSO = wormSO;

        // 컴포넌트 초기화
        launchComponent = GetComponent<WormLaunchComponent>();
        if (launchComponent == null)
            launchComponent = gameObject.AddComponent<WormLaunchComponent>();

        playerTargeting = GetComponent<PlayerTargetingComponent>();
        if (playerTargeting == null)
            playerTargeting = gameObject.AddComponent<PlayerTargetingComponent>();

        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            Debug.LogWarning("[TheWorm] SpriteRenderer not found! Adding default SpriteRenderer.");
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            // 기본 스프라이트는 유니티에서 설정하거나, 없으면 Square 사용
        }
        
        wormAnimator = GetComponent<Animator>();
        if (wormAnimator != null)
        {
            wormAnimator.enabled = false;
        }

        // 질량을 옛날 값(1f)으로 설정 (CreatureBase의 10000f 강제 설정을 방지/덮어씀)
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.mass = 1f;
        }
    }

    public override void StatSet()
    {
        // wormSO를 MainSO에 자동 할당
        if (wormSO != null)
            MainSO = wormSO;

        // WormSO에서 값 로드
        if (wormSO != null)
        {
            introFallDuration = wormSO.introFallDuration;
            postIntroDelay = wormSO.postIntroDelay;
            chargeDuration = wormSO.chargeDuration;
            chargeRotationSpeed = wormSO.chargeRotationSpeed;
            aimDuration = wormSO.aimDuration;
            maxLaunchDuration = wormSO.maxLaunchDuration;
            idleTimeAfterLaunch = wormSO.idleTimeAfterLaunch;
            chargeColor = wormSO.chargeColor;
            
            // WormLaunchComponent 설정
            if (launchComponent != null)
            {
                launchComponent.SetLaunchSettings(
                    wormSO.launchForce, 
                    wormSO.stopVelocityThreshold, 
                    wormSO.playerKnockbackForce
                );
            }
            
            if (verboseBTLog)
                Debug.Log($"[TheWorm] WormSO 값 로드 완료: Charge={chargeDuration}s, Aim={aimDuration}s, RotSpeed={chargeRotationSpeed}");
        }
        else
        {
            Debug.LogWarning("[TheWorm] WormSO가 null입니다! MainSO를 WormSO로 캐스팅 시도...");
            
            // MainSO가 WormSO인 경우
            if (MainSO is WormSO mainWormSO)
            {
                wormSO = mainWormSO;
                // 재귀 호출하지 않고 직접 값 설정
                introFallDuration = mainWormSO.introFallDuration;
                postIntroDelay = mainWormSO.postIntroDelay;
                chargeDuration = mainWormSO.chargeDuration;
                chargeRotationSpeed = mainWormSO.chargeRotationSpeed;
                aimDuration = mainWormSO.aimDuration;
                maxLaunchDuration = mainWormSO.maxLaunchDuration;
                idleTimeAfterLaunch = mainWormSO.idleTimeAfterLaunch;
                chargeColor = mainWormSO.chargeColor;
                
                if (launchComponent != null)
                {
                    launchComponent.SetLaunchSettings(
                        mainWormSO.launchForce, 
                        mainWormSO.stopVelocityThreshold, 
                        mainWormSO.playerKnockbackForce
                    );
                }
            }
        }

        base.StatSet();
        
        Debug.Log($"[TheWorm] base.StatSet() 완료. Player from BossBase: {(Player != null ? Player.name : "NULL")}");

        // PlayerTargetingComponent에 플레이어 전달
        if (playerTargeting != null && Player != null)
        {
            playerTargeting.SetTarget(Player);
            
            if (verboseBTLog)
                Debug.Log($"[TheWorm] Player set to PlayerTargeting: {Player.name}");
        }

        if (verboseBTLog)
            Debug.Log("[TheWorm] StatSet complete. Starting Intro sequence...");
    }

    /// <summary>
    /// Intro 시작 전: 그림자 초기 설정 및 초기 스프라이트 준비
    /// </summary>
    public override void OnBossActivatedBeforeIntro()
    {
        base.OnBossActivatedBeforeIntro();

        if (wormAnimator != null)
            wormAnimator.enabled = false;

        // 시작 시 공중 낙하용 스프라이트 미리 할당
        if (spriteRenderer != null && dropInAirSprite != null)
        {
            spriteRenderer.sprite = dropInAirSprite;
        }

        // 그림자 활성화 (있다면)
        if (shadowObject != null)
            shadowObject.SetActive(true);

        if (verboseBTLog)
            Debug.Log("[TheWorm] Intro준비 완료. 그림자 활성화 및 drop_inair 스프라이트 셋팅.");
    }

    /// <summary>
    /// Intro 시작: 그림자 낙하 애니메이션
    /// </summary>
    protected override void OnBeforeIntroStart()
    {
        base.OnBeforeIntroStart();

        if (wormAnimator != null)
            wormAnimator.enabled = false;

        initialPosition = transform.position;
        StartAnimation(CoPlayIntroAnimation());

        if (verboseBTLog)
            Debug.Log($"[TheWorm] Intro 시작! {introFallDuration}초 동안 낙하");
    }

    private Vector3 initialPosition;

    private IEnumerator CoPlayIntroAnimation()
    {
        float totalDuration = ResolveIntroTime(); // e.g. 2.5s
        float endAnimDuration = 0.5f;
        float fallDuration = Mathf.Max(0.1f, totalDuration - endAnimDuration);

        // 화면 위 10f에서 낙하 시작
        Vector3 spawnPos = initialPosition + Vector3.up * 10f;
        transform.position = spawnPos;

        if (spriteRenderer != null && dropInAirSprite != null)
        {
            spriteRenderer.sprite = dropInAirSprite;
        }

        float elapsed = 0f;
        while (elapsed < fallDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fallDuration);
            transform.position = Vector3.Lerp(spawnPos, initialPosition, t);
            yield return null;
        }

        transform.position = initialPosition;

        // 중앙에 닿았을 때 end1 ~ end4 시퀀스 재생
        if (dropEndSprites != null && dropEndSprites.Length > 0)
        {
            float frameDelay = endAnimDuration / dropEndSprites.Length;
            for (int i = 0; i < dropEndSprites.Length; i++)
            {
                if (spriteRenderer != null && dropEndSprites[i] != null)
                {
                    spriteRenderer.sprite = dropEndSprites[i];
                }
                yield return new WaitForSeconds(frameDelay);
            }
        }

        if (spriteRenderer != null && wormIdleSprite != null)
        {
            spriteRenderer.sprite = wormIdleSprite;
        }
    }

    /// <summary>
    /// Intro 길이 반환
    /// </summary>
    protected override float ResolveIntroTime()
    {
        return Mathf.Max(0f, introFallDuration);
    }

    /// <summary>
    /// Intro 후 대기 시간
    /// </summary>
    protected override float ResolvePostIntroDelay()
    {
        return Mathf.Max(0f, postIntroDelay);
    }

    /// <summary>
    /// 전투 시작 (Intro 종료 후)
    /// </summary>
    public override void First()
    {
        base.First();

        // 인트로 애니메이션이 돌아가고 있으면 중단하고 기본 자세 세팅
        ResetToIdle();

        // TheWorm은 Dynamic Rigidbody가 필요하므로 물리 설정 오버라이드
        ConfigureWormPhysics();

        // 그림자 비활성화
        if (shadowObject != null)
            shadowObject.SetActive(false);

        Debug.Log($"[TheWorm] First() called - Battle start! PlayerTargeting valid: {(playerTargeting != null && playerTargeting.IsTargetValid)}");

        if (verboseBTLog)
            Debug.Log("[TheWorm] 전투 시작! BT 가동");
    }

    /// <summary>
    /// TheWorm은 AddForce를 사용하므로 Dynamic Rigidbody가 필요
    /// </summary>
    protected void ConfigureWormPhysics()
    {
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            Debug.LogError("[TheWorm] Rigidbody2D not found!");
            return;
        }

        if (overrideForceKinematic && wormShouldBeDynamic)
        {
            Debug.Log($"[TheWorm] Overriding Rigidbody: {rb.bodyType} → Dynamic (for AddForce support)");
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.useFullKinematicContacts = false;
            rb.mass = 1f; // 옛날 값(1f)으로 복원
        }
        
        Debug.Log($"[TheWorm] Rigidbody configured: BodyType={rb.bodyType}, GravityScale={rb.gravityScale}, Mass={rb.mass}");
    }

    /// <summary>
    /// 매 프레임 BT 실행
    /// </summary>
    private void FixedUpdate()
    {
        TickBehaviorTree();
    }

    /// <summary>
    /// BT 구성: Charge → Aim → Launch → Idle대기 → 반복
    /// </summary>
    protected override BossBTNode CreateBehaviorTree()
    {
        return new BossSelectorNode(
            // 살아있고 대기 상태가 아닐 때
            new BossSequenceNode(
                new BossConditionNode(() => live && !wait),
                
                // 공격 패턴: Charge → Aim → Launch → 잠깐 Idle
                new BossSequenceNode(
                    new BTTask_WormCharge(this, chargeDuration, chargeRotationSpeed, chargeColor),
                    new BTTask_WormAim(this, aimDuration),
                    new BTTask_WormLaunch(this, maxLaunchDuration),
                    new BTTask_Wait(this, idleTimeAfterLaunch) // 발사 후 휴식
                )
            ),
            
            // 기본: Running 유지
            new BossActionNode(() => BossBTState.Running)
        );
    }

    /// <summary>
    /// 플레이어 충돌 처리 (데미지 + 넉백)
    /// </summary>
    public override void OnCollisionEnter2D(Collision2D collision)
    {
        base.OnCollisionEnter2D(collision);

        // 플레이어 충돌
        PlayerStatControl player = collision.gameObject.GetComponent<PlayerStatControl>();
        if (player != null && launchComponent != null && launchComponent.IsLaunched)
        {
            // 넉백 (WormLaunchComponent에서 자동 처리됨)
            if (verboseBTLog)
                Debug.Log($"[TheWorm] 플레이어 충돌! 넉백 적용");
        }
    }

    /// <summary>
    /// 보스 사망
    /// </summary>
    public override void BossDie()
    {
        ResetToIdle();
        base.BossDie();

        if (verboseBTLog)
            Debug.Log("[TheWorm] 사망!");
        
        // 오브젝트 비활성화
        gameObject.SetActive(false);
    }

    protected override void OnDisable()
    {
        ResetToIdle();
        base.OnDisable();
    }

    private void StartAnimation(IEnumerator newAnim)
    {
        if (activeAnimCoroutine != null)
        {
            StopCoroutine(activeAnimCoroutine);
        }
        activeAnimCoroutine = StartCoroutine(newAnim);
    }

    public void PlayChargeStartAnimation()
    {
        StartAnimation(CoChargeStartAnimation());
    }

    private IEnumerator CoChargeStartAnimation()
    {
        float frameDelay = 1f / animationFps;

        // worm_idle
        if (spriteRenderer != null && wormIdleSprite != null)
            spriteRenderer.sprite = wormIdleSprite;
        yield return new WaitForSeconds(frameDelay);

        // charge1
        if (spriteRenderer != null && charge1Sprite != null)
            spriteRenderer.sprite = charge1Sprite;
        yield return new WaitForSeconds(frameDelay);

        // charge2
        if (spriteRenderer != null && charge2Sprite != null)
            spriteRenderer.sprite = charge2Sprite;
        yield return new WaitForSeconds(frameDelay);

        // hold charge3 (gathering phase)
        if (spriteRenderer != null && charge3Sprite != null)
            spriteRenderer.sprite = charge3Sprite;
    }

    public void PlayLaunchAnimation()
    {
        StartAnimation(CoLaunchAnimation());
    }

    private IEnumerator CoLaunchAnimation()
    {
        float frameDelay = 1f / animationFps;

        // charge2
        if (spriteRenderer != null && charge2Sprite != null)
            spriteRenderer.sprite = charge2Sprite;
        yield return new WaitForSeconds(frameDelay);

        // charge1
        if (spriteRenderer != null && charge1Sprite != null)
            spriteRenderer.sprite = charge1Sprite;
        yield return new WaitForSeconds(frameDelay);

        // worm_idle
        if (spriteRenderer != null && wormIdleSprite != null)
            spriteRenderer.sprite = wormIdleSprite;
    }

    public void ResetToIdle()
    {
        if (activeAnimCoroutine != null)
        {
            StopCoroutine(activeAnimCoroutine);
            activeAnimCoroutine = null;
        }
        if (spriteRenderer != null && wormIdleSprite != null)
        {
            spriteRenderer.sprite = wormIdleSprite;
        }
    }
}
