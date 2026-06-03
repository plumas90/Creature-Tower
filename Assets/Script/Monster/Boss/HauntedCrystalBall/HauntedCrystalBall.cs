using System.Collections;
using UnityEngine;

public class HauntedCrystalBall : BossBase
{
    [Header("Crystal Ball Settings")]
    public HauntedCrystalBallSO crystalSO;

    [Header("Intro & Visuals")]
    [SerializeField] private Transform ghostVisual;
    [SerializeField] private float combatRotationSpeed = 90f;

    private Coroutine introCoroutine;
    private bool introFinished = false;
    private float currentRotZ = 0f;

    protected override void Awake()
    {
        base.Awake();

        // crystalSO를 MainSO에 자동 할당
        if (crystalSO != null)
            MainSO = crystalSO;

        if (ghostVisual == null)
            ghostVisual = transform.Find("ghost");
    }

    public override void StatSet()
    {
        // crystalSO를 MainSO에 자동 할당
        if (crystalSO != null)
            MainSO = crystalSO;

        if (crystalSO == null)
        {
            Debug.LogError("[HauntedCrystalBall] crystalSO is not assigned!");
            return;
        }

        bossCount = 1;
        
        base.StatSet(); // Intro 타임 및 Invincibility/Wait 타이밍 활성화를 위해 base.StatSet() 호출

        if (GameManager.Instance != null)
            Player = GameManager.Instance.playerOBJ;

        Debug.Log($"[HauntedCrystalBall] StatSet complete. HP: {curHp}/{maxHp}");
    }

    protected override float ResolveIntroTime()
    {
        return 5f; // 인트로 5초 고정
    }

    public override void OnBossActivatedBeforeIntro()
    {
        base.OnBossActivatedBeforeIntro();

        if (ghostVisual == null)
            ghostVisual = transform.Find("ghost");

        if (ghostVisual != null)
        {
            ghostVisual.localScale = new Vector3(50f, 50f, 1f);
            ghostVisual.localRotation = Quaternion.identity;
        }

        introFinished = false;
    }

    protected override void OnBeforeIntroStart()
    {
        base.OnBeforeIntroStart();

        if (ghostVisual == null)
            ghostVisual = transform.Find("ghost");

        introFinished = false;
        if (introCoroutine != null)
            StopCoroutine(introCoroutine);

        introCoroutine = StartCoroutine(CoIntroAnimation());
    }

    private IEnumerator CoIntroAnimation()
    {
        float duration = ResolveIntroTime();
        float elapsed = 0f;

        Vector3 startScale = new Vector3(50f, 50f, 1f);
        Vector3 targetScale = new Vector3(1.8f, 1.8f, 1f);

        if (ghostVisual != null)
            ghostVisual.localScale = startScale;

        currentRotZ = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            if (ghostVisual != null)
            {
                // 스케일을 50,50 에서 1.8, 1.8로 보간
                ghostVisual.localScale = Vector3.Lerp(startScale, targetScale, t);

                // Z값을 계속 올리면서 회전
                currentRotZ -= 360f * Time.deltaTime; // 초당 360도 (1바퀴)
                ghostVisual.localRotation = Quaternion.Euler(0f, 0f, currentRotZ);
            }

            yield return null;
        }

        if (ghostVisual != null)
            ghostVisual.localScale = targetScale;

