using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CaptainCrabBoss : BossBase
{
    private enum CaptainCrabPattern
    {
        Guard = 0,
        ClawSweep = 1,
        BubbleBeam = 2,
    }

    [Header("SO Reference")]
    [SerializeField] private CaptainCrabBossSO captainCrabSO;

    [Header("Weak Point")]
    [SerializeField] private Collider2D faceHurtboxCollider;
    [SerializeField] private BossHurtbox faceHurtbox;

    [Header("Pattern Anchors")]
    [SerializeField] private Transform faceShootPoint;
    [SerializeField] private Transform bombSpawnPoint;

    [Header("Claw Motion (Assign Arm Parents)")]
    [SerializeField] private Transform leftClawTransform;
    [SerializeField] private Transform rightClawTransform;
    [SerializeField] private Transform leftClawSwingPivot;
    [SerializeField] private Transform rightClawSwingPivot;
    [SerializeField] private int leftClawSweepDirectionSign = -1;
    [SerializeField] private int rightClawSweepDirectionSign = 1;

    [Header("Claw Sweep Hitboxes")]
    [SerializeField] private CaptainCrabClawSweepHitbox leftClawSweepHitbox;
    [SerializeField] private CaptainCrabClawSweepHitbox rightClawSweepHitbox;

    [Header("Projectile Blockers")]
    [SerializeField] private CaptainCrabProjectileBlocker[] projectileBlockers;

    [Header("Beam Warning")]
    [SerializeField] private SpriteRenderer[] beamWarningRenderers;

    private bool patternRunning;
    private float nextPatternAllowedAt;
    private float nextBombSpawnAt;
    private Coroutine patternRoutine;
    private Coroutine bombRoutine;
    private readonly List<CaptainCrabBubbleBomb> activeBombs = new List<CaptainCrabBubbleBomb>();
    private CaptainCrabPattern lastPattern = CaptainCrabPattern.Guard;
    private int consecutivePatternCount;
    private Vector3 leftClawDefaultLocalPos;
    private Vector3 rightClawDefaultLocalPos;
    private Quaternion leftClawDefaultLocalRot;
    private Quaternion rightClawDefaultLocalRot;
    private float leftClawCurrentSwingAngle;
    private float rightClawCurrentSwingAngle;
    private bool clawPoseCached;
    private Color[] beamWarningOriginalColors;

    protected override void Awake()
    {
        base.Awake();

        if (captainCrabSO != null)
            MainSO = captainCrabSO;

        if (faceHurtbox == null)
            faceHurtbox = GetComponentInChildren<BossHurtbox>(true);
        if (faceHurtbox != null && faceHurtboxCollider == null)
            faceHurtboxCollider = faceHurtbox.GetComponent<Collider2D>();

        ResolveClawReferences();
        CacheClawDefaultPose();
        ResetClawPoseImmediate();
        CacheBeamWarningRenderers();
        SetBeamWarningActive(false);
        SetSweepHitboxesActive(false);

        ConfigureProjectileBlockers();
    }

    public override void StatSet()
    {
        if (captainCrabSO == null && MainSO is CaptainCrabBossSO soFromMain)
            captainCrabSO = soFromMain;

        if (captainCrabSO != null)
            MainSO = captainCrabSO;

        if (captainCrabSO == null)
        {
            Debug.LogError("[CaptainCrabBoss] CaptainCrabBossSO is not assigned.");
            return;
        }

        base.StatSet();
        bossCount = Mathf.Max(1, MainSO != null ? MainSO.bossCount : 1);

        patternRunning = false;
        nextPatternAllowedAt = Time.time;
        nextBombSpawnAt = Time.time + GetCurrentBombSpawnInterval();
        lastPattern = CaptainCrabPattern.Guard;
        consecutivePatternCount = 0;
        CacheClawDefaultPose();
        ResetClawPoseImmediate();
        SetBeamWarningActive(false);
        SetFaceExposed(true);
        SetSweepHitboxesActive(false);
        CleanupBombList();
    }

    private void FixedUpdate()
    {
        TickBehaviorTree();
    }

    protected override BossBTNode CreateBehaviorTree()
    {
        if (captainCrabSO == null)
            return new BossActionNode(() => BossBTState.Running);

        return new BossSelectorNode(
            new BossSequenceNode(
                new BossConditionNode(() => live && !wait),
                new BTTask_CaptainCrabPatternCycle(this, captainCrabSO)
            ),
            new BossActionNode(() => BossBTState.Running)
        );
    }

    protected override float CalculateFinalDamage(float incomingDamage)
    {
        if (captainCrabSO == null)
            return base.CalculateFinalDamage(incomingDamage);

        return Mathf.Max(0f, incomingDamage * Mathf.Max(0f, captainCrabSO.faceDamageMultiplier));
    }

    public void TickPatternCycle()
    {
        if (!live || wait || captainCrabSO == null)
            return;

        if (bombRoutine == null)
            bombRoutine = StartCoroutine(CoAmbientBombLoop());

        if (patternRunning)
            return;

        if (Time.time < nextPatternAllowedAt)
            return;

        StartPattern(SelectNextPattern());
    }

    private CaptainCrabPattern SelectNextPattern()
    {
        float guardW = Mathf.Max(0f, captainCrabSO.guardWeight);
        float sweepW = Mathf.Max(0f, captainCrabSO.clawSweepWeight);
        float beamW = Mathf.Max(0f, captainCrabSO.bubbleBeamWeight);
        float total = guardW + sweepW + beamW;
        if (total <= 0f)
            return CaptainCrabPattern.Guard;

        CaptainCrabPattern selected = CaptainCrabPattern.Guard;
        for (int attempt = 0; attempt < 6; attempt++)
        {
            float roll = Random.value * total;
            if (roll < guardW)
                selected = CaptainCrabPattern.Guard;
            else if (roll < guardW + sweepW)
                selected = CaptainCrabPattern.ClawSweep;
            else
                selected = CaptainCrabPattern.BubbleBeam;

            if (captainCrabSO.allowSamePatternRepeat)
                return selected;

            int maxRepeat = Mathf.Max(1, captainCrabSO.maxConsecutiveSamePattern);
            if (selected != lastPattern || consecutivePatternCount < maxRepeat)
                return selected;
        }

        return CaptainCrabPattern.Guard;
    }

    private void StartPattern(CaptainCrabPattern pattern)
    {
        if (patternRoutine != null)
            StopCoroutine(patternRoutine);

        patternRunning = true;
        if (pattern == lastPattern)
            consecutivePatternCount++;
        else
            consecutivePatternCount = 1;
        lastPattern = pattern;

        switch (pattern)
        {
            case CaptainCrabPattern.Guard:
                patternRoutine = StartCoroutine(CoGuardPattern());
                break;
            case CaptainCrabPattern.ClawSweep:
                patternRoutine = StartCoroutine(CoClawSweepPattern());
                break;
            default:
                patternRoutine = StartCoroutine(CoBubbleBeamPattern());
                break;
        }
    }

    private IEnumerator CoGuardPattern()
    {
        SetFaceExposed(false);
        yield return MoveClawsToGuardPose();
        yield return new WaitForSeconds(Mathf.Max(0.05f, captainCrabSO.guardDuration));
        SetFaceExposed(true);
        yield return MoveClawsToDefaultPose();
        yield return new WaitForSeconds(Mathf.Max(0.05f, captainCrabSO.faceExposeDuration));
        EndPattern();
    }

    private IEnumerator CoClawSweepPattern()
    {
        yield return MoveClawsToDefaultPose();
        yield return MoveClawsToAbsoluteY(-2f, 0.5f);
        yield return RotateClaws(
            captainCrabSO.clawSweepWindupAngle,
            Mathf.Max(0f, captainCrabSO.clawSweepTelegraphTime)
        );

        SetSweepHitboxesDamage(captainCrabSO.clawSweepDamage);
        SetSweepHitboxesActive(true);
        yield return RotateClaws(
            captainCrabSO.clawSweepStrikeAngle,
            Mathf.Max(0.05f, captainCrabSO.clawSweepActiveTime)
        );
        SetSweepHitboxesActive(false);
        yield return RotateClaws(0f, Mathf.Max(0.01f, captainCrabSO.clawSweepRecoverTime));
        yield return MoveClawsInwardForClash();
        yield return MoveClawsToDefaultPose();
        EndPattern();
    }

    private IEnumerator CoBubbleBeamPattern()
    {
        float telegraph = Mathf.Max(0f, captainCrabSO.beamTelegraphTime);
        float redWarningDuration = 0.5f;
        float preWarning = Mathf.Max(0f, telegraph - redWarningDuration);
        if (preWarning > 0f)
            yield return new WaitForSeconds(preWarning);

        SetBeamWarningActive(true);
        yield return new WaitForSeconds(redWarningDuration);
        SetBeamWarningActive(false);

        int shotCount = Mathf.Max(1, captainCrabSO.beamShotCount);
        for (int i = 0; i < shotCount; i++)
        {
            FireBubbleBeamShot();
            if (i < shotCount - 1)
                yield return new WaitForSeconds(Mathf.Max(0.01f, captainCrabSO.beamShotInterval));
        }

        EndPattern();
    }

    private void FireBubbleBeamShot()
    {
        if (captainCrabSO == null || captainCrabSO.bubbleBeamProjectilePrefab == null)
            return;

        Vector3 origin = faceShootPoint != null ? faceShootPoint.position : transform.position;
        Vector2 dir = Vector2.down;
        if (Player != null)
        {
            Vector2 toPlayer = (Vector2)(Player.transform.position - origin);
            if (toPlayer.sqrMagnitude > 0.0001f)
                dir = toPlayer.normalized;
        }

        float spread = captainCrabSO.beamSpreadAngle;
        float angle = Random.Range(-spread, spread);
        dir = (Quaternion.Euler(0f, 0f, angle) * dir).normalized;

        GameObject obj = Instantiate(captainCrabSO.bubbleBeamProjectilePrefab, origin, Quaternion.identity);
        CaptainCrabBubbleProjectile projectile = obj.GetComponent<CaptainCrabBubbleProjectile>();
        if (projectile == null)
            projectile = obj.AddComponent<CaptainCrabBubbleProjectile>();

        projectile.Initialize(
            dir,
            Mathf.Max(0f, captainCrabSO.beamProjectileSpeed),
            Mathf.Max(0.05f, captainCrabSO.beamProjectileLifetime),
            Mathf.Max(0f, captainCrabSO.beamProjectileDamage)
        );
    }

    private IEnumerator CoAmbientBombLoop()
    {
        while (live && !isDead)
        {
            CleanupBombList();
            float currentBombInterval = GetCurrentBombSpawnInterval();
            if (nextBombSpawnAt > Time.time + currentBombInterval)
                nextBombSpawnAt = Time.time + currentBombInterval;

            if (captainCrabSO != null
                && captainCrabSO.bubbleBombPrefab != null
                && Time.time >= nextBombSpawnAt
                && activeBombs.Count < Mathf.Max(1, captainCrabSO.maxActiveBombs))
            {
                SpawnBubbleBomb();
                nextBombSpawnAt = Time.time + currentBombInterval;
            }

            yield return null;
        }

        bombRoutine = null;
    }

    private void SpawnBubbleBomb()
    {
        if (captainCrabSO == null || captainCrabSO.bubbleBombPrefab == null)
            return;

        Vector3 spawnPos = bombSpawnPoint != null ? bombSpawnPoint.position : transform.position;
        GameObject obj = Instantiate(captainCrabSO.bubbleBombPrefab, spawnPos, Quaternion.identity);
        CaptainCrabBubbleBomb bomb = obj.GetComponent<CaptainCrabBubbleBomb>();
        if (bomb == null)
            bomb = obj.AddComponent<CaptainCrabBubbleBomb>();

        Bounds zone = StageOwner != null ? StageOwner.GetZoneBounds() : new Bounds(transform.position, new Vector3(12f, 8f, 0f));
        bomb.Initialize(captainCrabSO, zone);
        activeBombs.Add(bomb);
    }

    private void EndPattern()
    {
        patternRunning = false;
        patternRoutine = null;
        nextPatternAllowedAt = Time.time + Mathf.Max(0.05f, captainCrabSO != null ? captainCrabSO.patternInterval : 1f);
    }

    private float GetCurrentBombSpawnInterval()
    {
        if (captainCrabSO == null)
            return 1f;

        float baseInterval = Mathf.Max(0.1f, captainCrabSO.bombSpawnInterval);
        bool lowHpPhase = maxHp > 0f && curHp <= (maxHp / 3f);
        return lowHpPhase ? (baseInterval / 1.5f) : baseInterval;
    }

    private void SetFaceExposed(bool exposed)
    {
        if (faceHurtboxCollider != null)
            faceHurtboxCollider.enabled = exposed;
    }

    private void SetSweepHitboxesDamage(float damage)
    {
        if (leftClawSweepHitbox != null)
            leftClawSweepHitbox.SetDamage(damage);
        if (rightClawSweepHitbox != null)
            rightClawSweepHitbox.SetDamage(damage);
    }

    private void SetSweepHitboxesActive(bool active)
    {
        if (leftClawSweepHitbox != null)
            leftClawSweepHitbox.SetActiveState(active);
        if (rightClawSweepHitbox != null)
            rightClawSweepHitbox.SetActiveState(active);
    }

    private void ConfigureProjectileBlockers()
    {
        if (projectileBlockers == null || projectileBlockers.Length == 0)
            projectileBlockers = GetComponentsInChildren<CaptainCrabProjectileBlocker>(true);

        for (int i = 0; i < projectileBlockers.Length; i++)
        {
            CaptainCrabProjectileBlocker blocker = projectileBlockers[i];
            if (blocker != null)
                blocker.Bind(this);
        }
    }

    private void CleanupBombList()
    {
        activeBombs.RemoveAll(b => b == null);
    }

    private void StopCombatCoroutines()
    {
        if (patternRoutine != null)
        {
            StopCoroutine(patternRoutine);
            patternRoutine = null;
        }

        if (bombRoutine != null)
        {
            StopCoroutine(bombRoutine);
            bombRoutine = null;
        }

        SetSweepHitboxesActive(false);
        ResetClawPoseImmediate();
        SetBeamWarningActive(false);
        SetFaceExposed(true);
    }

    public override void BossDie()
    {
        if (isDead)
            return;

        StopCombatCoroutines();
        base.BossDie();
        gameObject.SetActive(false);
    }

    protected override void OnDisable()
    {
        StopCombatCoroutines();
        base.OnDisable();
    }

    private void ResolveClawReferences()
    {
        if (leftClawTransform == null && leftClawSweepHitbox != null)
            leftClawTransform = leftClawSweepHitbox.transform.parent != null
                ? leftClawSweepHitbox.transform.parent
                : leftClawSweepHitbox.transform;
        if (rightClawTransform == null && rightClawSweepHitbox != null)
            rightClawTransform = rightClawSweepHitbox.transform.parent != null
                ? rightClawSweepHitbox.transform.parent
                : rightClawSweepHitbox.transform;

        if (leftClawSwingPivot == null)
            leftClawSwingPivot = leftClawTransform;
        if (rightClawSwingPivot == null)
            rightClawSwingPivot = rightClawTransform;
    }

    private void CacheBeamWarningRenderers()
    {
        if (beamWarningRenderers == null || beamWarningRenderers.Length == 0)
        {
            if (faceHurtbox != null)
                beamWarningRenderers = faceHurtbox.GetComponentsInChildren<SpriteRenderer>(true);
            else
                beamWarningRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        }

        if (beamWarningRenderers == null)
        {
            beamWarningOriginalColors = null;
            return;
        }

        beamWarningOriginalColors = new Color[beamWarningRenderers.Length];
        for (int i = 0; i < beamWarningRenderers.Length; i++)
        {
            SpriteRenderer sr = beamWarningRenderers[i];
            beamWarningOriginalColors[i] = sr != null ? sr.color : Color.white;
        }
    }

    private void SetBeamWarningActive(bool active)
    {
        if (beamWarningRenderers == null || beamWarningRenderers.Length == 0)
            CacheBeamWarningRenderers();

        if (beamWarningRenderers == null)
            return;

        if (beamWarningOriginalColors == null || beamWarningOriginalColors.Length != beamWarningRenderers.Length)
            CacheBeamWarningRenderers();

        for (int i = 0; i < beamWarningRenderers.Length; i++)
        {
            SpriteRenderer sr = beamWarningRenderers[i];
            if (sr == null)
                continue;

            sr.color = active ? Color.red : beamWarningOriginalColors[i];
        }
    }

    private void CacheClawDefaultPose()
    {
        ResolveClawReferences();

        if (leftClawTransform == null || rightClawTransform == null)
            return;

        leftClawDefaultLocalPos = leftClawTransform.localPosition;
        rightClawDefaultLocalPos = rightClawTransform.localPosition;
        leftClawDefaultLocalRot = leftClawTransform.localRotation;
        rightClawDefaultLocalRot = rightClawTransform.localRotation;
        leftClawCurrentSwingAngle = 0f;
        rightClawCurrentSwingAngle = 0f;
        clawPoseCached = true;
    }

    private void ResetClawPoseImmediate()
    {
        if (!clawPoseCached)
            return;

        if (leftClawTransform != null)
        {
            leftClawTransform.localPosition = leftClawDefaultLocalPos;
            leftClawTransform.localRotation = leftClawDefaultLocalRot;
        }
        if (rightClawTransform != null)
        {
            rightClawTransform.localPosition = rightClawDefaultLocalPos;
            rightClawTransform.localRotation = rightClawDefaultLocalRot;
        }
        leftClawCurrentSwingAngle = 0f;
        rightClawCurrentSwingAngle = 0f;
    }

    private IEnumerator MoveClawsToGuardPose()
    {
        if (!clawPoseCached || captainCrabSO == null)
            yield break;

        Vector3 leftTarget = leftClawDefaultLocalPos;
        Vector3 rightTarget = rightClawDefaultLocalPos;

        // guardCoverCenterX는 "중앙으로 당기는 거리"로 해석한다.
        // 부호와 무관하게 항상 0축(얼굴 방향)으로 모이게 한다.
        float inwardDistance = Mathf.Abs(captainCrabSO.guardCoverCenterX);
        leftTarget.x = Mathf.MoveTowards(leftClawDefaultLocalPos.x, 0f, inwardDistance);
        rightTarget.x = Mathf.MoveTowards(rightClawDefaultLocalPos.x, 0f, inwardDistance);

        float lower = Mathf.Max(0f, captainCrabSO.guardLowerYOffset);
        leftTarget.y = leftClawDefaultLocalPos.y - lower;
        rightTarget.y = rightClawDefaultLocalPos.y - lower;

        yield return MoveClawPair(
            leftTarget,
            rightTarget,
            Mathf.Max(0.01f, captainCrabSO.guardMoveDuration)
        );
    }

    private IEnumerator MoveClawsToDefaultPose()
    {
        if (!clawPoseCached || captainCrabSO == null)
            yield break;

        yield return MoveClawPair(
            leftClawDefaultLocalPos,
            rightClawDefaultLocalPos,
            Mathf.Max(0.01f, captainCrabSO.guardMoveDuration)
        );
    }

    private IEnumerator MoveClawPair(Vector3 leftTarget, Vector3 rightTarget, float duration)
    {
        if (leftClawTransform == null || rightClawTransform == null)
            yield break;

        Vector3 leftStart = leftClawTransform.localPosition;
        Vector3 rightStart = rightClawTransform.localPosition;
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            leftClawTransform.localPosition = Vector3.Lerp(leftStart, leftTarget, k);
            rightClawTransform.localPosition = Vector3.Lerp(rightStart, rightTarget, k);
            yield return null;
        }

        leftClawTransform.localPosition = leftTarget;
        rightClawTransform.localPosition = rightTarget;
    }

    private IEnumerator MoveClawsToAbsoluteY(float targetLocalY, float duration)
    {
        if (leftClawTransform == null || rightClawTransform == null)
            yield break;

        Vector3 leftTarget = leftClawTransform.localPosition;
        Vector3 rightTarget = rightClawTransform.localPosition;
        leftTarget.y = targetLocalY;
        rightTarget.y = targetLocalY;

        yield return MoveClawPair(leftTarget, rightTarget, Mathf.Max(0.01f, duration));
    }

    private IEnumerator MoveClawsInwardForClash()
    {
        if (!clawPoseCached || captainCrabSO == null)
            yield break;

        Vector3 leftTarget = leftClawTransform.localPosition;
        Vector3 rightTarget = rightClawTransform.localPosition;
        float inwardDistance = Mathf.Abs(captainCrabSO.clawSweepClashInwardDistance);
        leftTarget.x = Mathf.MoveTowards(leftTarget.x, 0f, inwardDistance);
        rightTarget.x = Mathf.MoveTowards(rightTarget.x, 0f, inwardDistance);

        yield return MoveClawPair(
            leftTarget,
            rightTarget,
            Mathf.Max(0.01f, captainCrabSO.clawSweepClashDuration)
        );

        float holdTime = Mathf.Max(0f, captainCrabSO.clawSweepClashHoldTime);
        if (holdTime > 0f)
            yield return new WaitForSeconds(holdTime);
    }

    private IEnumerator RotateClaws(float targetDegrees, float duration)
    {
        if (!clawPoseCached || leftClawTransform == null || rightClawTransform == null)
            yield break;

        int leftSign = leftClawSweepDirectionSign >= 0 ? 1 : -1;
        int rightSign = rightClawSweepDirectionSign >= 0 ? 1 : -1;
        float leftTargetAngle = targetDegrees * leftSign;
        float rightTargetAngle = targetDegrees * rightSign;
        float leftStartAngle = leftClawCurrentSwingAngle;
        float rightStartAngle = rightClawCurrentSwingAngle;

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            float leftAngle = Mathf.Lerp(leftStartAngle, leftTargetAngle, k);
            float rightAngle = Mathf.Lerp(rightStartAngle, rightTargetAngle, k);
            ApplyClawSwingAngle(true, leftAngle);
            ApplyClawSwingAngle(false, rightAngle);
            yield return null;
        }

        ApplyClawSwingAngle(true, leftTargetAngle);
        ApplyClawSwingAngle(false, rightTargetAngle);
    }

    private void ApplyClawSwingAngle(bool left, float targetAngle)
    {
        Transform clawTransform = left ? leftClawTransform : rightClawTransform;

        if (clawTransform == null)
            return;

        Quaternion baseRot = left ? leftClawDefaultLocalRot : rightClawDefaultLocalRot;
        clawTransform.localRotation = baseRot * Quaternion.Euler(0f, 0f, targetAngle);
        if (left)
            leftClawCurrentSwingAngle = targetAngle;
        else
            rightClawCurrentSwingAngle = targetAngle;
    }

    protected override void OnBeforeIntroStart()
    {
        base.OnBeforeIntroStart();
        StartCoroutine(CoCaptainCrabIntroMotion());
    }

    private IEnumerator CoCaptainCrabIntroMotion()
    {
        float duration = IntroTime > 0.1f ? IntroTime : 2.5f;

        List<Transform> leftBones = new List<Transform>();
        List<Transform> rightBones = new List<Transform>();
        
        FindBonesRecursive(leftClawTransform, leftBones);
        FindBonesRecursive(rightClawTransform, rightBones);

        Dictionary<Transform, Quaternion> boneOrigRots = new Dictionary<Transform, Quaternion>();
        Dictionary<Transform, Vector3> boneOrigPos = new Dictionary<Transform, Vector3>();

        foreach (var b in leftBones)
        {
            boneOrigRots[b] = b.localRotation;
            boneOrigPos[b] = b.localPosition;
        }
        foreach (var b in rightBones)
        {
            boneOrigRots[b] = b.localRotation;
            boneOrigPos[b] = b.localPosition;
        }

        Quaternion leftArmOrigRot = leftClawTransform != null ? leftClawTransform.localRotation : Quaternion.identity;
        Quaternion rightArmOrigRot = rightClawTransform != null ? rightClawTransform.localRotation : Quaternion.identity;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            // 1. bone-n 만큼 z값을 갔다가 돌아오기 (sin 곡선: 0 -> 1 -> 0)
            float boneFactor = Mathf.Sin(t * Mathf.PI);
            float targetAngle = 30f * boneFactor;  // Z축 회전 변화
            float targetZPos = 1.5f * boneFactor;   // Z축 위치 변화

            foreach (var b in leftBones)
            {
                if (b == null) continue;
                b.localRotation = boneOrigRots[b] * Quaternion.Euler(0f, 0f, targetAngle);
                Vector3 p = boneOrigPos[b];
                p.z = boneOrigPos[b].z + targetZPos;
                b.localPosition = p;
            }

            foreach (var b in rightBones)
            {
                if (b == null) continue;
                b.localRotation = boneOrigRots[b] * Quaternion.Euler(0f, 0f, -targetAngle);
                Vector3 p = boneOrigPos[b];
                p.z = boneOrigPos[b].z + targetZPos;
                b.localPosition = p;
            }

            // 2. 기존 arm을 살짝 좌우로 2번 흔들기 (sin(t * 4 * PI))
            float shakeFactor = Mathf.Sin(t * 4f * Mathf.PI);
            float shakeAngle = 12f * shakeFactor;

            if (leftClawTransform != null)
            {
                leftClawTransform.localRotation = leftArmOrigRot * Quaternion.Euler(0f, 0f, shakeAngle);
            }
            if (rightClawTransform != null)
            {
                rightClawTransform.localRotation = rightArmOrigRot * Quaternion.Euler(0f, 0f, -shakeAngle);
            }

            yield return null;
        }

        // 복구
        foreach (var kvp in boneOrigRots)
        {
            if (kvp.Key != null) kvp.Key.localRotation = kvp.Value;
        }
        foreach (var kvp in boneOrigPos)
        {
            if (kvp.Key != null) kvp.Key.localPosition = kvp.Value;
        }
        if (leftClawTransform != null) leftClawTransform.localRotation = leftArmOrigRot;
        if (rightClawTransform != null) rightClawTransform.localRotation = rightArmOrigRot;
    }

    private void FindBonesRecursive(Transform parent, List<Transform> list)
    {
        if (parent == null) return;
        foreach (Transform child in parent)
        {
            if (child.name.ToLower().Contains("bone"))
            {
                list.Add(child);
            }
            FindBonesRecursive(child, list);
        }
    }
}

