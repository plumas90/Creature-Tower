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

    private BoxCollider2D _boxCollider2D;
    private Collider2D _moveCollider2D;
    private Rigidbody2D _moveBody2D;
    private readonly List<RaycastHit2D> _sweepHits = new List<RaycastHit2D>(8);
    private ContactFilter2D _moveContactFilter;
    private bool _moveContactFilterReady;
    private int _reflectLayerMask;
    private bool _reflectLayerMaskCached;


    private Vector3 zero = new Vector3(0,0);
    private Vector3 midlle = new Vector3(0,1.5f);

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

    Vector2 direction = Vector2.zero;

    private float btMoveSpeedMultiplier = 1f;

    [Header("Ballistic Movement")]
    [SerializeField] private Vector2 startDirection = new Vector2(1f, -1f); // 5 o'clock
    [SerializeField] [Min(0f)] private float sweepSkin = 0.02f;
    [SerializeField] [Range(0, 4)] private int maxRaycastBouncesPerTick = 2;
    [SerializeField] [Min(0f)] private float collisionStopDuration = 0.04f;

    [Header("Move Debug")]
    [SerializeField] private bool enableMoveDebug = true;
    [SerializeField] [Min(0.02f)] private float moveDebugInterval = 0.1f;

    private float collisionStopUntilTime;
    private float nextMoveDebugTime;

    public override void StatSet() 
    {
        StopIntroPlayableGraphs();
        base.StatSet();
        _boxCollider2D = this.GetComponent<BoxCollider2D>();

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

        Debug.Log($"[ThreeMonkeyBoss][MoveDebug] enabled={enableMoveDebug} interval={moveDebugInterval:F2}");

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

        // 레이어 규칙상 몬스터는 플레이어(10)보다 위쪽 정렬이 필요할 수 있어 명시적으로 보정
        ApplyRendererState(tower1EyeOBJ, 13);
        ApplyRendererState(tower2MouseOBJ, 14);
        ApplyRendererState(tower3EarOBJ, 15);
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
                new BossActionNode(() =>
                {
                    MoveBallistic();
                    return TraceState("MoveBallistic", BossBTState.Running);
                })
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

    private void MoveBallistic()
    {
        if (Time.time < collisionStopUntilTime)
        {
            LogMoveDebug($"stop-window active until={collisionStopUntilTime:F3}");
            return;
        }

        if (direction.sqrMagnitude < 0.0001f)
            direction = GetStartDirection();

        float remainingDistance = speed * btMoveSpeedMultiplier * Time.fixedDeltaTime;
        if (remainingDistance <= 0f)
            return;

        EnsureMoveCollider();
        if (_moveBody2D == null)
        {
            LogMoveDebug("Rigidbody2D missing; MoveBallistic skipped", true);
            return;
        }

        Vector2 moveDir = direction.normalized;
        Vector2 startPos = _moveBody2D.position;
        Vector2 simPos = startPos;
        bool collided = false;
        string hitName = "none";
        Vector2 hitNormal = Vector2.zero;
        int bounceCount = 0;
        int maxBounces = Mathf.Max(0, maxRaycastBouncesPerTick);

        while (remainingDistance > 0f)
        {
            float castDistance = remainingDistance + sweepSkin;
            _sweepHits.Clear();
            int hitCount = _moveBody2D.Cast(simPos, _moveBody2D.rotation, moveDir, _moveContactFilter, _sweepHits, castDistance);
            bool hasHit = TryGetNearestCastHit(hitCount, out RaycastHit2D nearestHit);

            if (!hasHit)
            {
                simPos += moveDir * remainingDistance;
                remainingDistance = 0f;
                break;
            }

            float safeDistance = Mathf.Max(0f, nearestHit.distance - sweepSkin);
            if (safeDistance > 0f)
                simPos += moveDir * safeDistance;

            collided = true;
            hitName = nearestHit.collider != null ? nearestHit.collider.name : "null";
            hitNormal = nearestHit.normal;

            remainingDistance -= safeDistance;
            if (remainingDistance <= 0f || bounceCount >= maxBounces)
                break;

            moveDir = Vector2.Reflect(moveDir, nearestHit.normal).normalized;
            collisionStopUntilTime = Time.time + collisionStopDuration;
            // 충돌 직후에는 잔여 이동을 즉시 중단해 연속 튕김/터널링을 줄인다.
            remainingDistance = 0f;
            bounceCount++;
        }

        direction = moveDir;
        _moveBody2D.MovePosition(simPos);

        LogMoveDebug($"from={startPos} to={simPos} dir={direction} collided={collided} hit={hitName} normal={hitNormal} bounces={bounceCount}");
    }

    private void LogMoveDebug(string message, bool force = false)
    {
        if (!enableMoveDebug)
            return;

        if (!force && Time.time < nextMoveDebugTime)
            return;

        nextMoveDebugTime = Time.time + moveDebugInterval;
        Debug.Log($"[ThreeMonkeyBoss][MoveDebug] {message}");
    }

    private void EnsureMoveCollider()
    {
        if (_moveCollider2D != null)
            return;

        _moveCollider2D = GetComponent<Collider2D>();
        _moveBody2D = Body2D != null ? Body2D : GetComponent<Rigidbody2D>();

        if (_moveBody2D != null)
        {
            _moveContactFilter.useLayerMask = true;
            _moveContactFilter.layerMask = GetReflectLayerMask();
            _moveContactFilter.useTriggers = false;
            _moveContactFilter.useDepth = false;
            _moveContactFilterReady = true;
        }
    }

    private bool TryGetNearestCastHit(int hitCount, out RaycastHit2D nearestHit)
    {
        nearestHit = default;
        if (!_moveContactFilterReady || hitCount <= 0)
            return false;

        float nearestDistance = float.MaxValue;
        bool hasHit = false;

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit2D hit = _sweepHits[i];
            if (!IsValidCastHit(hit))
                continue;

            if (hit.distance < nearestDistance)
            {
                nearestDistance = hit.distance;
                nearestHit = hit;
                hasHit = true;
            }
        }

        return hasHit;
    }

    private bool IsValidCastHit(RaycastHit2D hit)
    {
        Collider2D col = hit.collider;
        if (col == null)
            return false;

        if (col.isTrigger)
            return false;

        if (col.transform.root == transform.root)
            return false;

        if (!IsReflectTargetLayer(col.gameObject.layer))
            return false;

        return hit.distance >= 0f;
    }

    private int GetReflectLayerMask()
    {
        if (_reflectLayerMaskCached)
            return _reflectLayerMask;

        _reflectLayerMaskCached = true;
        _reflectLayerMask = 0;
        AddLayerToMask("Wall");
        AddLayerToMask("Player");
        AddLayerToMask("Creatuer");
        AddLayerToMask("Creature");
        AddLayerToMask("Enemy");
        AddLayerToMask("Boss");
        return _reflectLayerMask;
    }

    private void AddLayerToMask(string layerName)
    {
        int layer = LayerMask.NameToLayer(layerName);
        if (layer < 0)
            return;

        _reflectLayerMask |= 1 << layer;
    }

    private Vector2 GetStartDirection()
    {
        if (startDirection.sqrMagnitude < 0.0001f)
            return new Vector2(1f, -1f).normalized;

        return startDirection.normalized;
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
        direction = GetStartDirection();

        if (verboseBTLog)
            Debug.Log($"[ThreeMonkeyBoss][BT] start ballistic direction => {direction}");
    }

    public override void OnCollisionEnter2D(Collision2D collision)
    {
        base.OnCollisionEnter2D(collision);
        if (collision.gameObject.TryGetComponent(out PlayerStatControl playerStat))
            ApplyMonkeyEffect(playerStat, currentBottomEffect);

        TryReflectByCollision(collision);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        TryReflectByCollision(collision);
    }

    private void TryReflectByCollision(Collision2D collision)
    {
        if (collision == null)
            return;

        if (collision.contactCount <= 0)
            return;

        if (!IsReflectTargetLayer(collision.gameObject.layer))
            return;

        Vector2 dir = direction.sqrMagnitude > 0.0001f ? direction.normalized : GetStartDirection();
        Vector2 normal = collision.contacts[0].normal;

        // 이미 표면에서 멀어지는 방향이면 반사하지 않는다.
        if (Vector2.Dot(dir, normal) >= 0f)
            return;

        direction = Vector2.Reflect(dir, normal).normalized;
        collisionStopUntilTime = Time.time + collisionStopDuration;
        LogMoveDebug($"fallback reflect by collision layer={collision.gameObject.layer} normal={normal} dir={direction}", true);
    }

    private bool IsReflectTargetLayer(int layer)
    {
        int wall = LayerMask.NameToLayer("Wall");
        int player = LayerMask.NameToLayer("Player");
        int creatureTypo = LayerMask.NameToLayer("Creatuer");
        int creature = LayerMask.NameToLayer("Creature");
        int enemy = LayerMask.NameToLayer("Enemy");
        int boss = LayerMask.NameToLayer("Boss");

        return layer == wall
            || layer == player
            || layer == enemy
            || layer == boss
            || (creatureTypo >= 0 && layer == creatureTypo)
            || (creature >= 0 && layer == creature);
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
        if (!companionCleared)
        {
            companionCleared = true;
            KillBro();
        }

        base.BossDie();
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
        _boxCollider2D.enabled = false;

        GameObject eyePrefab = eyeDetachedPrefab != null ? eyeDetachedPrefab : prefab1TowerEye;
        if (eyePrefab == null) return;

        _tower1Eye =Instantiate(eyePrefab);
        _tower1Eye.transform.position = this.transform.position;
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
        // 아래층 분리 후 남은 층 정렬
        StartCoroutine(Run(1, tower2MouseOBJ, zero));
        StartCoroutine(Run(1, tower3EarOBJ, midlle));

        // 분리 직후 짧은 무적 + BT 정지
        StartInvincibilityNSecond(1f);
        WaitPls(1f);
    }

    public void tower2fire()
    {
        _boxCollider2D.enabled = false;

        GameObject earPrefab = earDetachedPrefab != null ? earDetachedPrefab
            : (prefab2TowerMouse != null ? prefab2TowerMouse : prefab1TowerEye);
        if (earPrefab == null) return;

        _tower2Mouse = Instantiate(earPrefab);
        _tower2Mouse.transform.position = this.transform.position;
        _tower2Mouse.SetActive(true);
        var part = _tower2Mouse.GetComponent<MonkeyPart>();
        if (part != null)
        {
            part.effectType = MonkeyEffectType.Ear;
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

        // 아래층 분리 후 남은 층 정렬
        StartCoroutine(Run(1,tower3EarOBJ,zero));

        StartInvincibilityNSecond(1f);
        WaitPls(1f);
    }
    public void OnCol() 
    {
        _boxCollider2D.enabled = true;
    }

    private Vector2 GetDetachDirection()
    {
        if (lastHitBulletDirection.sqrMagnitude > 0.0001f)
            return lastHitBulletDirection.normalized;

        if (direction.sqrMagnitude > 0.0001f)
            return direction.normalized;

        return Vector2.right;
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
