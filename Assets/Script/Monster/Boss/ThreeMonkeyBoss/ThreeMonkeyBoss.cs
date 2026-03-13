using System.Collections;
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


    private Vector3 zero = new Vector3(0,0);
    private Vector3 midlle = new Vector3(0,1.5f);

    [Header("Monkey Effect")]
    public float collisionEffectDuration = 1f;

    [Header("Intro Animation")]
    [Min(0f)] public float introRotateAngle = 35f;

    private bool eyeDetached;
    private bool earDetached;
    private bool companionCleared;

    // 최초 하단은 눈 원숭이
    private MonkeyEffectType currentBottomEffect = MonkeyEffectType.Eye;
    private Vector2 lastHitBulletDirection = Vector2.right;

    Transform targetPlayerTransform;
    Vector2 direction = Vector2.zero;

    public override void StatSet() 
    {
        base.StatSet();
        _boxCollider2D = this.GetComponent<BoxCollider2D>();

        // 분리형 보스 카운트 규칙: 시작은 본체 1마리
        bossCount = 1;
        if (GameManager.Instance != null)
            GameManager.Instance.bossCount = 1;

        eyeDetached = false;
        earDetached = false;
        companionCleared = false;
        currentBottomEffect = MonkeyEffectType.Eye;

        EnsureVisualState();

        if (Player != null)
            targetPlayerTransform = Player.transform;

        if (IntroTime > 0f)
            StartCoroutine(PlayStackIntroAnimation(IntroTime));

        Debug.Log($"[ThreeMonkeyBoss] StatSet done | pos={transform.position} | active={gameObject.activeSelf}");
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
        if (wait)
        {

        }
        else 
        {
            transform.Translate(direction * speed * Time.deltaTime);
        }
    }
    
    //TODO   ���̾� �ٲٱ� �� �ؾߵ�;
    public override void First()
    {
        GetDirection();
    }

    public void GetDirection() 
    {
        if (targetPlayerTransform == null && Player != null)
            targetPlayerTransform = Player.transform;
        if (targetPlayerTransform == null)
            return;

        Vector2 me = transform.position;
        Vector2 u = targetPlayerTransform.position;
        direction = (u - me).normalized;
    }

    public override void OnCollisionEnter2D(Collision2D collision)
    {
        base.OnCollisionEnter2D(collision);

        if (collision.gameObject.TryGetComponent(out PlayerStatControl playerStat))
            ApplyMonkeyEffect(playerStat, currentBottomEffect);

        // 벽/플레이어/동일 계열 오브젝트 충돌 시 반사
        if (IsReflectTargetLayer(collision.gameObject.layer))
        {
            Vector3 normal = collision.contacts[0].normal; // ��������
            direction = Vector3.Reflect(direction, normal).normalized; // �ݻ�
        }

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
        if (GameManager.Instance == null) return;
        GameManager.Instance.bossCount += value;
    }

    // mouthDetachedPrefab 슬롯은 현재 3단 분리 확장(입 분리 추가) 시 사용 예정.

    IEnumerator Run(float duration , GameObject target , Vector3 endposition)
    {
        if (target == null) yield break;

        var runTime = 0.0f;
        Transform moveTarget = target.transform;
        while (runTime < duration)
        {
            runTime += Time.deltaTime;

            moveTarget.position = Vector3.Lerp(moveTarget.position, endposition, runTime / duration);

            yield return null;
        }

        moveTarget.position = endposition;
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

}
