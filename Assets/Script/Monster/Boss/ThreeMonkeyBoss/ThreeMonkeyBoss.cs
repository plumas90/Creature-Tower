using System.Collections;
using System.Collections.Generic;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine;
public class ThreeMonkeyBoss : BossBase
{
    public BaseMonkey secondMouseMonkeySO;
    public BaseMonkey LastEarMonkeySO;


    public GameObject tower3EarOBJ;
    public GameObject tower2MouseOBJ;
    public GameObject tower1EyeOBJ;

    public GameObject prefab1TowerEye;
    public GameObject prefab2TowerMouse;

    [Header("Detached Prefabs (Optional Override)")]
    public GameObject eyeDetachedPrefab;
    public GameObject earDetachedPrefab;
    public GameObject mouthDetachedPrefab;

    private GameObject _tower1Eye;
    private GameObject _tower2Mouse;

    private Collider2D _mainCollider2D;
    private BallisticMovementComponent ballisticMovement;


    private Vector3 eyeSlotLocalPos;
    private Vector3 mouthSlotLocalPos;
    private Vector3 earSlotLocalPos;

    [Header("Monkey Effect")]
    public float collisionEffectDuration = 1f;

    [Header("Intro Animation")]
    [Min(0f)] public float introRotateAngle = 35f;
    [Min(0f)] [SerializeField] private float postIntroDelay = 1f;
    [SerializeField] private AnimationClip eyeIntroClip;
    [SerializeField] private AnimationClip mouseIntroClip;
    [SerializeField] private AnimationClip earIntroClip;
    [SerializeField] private string eyeIntroStateName = "EyeMonkeyIntro";
    [SerializeField] private string mouseIntroStateName = "MouseMonkeyIntro";
    [SerializeField] private string earIntroStateName = "EarsMonkeyIntro";
    [SerializeField] private Animator eyeIntroAnimator;
    [SerializeField] private Animator mouseIntroAnimator;
    [SerializeField] private Animator earIntroAnimator;
    [SerializeField] private bool verboseIntroLog = true;
    [Header("BT Debug")]
    [SerializeField] private bool verboseBTLog = true;
    [Min(0.1f)] [SerializeField] private float btHeartbeatInterval = 0.5f;

    private bool introAutoConfiguredLogged;
    private readonly List<PlayableGraph> introPlayableGraphs = new List<PlayableGraph>();
    private readonly List<Animator> frozenAnimators = new List<Animator>();
    private readonly Dictionary<string, bool> btConditionTrace = new Dictionary<string, bool>();
    private readonly Dictionary<string, BossBTState> btStateTrace = new Dictionary<string, BossBTState>();
    private float nextBtHeartbeatTime;

    private bool eyeDetached;
    private bool earDetached;
    private bool companionCleared;

    // 최초 하단은 눈 원숭이
    private MonkeyEffectType currentBottomEffect = MonkeyEffectType.Eye;
    private Vector2 lastHitBulletDirection = Vector2.right;

    private float btMoveSpeedMultiplier = 1f;

    [Header("Ballistic Movement")]
    [SerializeField] private Vector2 startDirection = new Vector2(1f, -1f); // 5 o'clock
    [SerializeField] [Min(0f)] private float detachSpawnOutwardOffset = 0.35f;
    [SerializeField] [Min(0f)] private float detachSpawnSafeRadius = 0.8f; // 안전 반경
    [SerializeField] [Range(4, 32)] private int detachSpawnSafeSearchAttempts = 16; // 안전 위치 찾기 시도 횟수
    [SerializeField] [Min(0f)] private float detachSpawnMaxSearchDistance = 3.0f; // 최대 탐색 거리

    private readonly Collider2D[] detachSpawnOverlapBuffer = new Collider2D[8];