        introFinished = true;
        introCoroutine = null;
    }

    public override void First()
    {
        base.First();

        if (introCoroutine != null)
        {
            StopCoroutine(introCoroutine);
            introCoroutine = null;
        }

        if (ghostVisual != null)
            ghostVisual.localScale = new Vector3(1.8f, 1.8f, 1f);

        introFinished = true;
    }

    private void Update()
    {
        if (introFinished && ghostVisual != null)
        {
            // 보스전 동안 회전 속도 적용
            currentRotZ -= combatRotationSpeed * Time.deltaTime;
            ghostVisual.localRotation = Quaternion.Euler(0f, 0f, currentRotZ);
        }
    }

    // BossBase의 데미지 계산 훅을 override하여 항상 1 데미지로 고정
    protected override float CalculateFinalDamage(float incomingDamage)
    {
        float fixedDamage = 1f;
        if (crystalSO != null)
            fixedDamage = crystalSO.incomingDamageOverride;

        Debug.Log($"[HauntedCrystalBall] CalculateFinalDamage: {incomingDamage} → {fixedDamage}");
        return fixedDamage;
    }

    // Bullet 충돌 시 호출되는 훅 - 여기서 패턴 실행
    protected override void OnDamagedByBullet(Bullet bullet, float finalDamage)
    {
        base.OnDamagedByBullet(bullet, finalDamage);

        if (isDead || !live)
            return;

        // 피격 시마다 랜덤 패턴 즉시 실행
        int randomPattern = Random.Range(1, 5); // 1~4
        Debug.Log($"[HauntedCrystalBall] Executing pattern {randomPattern} on bullet hit, HP: {curHp}/{maxHp}");
        StartCoroutine(ExecutePattern(randomPattern));
    }

    // Damege() 메서드도 유지 (다른 경로로 데미지 받을 경우 대비)
    public override void Damege(float damage)
    {
        if (isDead || !live)
            return;

        // 피격 시마다 랜덤 패턴 즉시 실행 (데미지 처리 전에)
        int randomPattern = Random.Range(1, 5); // 1~4
        Debug.Log($"[HauntedCrystalBall] Executing pattern {randomPattern} BEFORE damage, HP: {curHp}/{maxHp}");
        StartCoroutine(ExecutePattern(randomPattern));

        // 그 후 데미지 처리
        base.Damege(damage);
        Debug.Log($"[HauntedCrystalBall] After damage, HP: {curHp}/{maxHp}, isDead: {isDead}");
    }

    private IEnumerator ExecutePattern(int patternNumber)
    {
        if (crystalSO == null)
            yield break;

        switch (patternNumber)
        {
            case 1: // 십자 발사
                ShootCrossPattern();
                break;
            case 2: // X자 발사
                ShootXPattern();
                break;
            case 3: // 회전구
                SpawnRotatingCircles();
                break;
            case 4: // 랜덤 타일
                SpawnRandomTiles();
                break;
        }

        yield return null;
    }

    private GameObject GetRandomGhostPrefab()
    {
        if (crystalSO.blackGhostPrefab != null && crystalSO.whiteGhostPrefab != null)
        {
            return Random.value < 0.5f ? crystalSO.blackGhostPrefab : crystalSO.whiteGhostPrefab;
        }
        return crystalSO.ghostPrefab;
    }

    private void ShootCrossPattern()
    {
        GameObject ghostPrefab = GetRandomGhostPrefab();
        if (ghostPrefab == null) return;

        Vector2[] directions = { Vector2.up, Vector2.down, Vector2.left, Vector2.right };
        foreach (Vector2 dir in directions)
        {
            float randomOffset = Random.Range(-22.5f, 22.5f);
            Quaternion randomRot = Quaternion.Euler(0f, 0f, randomOffset);
            GameObject ghost = Instantiate(ghostPrefab, transform.position, Quaternion.identity);
            var ghostScript = ghost.GetComponent<HauntedCrystalBallGhost>();
            if (ghostScript != null)
                ghostScript.Initialize((randomRot * dir).normalized, crystalSO.pattern1GhostSpeed, crystalSO.pattern1Damage);
        }
        Debug.Log("[HauntedCrystalBall] Cross pattern executed");
    }

    private void ShootXPattern()
    {
        GameObject ghostPrefab = GetRandomGhostPrefab();
        if (ghostPrefab == null) return;

        Vector2[] directions = {
            new Vector2(1, 1).normalized,
            new Vector2(-1, 1).normalized,
            new Vector2(1, -1).normalized,
            new Vector2(-1, -1).normalized
        };
        foreach (Vector2 dir in directions)
        {
            float randomOffset = Random.Range(-22.5f, 22.5f);
            Quaternion randomRot = Quaternion.Euler(0f, 0f, randomOffset);
            GameObject ghost = Instantiate(ghostPrefab, transform.position, Quaternion.identity);
            var ghostScript = ghost.GetComponent<HauntedCrystalBallGhost>();
            if (ghostScript != null)
                ghostScript.Initialize((randomRot * dir).normalized, crystalSO.pattern2GhostSpeed, crystalSO.pattern2Damage);
        }
        Debug.Log("[HauntedCrystalBall] X pattern executed");
    }

    private void SpawnRotatingCircles()
    {
        if (crystalSO.ghostCirclePrefab == null) return;

        Vector2 bossPos = transform.position;
        
        // 왼쪽 구 - 랜덤 거리
        float leftDistance = Random.Range(crystalSO.pattern3SpawnDistanceMin, crystalSO.pattern3SpawnDistanceMax);
        Vector2 leftPos = bossPos + Vector2.left * leftDistance;
        GameObject leftCircle = Instantiate(crystalSO.ghostCirclePrefab, leftPos, Quaternion.identity);
        var leftScript = leftCircle.GetComponent<HauntedCrystalBallGhostCircle>();
        if (leftScript != null)
            leftScript.Initialize(bossPos, leftDistance, crystalSO.pattern3RotationSpeed, crystalSO.pattern3Damage, true, crystalSO.pattern3WaitTime);

        // 오른쪽 구 - 랜덤 거리
        float rightDistance = Random.Range(crystalSO.pattern3SpawnDistanceMin, crystalSO.pattern3SpawnDistanceMax);
        Vector2 rightPos = bossPos + Vector2.right * rightDistance;
        GameObject rightCircle = Instantiate(crystalSO.ghostCirclePrefab, rightPos, Quaternion.identity);
        var rightScript = rightCircle.GetComponent<HauntedCrystalBallGhostCircle>();
        if (rightScript != null)
            rightScript.Initialize(bossPos, rightDistance, crystalSO.pattern3RotationSpeed, crystalSO.pattern3Damage, false, crystalSO.pattern3WaitTime);

        Debug.Log($"[HauntedCrystalBall] Rotating circles spawned (left: {leftDistance:F2}, right: {rightDistance:F2})");
    }

    private void SpawnRandomTiles()
    {
        if (crystalSO.tilePrefab == null || StageOwner == null) return;

        for (int i = 0; i < crystalSO.pattern4TileCount; i++)
        {
            Vector2 randomPos = StageOwner.GetRandomPositionInZone();
            GameObject tile = Instantiate(crystalSO.tilePrefab, randomPos, Quaternion.identity);
            var tileScript = tile.GetComponent<HauntedCrystalBallTile>();
            if (tileScript != null)
                tileScript.Initialize(crystalSO.pattern4Damage, crystalSO.pattern4WarningTime, crystalSO.pattern4ActiveTime);
        }
        Debug.Log($"[HauntedCrystalBall] {crystalSO.pattern4TileCount} tiles spawned");
    }

    public override void BossDie()
    {
        if (isDead)
            return;

        Debug.Log("[HauntedCrystalBall] Boss died!");
        base.BossDie();
        
        // 오브젝트 비활성화
        gameObject.SetActive(false);
    }

    public new string GetDisplayName()
    {
        return "Haunted Crystal Ball";
    }
}
