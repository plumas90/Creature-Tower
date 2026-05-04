using System.Collections;
using UnityEngine;

public class HauntedCrystalBall : BossBase
{
    [Header("Crystal Ball Settings")]
    public HauntedCrystalBallSO crystalSO;

    protected override void Awake()
    {
        base.Awake();

        // crystalSO를 MainSO에 자동 할당
        if (crystalSO != null)
            MainSO = crystalSO;
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
        atk = crystalSO.atk;
        maxHp = crystalSO.hp;
        curHp = crystalSO.hp;
        speed = crystalSO.speed; // 0
        live = true;
        invincibility = false;
        wait = false;

        if (GameManager.Instance != null)
            Player = GameManager.Instance.playerOBJ;

        Debug.Log($"[HauntedCrystalBall] StatSet complete. HP: {curHp}/{maxHp}");
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

    private void ShootCrossPattern()
    {
        if (crystalSO.ghostPrefab == null) return;

        Vector2[] directions = { Vector2.up, Vector2.down, Vector2.left, Vector2.right };
        foreach (Vector2 dir in directions)
        {
            float randomOffset = Random.Range(-22.5f, 22.5f);
            Quaternion randomRot = Quaternion.Euler(0f, 0f, randomOffset);
            GameObject ghost = Instantiate(crystalSO.ghostPrefab, transform.position, Quaternion.identity);
            var ghostScript = ghost.GetComponent<HauntedCrystalBallGhost>();
            if (ghostScript != null)
                ghostScript.Initialize((randomRot * dir).normalized, crystalSO.pattern1GhostSpeed, crystalSO.pattern1Damage);
        }
        Debug.Log("[HauntedCrystalBall] Cross pattern executed");
    }

    private void ShootXPattern()
    {
        if (crystalSO.ghostPrefab == null) return;

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
            GameObject ghost = Instantiate(crystalSO.ghostPrefab, transform.position, Quaternion.identity);
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