    public override void StatSet() 
    {
        StopIntroPlayableGraphs();
        base.StatSet();
        _mainCollider2D = GetComponent<Collider2D>();

        // BallisticMovementComponent 초기화
        ballisticMovement = GetComponent<BallisticMovementComponent>();
        if (ballisticMovement == null)
            ballisticMovement = gameObject.AddComponent<BallisticMovementComponent>();

        // 초기 방향 설정
        Vector2 initialDir = startDirection.sqrMagnitude > 0.0001f ? startDirection.normalized : new Vector2(1f, -1f).normalized;
        ballisticMovement.CurrentDirection = initialDir;
        ballisticMovement.SpeedMultiplier = btMoveSpeedMultiplier;

        // Blackboard에 초기 방향 저장
        blackboard.Set("MoveDirection", initialDir);

        // 분리형 보스 카운트 규칙: 시작은 본체 1마리
        bossCount = 1;

        eyeDetached = false;
        earDetached = false;
        companionCleared = false;
        currentBottomEffect = MonkeyEffectType.Eye;

        EnsureVisualState();
        ResolveBTParams(MainSO);

        if (!TryPlayAnimatorIntro() && IntroTime > 0f)
            StartCoroutine(PlayStackIntroAnimation(IntroTime));

        if (introPlayableGraphs.Count > 0 && IntroTime > 0f)
            StartCoroutine(StopIntroPlayableGraphsAfterDelay(IntroTime + 0.1f));

        Debug.Log($"[ThreeMonkeyBoss] StatSet done | pos={transform.position} | active={gameObject.activeSelf}");
    }

    public override void OnBossActivatedBeforeIntro()
    {
        base.OnBossActivatedBeforeIntro();

        EnsurePartIntroAnimators();
        FreezeAnimator(eyeIntroAnimator);
        FreezeAnimator(mouseIntroAnimator);
        FreezeAnimator(earIntroAnimator);

        if (verboseIntroLog)
            Debug.Log($"[ThreeMonkeyBoss][Intro] pre-intro freeze | eye={eyeIntroAnimator != null} mouse={mouseIntroAnimator != null} ear={earIntroAnimator != null}");
    }

    protected override void OnBeforeIntroStart()
    {
        RestoreFrozenAnimators();
    }

    protected override float ResolveIntroTime()
    {
        float clipDuration = GetConfiguredIntroDuration();
        if (clipDuration > 0f)
            return clipDuration;

        return base.ResolveIntroTime();
    }

    protected override float ResolvePostIntroDelay()
    {
        return Mathf.Max(0f, postIntroDelay);
    }

    private void EnsureVisualState()
    {
        if (tower1EyeOBJ != null) tower1EyeOBJ.SetActive(true);
        if (tower2MouseOBJ != null) tower2MouseOBJ.SetActive(true);
        if (tower3EarOBJ != null) tower3EarOBJ.SetActive(true);

        CacheStackSlotPositions();

        // 레이어 규칙상 몬스터는 플레이어(10)보다 위쪽 정렬이 필요할 수 있어 명시적으로 보정
        ApplyRendererState(tower1EyeOBJ, 13);
        ApplyRendererState(tower2MouseOBJ, 14);
        ApplyRendererState(tower3EarOBJ, 15);
    }

    private void CacheStackSlotPositions()
    {
        if (tower1EyeOBJ != null)
            eyeSlotLocalPos = tower1EyeOBJ.transform.localPosition;

        if (tower2MouseOBJ != null)
            mouthSlotLocalPos = tower2MouseOBJ.transform.localPosition;

        if (tower3EarOBJ != null)
            earSlotLocalPos = tower3EarOBJ.transform.localPosition;
    }

    private void ApplyRendererState(GameObject go, int sortingOrder)
    {
        if (go == null) return;
        var sr = go.GetComponent<SpriteRenderer>();
        if (sr == null) return;

        sr.enabled = true;
        sr.color = Color.white;
        sr.sortingOrder = sortingOrder;
    }


    // Update is called once per frame
    public void Update()
    {
        TraceBtHeartbeat();
    }

    private void FixedUpdate()
    {
        TickBehaviorTree();
    }

    protected override BossBTNode CreateBehaviorTree()
    {
        return new BossSelectorNode(
            new BossSequenceNode(
                new BossConditionNode(() => TraceCondition("Gate(live && !wait)", live && !wait)),
                new BTTask_BallisticMove(this, "MoveDirection")
            ),
            new BossActionNode(() => TraceState("IdleFallback", BossBTState.Running))
        );
    }

    private bool TraceCondition(string key, bool value)
    {
        if (!verboseBTLog)
            return value;

        if (!btConditionTrace.TryGetValue(key, out bool prev) || prev != value)
        {
            btConditionTrace[key] = value;
            Debug.Log($"[ThreeMonkeyBoss][BT] {key} => {value}");
        }

        return value;
    }

    private BossBTState TraceState(string key, BossBTState state)
    {
        if (!verboseBTLog)
            return state;

        if (!btStateTrace.TryGetValue(key, out BossBTState prev) || prev != state)
        {
            btStateTrace[key] = state;
            Debug.Log($"[ThreeMonkeyBoss][BT] {key} => {state}");
        }

        return state;
    }

    private void TraceBtHeartbeat()
    {
        if (!verboseBTLog)
            return;

        if (Time.time < nextBtHeartbeatTime)
            return;

        nextBtHeartbeatTime = Time.time + btHeartbeatInterval;

//        Debug.Log($"[ThreeMonkeyBoss][BT] heartbeat | live={live} wait={wait} inv={invincibility} brain={brainRunning} speed={speed:F2} mul={btMoveSpeedMultiplier:F2} dir={direction}");
    }

    private void ResolveBTParams(EnemySO so)
    {
        if (so == null)
        {
            btMoveSpeedMultiplier = 1f;
            return;
        }

        // 기존 SO(필드 추가 이전 에셋)에는 0이 저장돼 있을 수 있어, 레거시-safe 기본값으로 보정한다.
        btMoveSpeedMultiplier = so.btMoveSpeedMultiplier > 0f ? so.btMoveSpeedMultiplier : 1f;
    }

    private float GetConfiguredIntroDuration()
    {
        EnsurePartIntroAnimators();

        float eye = ResolvePartIntroDuration(eyeIntroClip, eyeIntroAnimator, eyeIntroStateName);
        float mouse = ResolvePartIntroDuration(mouseIntroClip, mouseIntroAnimator, mouseIntroStateName);
        float ear = ResolvePartIntroDuration(earIntroClip, earIntroAnimator, earIntroStateName);

        return Mathf.Max(eye, Mathf.Max(mouse, ear));
    }

    private bool TryPlayAnimatorIntro()
    {
        EnsurePartIntroAnimators();

        bool eyePlayed = PlayPartIntro("Eye", eyeIntroAnimator, eyeIntroClip, eyeIntroStateName);
        bool mousePlayed = PlayPartIntro("Mouse", mouseIntroAnimator, mouseIntroClip, mouseIntroStateName);
        bool earPlayed = PlayPartIntro("Ear", earIntroAnimator, earIntroClip, earIntroStateName);

        return eyePlayed || mousePlayed || earPlayed;
    }

    private void EnsurePartIntroAnimators()
    {
        EnsurePartObjects();

        if (eyeIntroAnimator == null)
            eyeIntroAnimator = GetAnimatorFromObject(tower1EyeOBJ);

        if (mouseIntroAnimator == null)
            mouseIntroAnimator = GetAnimatorFromObject(tower2MouseOBJ);

        if (earIntroAnimator == null)
            earIntroAnimator = GetAnimatorFromObject(tower3EarOBJ);

        if (!introAutoConfiguredLogged)
        {
            introAutoConfiguredLogged = true;
            Debug.Log($"[ThreeMonkeyBoss] Intro auto-map | eye={(eyeIntroAnimator != null)} mouse={(mouseIntroAnimator != null)} ear={(earIntroAnimator != null)}");
        }
    }

    private void EnsurePartObjects()
    {
        if (tower1EyeOBJ == null)
            tower1EyeOBJ = FindChildByKeywords("eye", "monkeyeye", "1monkey");

        if (tower2MouseOBJ == null)
            tower2MouseOBJ = FindChildByKeywords("mouse", "monkeymouse", "2monkey");

        if (tower3EarOBJ == null)
            tower3EarOBJ = FindChildByKeywords("ear", "ears", "monkeyear", "3monkey");
    }

    private GameObject FindChildByKeywords(params string[] keywords)
    {
        if (keywords == null || keywords.Length == 0)
            return null;

        Transform[] children = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            Transform tr = children[i];
            if (tr == null || tr == transform)
                continue;

            string n = tr.name.ToLowerInvariant();
            for (int k = 0; k < keywords.Length; k++)
            {
                string kw = keywords[k];
                if (string.IsNullOrEmpty(kw))
                    continue;

                if (n.Contains(kw.ToLowerInvariant()))
                    return tr.gameObject;
            }
        }

        return null;
    }

    private Animator GetAnimatorFromObject(GameObject go)
    {
        if (go == null)
            return null;

        Animator animator = go.GetComponent<Animator>();
        if (animator == null)
            animator = go.GetComponentInChildren<Animator>(true);

        return animator;
    }

    private float ResolvePartIntroDuration(AnimationClip configuredClip, Animator animator, string stateName)
    {
        AnimationClip clip = GetPartIntroClip(configuredClip, animator, stateName);
        return clip != null ? Mathf.Max(0f, clip.length) : 0f;
    }

    private bool PlayPartIntro(string partName, Animator animator, AnimationClip configuredClip, string stateName)
    {
        if (animator == null)
        {
            if (verboseIntroLog)
                Debug.LogWarning($"[ThreeMonkeyBoss][Intro] {partName}: animator not found");
            return false;
        }

        // 클립이 명시되어 있으면 상태보다 우선해서 직접 재생한다.
        if (configuredClip != null)
        {
            bool clipPlayed = PlayClipWithPlayable(animator, configuredClip);
            if (verboseIntroLog)
                Debug.Log($"[ThreeMonkeyBoss][Intro] {partName}: direct clip '{configuredClip.name}' => {clipPlayed}");
            return clipPlayed;
        }

        if (animator.runtimeAnimatorController == null)
        {
            if (verboseIntroLog)
                Debug.LogWarning($"[ThreeMonkeyBoss][Intro] {partName}: runtimeAnimatorController missing and no configured clip");
            return false;
        }

        if (!string.IsNullOrEmpty(stateName) && TryPlayState(animator, stateName))
        {
            if (verboseIntroLog)
                Debug.Log($"[ThreeMonkeyBoss][Intro] {partName}: state '{stateName}'");
            return true;
        }

        if (TryPlayIntroNamedState(animator))
        {
            if (verboseIntroLog)
                Debug.Log($"[ThreeMonkeyBoss][Intro] {partName}: fallback intro-named state");
            return true;
        }

        AnimationClip clip = GetPartIntroClip(null, animator, stateName);
        bool played = PlayClipWithPlayable(animator, clip);
        if (verboseIntroLog)
            Debug.Log($"[ThreeMonkeyBoss][Intro] {partName}: controller clip fallback '{(clip != null ? clip.name : "null")}' => {played}");

        return played;
    }

    private bool TryPlayState(Animator animator, string stateName)
    {
        if (animator == null || animator.runtimeAnimatorController == null)
            return false;

        int stateHash = Animator.StringToHash(stateName);
        if (!animator.HasState(0, stateHash))
            return false;

        animator.Play(stateHash, 0, 0f);
        animator.Update(0f);
        return true;
    }

    private bool TryPlayIntroNamedState(Animator animator)
    {
        if (animator == null || animator.runtimeAnimatorController == null)
            return false;

        AnimationClip[] clips = animator.runtimeAnimatorController.animationClips;
        if (clips == null || clips.Length == 0)
            return false;

        for (int i = 0; i < clips.Length; i++)
        {
            AnimationClip clip = clips[i];
            if (clip == null)
                continue;

            if (clip.name.IndexOf("intro", System.StringComparison.OrdinalIgnoreCase) < 0)
                continue;

            if (TryPlayState(animator, clip.name))
                return true;
        }

        return false;
    }

    private AnimationClip GetPartIntroClip(AnimationClip configuredClip, Animator animator, string stateName)
    {
        if (configuredClip != null)
            return configuredClip;

        if (animator == null || animator.runtimeAnimatorController == null)
            return null;

        AnimationClip[] clips = animator.runtimeAnimatorController.animationClips;
        if (clips == null || clips.Length == 0)
            return null;

        if (!string.IsNullOrEmpty(stateName))
        {
            for (int i = 0; i < clips.Length; i++)
            {
                AnimationClip clip = clips[i];
                if (clip != null && clip.name.Equals(stateName, System.StringComparison.OrdinalIgnoreCase))
                    return clip;
            }
        }

        for (int i = 0; i < clips.Length; i++)
        {
            AnimationClip clip = clips[i];
            if (clip != null && clip.name.IndexOf("intro", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return clip;
        }

        return clips[0];
    }

    private bool PlayClipWithPlayable(Animator animator, AnimationClip clip)
    {
        if (animator == null || clip == null)
            return false;

        PlayableGraph graph = PlayableGraph.Create($"ThreeMonkeyIntro_{animator.gameObject.name}");
        graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);

        AnimationPlayableOutput output = AnimationPlayableOutput.Create(graph, "IntroOutput", animator);
        AnimationClipPlayable clipPlayable = AnimationClipPlayable.Create(graph, clip);
        clipPlayable.SetApplyFootIK(false);
        clipPlayable.SetApplyPlayableIK(false);
        clipPlayable.SetTime(0d);
        clipPlayable.SetSpeed(1d);
        output.SetSourcePlayable(clipPlayable);

        graph.Play();
        introPlayableGraphs.Add(graph);
        return true;
    }

    private IEnumerator StopIntroPlayableGraphsAfterDelay(float delay)
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        StopIntroPlayableGraphs();
    }

    private void StopIntroPlayableGraphs()
    {
        for (int i = 0; i < introPlayableGraphs.Count; i++)
        {
            PlayableGraph graph = introPlayableGraphs[i];
            if (graph.IsValid())
                graph.Destroy();
        }

        introPlayableGraphs.Clear();
    }

    private void FreezeAnimator(Animator animator)
    {
        if (animator == null)
            return;

        if (!frozenAnimators.Contains(animator))
            frozenAnimators.Add(animator);

        animator.speed = 0f;
        animator.Update(0f);
    }

    private void RestoreFrozenAnimators()
    {
        for (int i = 0; i < frozenAnimators.Count; i++)
        {
            Animator animator = frozenAnimators[i];
            if (animator == null)
                continue;

            animator.speed = 1f;
            animator.Update(0f);
        }

        frozenAnimators.Clear();
    }
    
    //TODO   ���̾� �ٲٱ� �� �ؾߵ�;
    public override void First()
    {
        StopIntroPlayableGraphs();
        
        // BallisticMovementComponent를 통해 방향 설정
        if (ballisticMovement != null)
        {
            ballisticMovement.CurrentDirection = DetermineStartDirection();
            if (verboseBTLog)
                Debug.Log($"[ThreeMonkeyBoss][BT] start ballistic direction => {ballisticMovement.CurrentDirection}");
        }
    }

    private Vector2 DetermineStartDirection()
    {
        // 기본 방향 로직
        Vector2[] options = { Vector2.right, Vector2.left, Vector2.up, Vector2.down };
        return options[UnityEngine.Random.Range(0, options.Length)];
    }

    public override void OnCollisionEnter2D(Collision2D collision)
    {
        base.OnCollisionEnter2D(collision);
        PlayerStatControl playerStat = ResolvePlayerStat(collision);
        if (playerStat != null)
            ApplyMonkeyEffect(playerStat, currentBottomEffect);

        TryReflectByCollision(collision, true);
    }

    private PlayerStatControl ResolvePlayerStat(Collision2D collision)
    {
        if (collision == null)
            return null;

        if (collision.gameObject != null && collision.gameObject.TryGetComponent(out PlayerStatControl direct))
            return direct;

        if (collision.collider != null)
        {
            PlayerStatControl fromCollider = collision.collider.GetComponentInParent<PlayerStatControl>();
            if (fromCollider != null)
                return fromCollider;
        }

        if (collision.rigidbody != null)
        {
            PlayerStatControl fromRigidbody = collision.rigidbody.GetComponentInParent<PlayerStatControl>();
            if (fromRigidbody != null)
                return fromRigidbody;
        }

        return null;
    }

    public override void OnCollisionStay2D(Collision2D collision)
    {
        base.OnCollisionStay2D(collision);
        // Stay 구간에서 stop-window를 매 프레임 갱신하면 접촉 상태가 과도하게 정지될 수 있다.
        TryReflectByCollision(collision, false);
    }

    private void TryReflectByCollision(Collision2D collision, bool applyStopWindow)
    {
        if (collision == null || collision.collider == null)
            return;

        int layer = collision.collider.gameObject.layer;
        
        ContactPoint2D[] contacts = new ContactPoint2D[collision.contactCount];
        collision.GetContacts(contacts);

        if (contacts.Length == 0)
            return;

        Vector2 avgNormal = Vector2.zero;
        foreach (var contact in contacts)
            avgNormal += contact.normal;

        if (avgNormal.sqrMagnitude < 0.0001f)
            avgNormal = Vector2.right;

        avgNormal.Normalize();

        // BallisticMovementComponent에서 방향 가져오기
        if (ballisticMovement == null)
            return;

        Vector2 currentDir = ballisticMovement.CurrentDirection;
        float reflectedDot = Vector2.Dot(currentDir, avgNormal);

        // 이미 벗어나는 중이면 반사하지 않음
        if (reflectedDot > 0f)
            return;

        // 반사 적용
        Vector2 newDir = Vector2.Reflect(currentDir, avgNormal).normalized;
        ballisticMovement.CurrentDirection = newDir;
        
        // Blackboard에도 업데이트
        if (blackboard != null)
            blackboard.Set("MoveDirection", newDir);

        // stop-window 적용 (Enter시에만)
        if (applyStopWindow)
        {
            int bossLayer = LayerMask.NameToLayer("Boss");
            int creatureLayer = LayerMask.NameToLayer("Creatuer");
            int creature2Layer = LayerMask.NameToLayer("Creature");
            
            bool isSoftBody = (layer == bossLayer || layer == creatureLayer || layer == creature2Layer);
            
            if (!isSoftBody && ballisticMovement != null)
            {
                ballisticMovement.SetStopWindow(0.04f);
            }
        }
    }

    protected override void OnDamagedByBullet(Bullet bullet, float finalDamage)
    {
        if (bullet != null)
        {
            Vector2 bdir = bullet._direction;
            if (bdir.sqrMagnitude > 0.0001f)
                lastHitBulletDirection = bdir.normalized;
        }

        if (!eyeDetached && curHp <= maxHp * (2f / 3f))
        {
            eyeDetached = true;
            tower1fire();
            currentBottomEffect = MonkeyEffectType.Ear;
            StatReSetting(secondMouseMonkeySO);
            return;
        }

        if (!earDetached && curHp <= maxHp * (1f / 3f))
        {
            earDetached = true;
            tower2fire();
            currentBottomEffect = MonkeyEffectType.Mouth;
            StatReSetting(LastEarMonkeySO);
            return;
        }
    }

    public override void Damege(float damege)
    {
        base.Damege(damege);
    }

    public override void BossDie()
    {
        base.BossDie();
        gameObject.SetActive(false);
    }

    public void KillBro()
    {
        if (_tower1Eye != null)
        {
            _tower1Eye.SetActive(false);
            var m1 = _tower1Eye.GetComponent<MonkeyPart>();
            if (m1 != null) m1.BossDie();
        }

        if (_tower2Mouse != null)
        {
            _tower2Mouse.SetActive(false);
            var m2 = _tower2Mouse.GetComponent<MonkeyPart>();
            if (m2 != null) m2.BossDie();
        }
    }

    public void StatReSetting(EnemySO enemyso) 
    {
        if (enemyso == null) return;
        atk = enemyso.atk;
        maxHp = enemyso.hp;
        curHp = enemyso.hp;
        speed = enemyso.speed;
        ResolveBTParams(enemyso);
    }


    public void tower1fire() 
    {
        GameObject eyePrefab = eyeDetachedPrefab != null ? eyeDetachedPrefab : prefab1TowerEye;
        if (eyePrefab == null) return;

        SetMainColliderEnabled(false);

        _tower1Eye =Instantiate(eyePrefab);
        _tower1Eye.transform.position = ResolveDetachSpawnPosition(tower1EyeOBJ);
        ResolveDetachSpawnOverlap(_tower1Eye);
        _tower1Eye.SetActive(true);
        var part = _tower1Eye.GetComponent<MonkeyPart>();
        if (part != null)
        {
            part.effectType = MonkeyEffectType.Eye;
            part.bossCount = 1;
            part.StageOwner = StageOwner;
            part.Init(GetDetachDirection());

            // 분리 개체가 정상 생성된 경우에만 카운트 증가
            AddSplitBossCount(1);
        }
        else
        {
            Debug.LogWarning($"[ThreeMonkeyBoss] Eye detached prefab missing MonkeyPart: {_tower1Eye.name}");
        }


        Invoke("OnCol", 1f);

        tower1EyeOBJ.SetActive(false);
        // 아래층 분리 후 남은 층 정렬: 2층 -> 1층, 3층 -> 2층
        StartCoroutine(Run(1f, tower2MouseOBJ, eyeSlotLocalPos));
        StartCoroutine(Run(1f, tower3EarOBJ, mouthSlotLocalPos));

        // 분리 직후 짧은 무적 + BT 정지
        StartInvincibilityNSecond(1f);
        WaitPls(1f);
    }

    public void tower2fire()
    {
        GameObject earPrefab = earDetachedPrefab != null ? earDetachedPrefab
            : (prefab2TowerMouse != null ? prefab2TowerMouse : prefab1TowerEye);
        if (earPrefab == null) return;

        SetMainColliderEnabled(false);

        _tower2Mouse = Instantiate(earPrefab);
        _tower2Mouse.transform.position = ResolveDetachSpawnPosition(tower2MouseOBJ);
        ResolveDetachSpawnOverlap(_tower2Mouse);
        _tower2Mouse.SetActive(true);
        var part = _tower2Mouse.GetComponent<MonkeyPart>();
        if (part != null)
        {
            part.effectType = MonkeyEffectType.Mouth;
            part.bossCount = 1;
            part.StageOwner = StageOwner;
            part.Init(GetDetachDirection());

            // 분리 개체가 정상 생성된 경우에만 카운트 증가
            AddSplitBossCount(1);
        }
        else
        {
            Debug.LogWarning($"[ThreeMonkeyBoss] Ear detached prefab missing MonkeyPart: {_tower2Mouse.name}");
        }


        Invoke("OnCol", 1f);

        tower2MouseOBJ.SetActive(false);

        // 아래층 분리 후 남은 층 정렬: 3층 -> 1층
        StartCoroutine(Run(1f, tower3EarOBJ, eyeSlotLocalPos));

        StartInvincibilityNSecond(1f);
        WaitPls(1f);
    }
    public void OnCol() 
    {
        SetMainColliderEnabled(true);
    }

    private void SetMainColliderEnabled(bool enabled)
    {
        if (_mainCollider2D == null)
            _mainCollider2D = GetComponent<Collider2D>();

        if (_mainCollider2D != null)
            _mainCollider2D.enabled = enabled;
    }

    private Vector2 GetDetachDirection()
    {
        if (lastHitBulletDirection.sqrMagnitude > 0.0001f)
            return lastHitBulletDirection.normalized;

        if (ballisticMovement != null && ballisticMovement.CurrentDirection.sqrMagnitude > 0.0001f)
            return ballisticMovement.CurrentDirection.normalized;

        return Vector2.right;
    }

    private Vector3 ResolveDetachSpawnPosition(GameObject sourcePart)
    {
        Vector3 basePos = sourcePart != null ? sourcePart.transform.position : transform.position;
        Vector2 dir = GetDetachDirection();
        if (dir.sqrMagnitude < 0.0001f)
            dir = Vector2.right;

        Vector3 spawnPos = basePos + (Vector3)(dir.normalized * detachSpawnOutwardOffset);
        spawnPos.z = transform.position.z;
        return spawnPos;
    }

    private void ResolveDetachSpawnOverlap(GameObject spawned)
    {
        if (spawned == null)
            return;

        Collider2D col = spawned.GetComponent<Collider2D>();
        if (col == null)
            return;

        int wallLayer = LayerMask.NameToLayer("Wall");
        if (wallLayer < 0)
            return;

        LayerMask wallMask = 1 << wallLayer;
        Vector2 originalPos = spawned.transform.position;

        // 먼저 현재 위치가 안전한지 체크
        if (!IsOverlappingWall(col, wallMask))
        {
            Debug.Log($"[ThreeMonkeyBoss] {spawned.name} spawned safely at {originalPos}");
            return;
        }

        Debug.LogWarning($"[ThreeMonkeyBoss] {spawned.name} overlapping wall! Searching for safe position...");

        // 스테이지 중심 계산 (nxnZone 우선)
        Vector2 stageCenter = transform.position;
        if (StageOwner != null)
        {
            if (StageOwner.nxnZone != null)
                stageCenter = StageOwner.nxnZone.bounds.center;
            else if (StageOwner.transform != null)
                stageCenter = StageOwner.transform.position;
        }

        // 방법 1: 스테이지 중심 방향으로 직선 이동 시도
        Vector2 toCenter = (stageCenter - originalPos).normalized;
        for (float dist = 0.5f; dist <= detachSpawnMaxSearchDistance; dist += 0.3f)
        {
            Vector2 testPos = originalPos + toCenter * dist;
            spawned.transform.position = testPos;
            
            if (!IsOverlappingWall(col, wallMask))
            {
                Debug.Log($"[ThreeMonkeyBoss] Found safe position at distance {dist:F2} towards center: {testPos}");
                return;
            }
        }

        // 방법 2: 여러 방향으로 탐색 (원형 패턴)
        int attempts = Mathf.Max(4, detachSpawnSafeSearchAttempts);
        float angleStep = 360f / attempts;
        
        for (int i = 0; i < attempts; i++)
        {
            float angle = angleStep * i;
            Vector2 direction = Quaternion.Euler(0, 0, angle) * Vector2.right;
            
            for (float dist = 0.5f; dist <= detachSpawnMaxSearchDistance; dist += 0.3f)
            {
                Vector2 testPos = originalPos + direction * dist;
                spawned.transform.position = testPos;
                
                if (!IsOverlappingWall(col, wallMask))
                {
                    Debug.Log($"[ThreeMonkeyBoss] Found safe position at angle {angle:F0}°, distance {dist:F2}: {testPos}");
                    return;
                }
            }
        }

        // 방법 3: 최후의 수단 - 스테이지 중심으로 강제 이동
        spawned.transform.position = stageCenter;
        if (!IsOverlappingWall(col, wallMask))
        {
            Debug.LogWarning($"[ThreeMonkeyBoss] Forced spawn at stage center: {stageCenter}");
            return;
        }

        // 그래도 안되면... 원래 위치로 복귀하고 경고
        spawned.transform.position = originalPos;
        Debug.LogError($"[ThreeMonkeyBoss] FAILED to find safe spawn position for {spawned.name}! Still overlapping wall.");
    }

    /// <summary>
    /// 콜라이더가 벽과 겹치는지 체크
    /// </summary>
    private bool IsOverlappingWall(Collider2D col, LayerMask wallMask)
    {
        if (col == null)
            return false;

        ContactFilter2D filter = default;
        filter.useLayerMask = true;
        filter.layerMask = wallMask;
        filter.useTriggers = false;

        int count = col.Overlap(filter, detachSpawnOverlapBuffer);
        return count > 0;
    }

    private void ApplyMonkeyEffect(PlayerStatControl playerStat, MonkeyEffectType effect)
    {
        if (playerStat == null) return;

        var receiver = playerStat.GetComponent<PlayerBossStatusEffectReceiver>();
        if (receiver == null)
            receiver = playerStat.gameObject.AddComponent<PlayerBossStatusEffectReceiver>();

        receiver.ApplyEffect(effect, collisionEffectDuration);
    }

    private void AddSplitBossCount(int value)
    {
        if (value <= 0) return;

        if (StageOwner != null)
            StageOwner.RegisterBossSpawnCount(value);
        else if (GameManager.Instance != null)
            GameManager.Instance.BossCountAdd(value);
    }

    // mouthDetachedPrefab 슬롯은 현재 3단 분리 확장(입 분리 추가) 시 사용 예정.

    IEnumerator Run(float duration , GameObject target , Vector3 endposition)
    {
        if (target == null) yield break;

        var runTime = 0.0f;
        Transform moveTarget = target.transform;
        Vector3 startPos = moveTarget.localPosition;
        Vector3 endPos = new Vector3(endposition.x, endposition.y, startPos.z);
        while (runTime < duration)
        {
            runTime += Time.deltaTime;
            float t = duration > 0f ? Mathf.Clamp01(runTime / duration) : 1f;
            moveTarget.localPosition = Vector3.Lerp(startPos, endPos, t);

            yield return null;
        }

        moveTarget.localPosition = endPos;
    }

    private IEnumerator PlayStackIntroAnimation(float duration)
    {
        if (duration <= 0f) yield break;

        Transform bottom = tower1EyeOBJ != null ? tower1EyeOBJ.transform : null;
        Transform middle = tower2MouseOBJ != null ? tower2MouseOBJ.transform : null;
        Transform top = tower3EarOBJ != null ? tower3EarOBJ.transform : null;

        Quaternion b0 = bottom != null ? bottom.localRotation : Quaternion.identity;
        Quaternion m0 = middle != null ? middle.localRotation : Quaternion.identity;
        Quaternion t0 = top != null ? top.localRotation : Quaternion.identity;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            // 하단/상단: 좌 -> 우, 중단: 우 -> 좌
            float bottomZ = Mathf.Lerp(-introRotateAngle, introRotateAngle, t);
            float middleZ = Mathf.Lerp(introRotateAngle, -introRotateAngle, t);
            float topZ = Mathf.Lerp(-introRotateAngle, introRotateAngle, t);

            if (bottom != null) bottom.localRotation = Quaternion.Euler(0f, 0f, bottomZ);
            if (middle != null) middle.localRotation = Quaternion.Euler(0f, 0f, middleZ);
            if (top != null) top.localRotation = Quaternion.Euler(0f, 0f, topZ);

            yield return null;
        }

        if (bottom != null) bottom.localRotation = b0;
        if (middle != null) middle.localRotation = m0;
        if (top != null) top.localRotation = t0;
    }

    protected override void OnDisable()
    {
        RestoreFrozenAnimators();
        StopIntroPlayableGraphs();
        base.OnDisable();
    }

}
