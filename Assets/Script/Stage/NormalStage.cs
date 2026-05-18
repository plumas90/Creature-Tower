using UnityEngine;
using System.Collections.Generic;

public class NormalStage : Stage
{
    public enum NormalStageCase
    {
        MonsterWithReward,   // 전투 방 (전투 클리어 시 보상 스폰)
        MonsterNoReward,     // 꽝 전투 방 (전투 클리어 시 보상 없음)
        ChoiceResult         // 안전 보상 방 (처음부터 보상 스폰)
    }

    public enum RoomTheme
    {
        Mystery,       // 물음표 (?) 방 - 무작위 보상 및 저울 기믹 가능
        Shop,          // 상점 방 - 확정적으로 상점 스폰
        Transfusion,   // 수혈기 방 - 확정적으로 수혈기 스폰
        DNA,           // DNA 방 - 확정적으로 DNA 상자(AugmentBox) 스폰
        Coin,          // 코인 방 - 확정적으로 코인 상자(CoinBox) 스폰
        Box,           // 일반 상자 방 - 확정적으로 골드 또는 증강 상자(CoinBox 또는 AugmentBox) 중 스폰
        Potion         // 포션 방 - 확정적으로 포션(50% 회복) 스폰
    }

    public enum RewardType
    {
        RandomBox,
        CoinBox,
        AugmentBox,
        Shop,
        Transfuser,
        Potion
    }

    [Header("Normal Stage Case")]
    [SerializeField] private NormalStageCase stageCase = NormalStageCase.MonsterWithReward;
    [SerializeField] private RoomTheme roomTheme = RoomTheme.Mystery;

    // GameManager나 지도 생성기에서 호출하여 씬 생성 시 활용하는 GET/SET 프로퍼티
    public NormalStageCase GetStageCase() => stageCase;
    public void SetStageCase(NormalStageCase value) => stageCase = value;

    public RoomTheme GetRoomTheme() => roomTheme;
    public void SetRoomTheme(RoomTheme value) => roomTheme = value;

    // 인스펙터 상태를 리셋하거나 런타임에 동적으로 스테이지를 초기화하는 용도
    public void InitStage(int roomNo, NormalStageCase newCase, RoomTheme newTheme)
    {
        roomNumber = roomNo;
        stageCase = newCase;
        roomTheme = newTheme;

        // 동적 도어 및 게이트 상태 초기화
        SetupMonsterGate();

        // 런타임 소환 보상 초기화
        foreach (var obj in spawnedRewards)
        {
            if (obj != null) Destroy(obj);
        }
        spawnedRewards.Clear();

        if (spawnedBalanceScale != null)
        {
            Destroy(spawnedBalanceScale);
            spawnedBalanceScale = null;
        }

        hasExecutedWaves = false;
    }

    public void SetRequiredMonsterCount(int count)
    {
        remainingMonsterCount = count;
        monsterGateActive = count > 0;
        
        Debug.Log($"[NormalStage] SetRequiredMonsterCount: count={count}, remainingMonsterCount={remainingMonsterCount}, monsterGateActive={monsterGateActive}");

        if (monsterGateActive)
        {
            CloseTopDoor();
            // 입장 전이므로 botDoor(입구)는 닫지 않음. InCheckClear에서 닫힙니다.
        }
        else
        {
            OpenTopDoor();
            if (botDoor != null)
                OpenBotDoor();
        }
    }

    [Header("Spawn Point")]
    public GameObject spawnPointStairs;
    public Transform PlayerSpawnPoint;
    public Transform PlayerStartPosition;
    public NextStageStairs NextStageStairs;


    [Header("Choice Result Layout")]
    [SerializeField] private BoxCollider2D battleZone;
    [SerializeField] private float choiceResultSpacing = 2f;

    [Header("Unified Dynamic Rewards")]
    [SerializeField] private GameObject randomBoxPrefab;
    [SerializeField] private GameObject coinBoxPrefab;
    [SerializeField] private GameObject augmentBoxPrefab;
    [SerializeField] private GameObject shopPrefab;
    [SerializeField] private GameObject bloodTransfusionPrefab;
    [SerializeField] private GameObject balanceScalePrefab;
    [SerializeField] private GameObject potionPrefab;
    [SerializeField] private int maxRewardOverride = 0; // 0이면 Act 제한 적용, 1~4면 고정 제한
    [SerializeField] private float balanceScaleGimmickChance = 0f; // 저울 양자택일방 스폰 확률 (우선 비활성화) // 저울 양자택일방 스폰 확률

    [Header("Monster Wave Spawner")]
    [SerializeField] private MonsterGroupSO monsterGroup;
    [SerializeField] private List<MonsterGroupSO> availableMonsterGroups = new List<MonsterGroupSO>();

    private int remainingMonsterCount;
    private bool monsterGateActive;
    private List<GameObject> spawnedRewards = new List<GameObject>();
    private GameObject spawnedBalanceScale = null;
    private bool hasExecutedWaves = false;

    protected override void Awake()
    {
        base.Awake();
    }

    public override Transform GetPlayerSpawnPoint()
    {
        return PlayerSpawnPoint;
    }

    public override void ReadyStage()
    {
        base.ReadyStage();

        // 물음표(?) 방이고 전투가 계획된 방인 경우, 20% 확률로 몬스터 전투 없이 바로 보상을 스폰하는 안전 방(ChoiceResult)으로 전환
        // 물음표(?) 방 20% 확률 전투 스킵 기능 우선 비활성화 (전투 및 일반 보상 테스트를 원활하게 수행하기 위함)
        /*
        if (roomTheme == RoomTheme.Mystery && stageCase == NormalStageCase.MonsterWithReward)
        {
            if (Random.value <= 0.2f)
            {
                stageCase = NormalStageCase.ChoiceResult;
                Debug.Log("[NormalStage] ? 방 20% 보너스 성공! 전투가 없고 바로 보상을 주는 안전 선택방으로 변경되었습니다.");
            }
        }
        */

        SetupMonsterGate();

        // ChoiceResult(보상방)인 경우 진입 즉시 보상 스폰
        if (stageCase == NormalStageCase.ChoiceResult)
        {
            ResultSummon();
        }
        else
        {
            PrepareMonsterWaves();
        }

        if (GameManager.Instance != null)
            GameManager.Instance.BossCountSet(0);

        // 씬에 미리 배치되어 있는 일반 몬스터가 있다면 자동 감지 및 등록
        EnemyBase[] prePlacedEnemies = GetComponentsInChildren<EnemyBase>(true);
        if (prePlacedEnemies != null && prePlacedEnemies.Length > 0)
        {
            int prePlacedCount = 0;
            foreach (var enemy in prePlacedEnemies)
            {
                if (enemy.ownerStage == null)
                {
                    enemy.ownerStage = this;
                    prePlacedCount++;
                }
            }
            if (prePlacedCount > 0)
            {
                Debug.Log($"[NormalStage] 씬에 미리 배치된 몬스터 {prePlacedCount}마리를 감지하여 등록했습니다.");
                int currentRequired = remainingMonsterCount > 0 ? remainingMonsterCount : 0;
                SetRequiredMonsterCount(currentRequired + prePlacedCount);
            }
        }
    }

    public override void InCheckClear(GameObject player)
    {
        base.InCheckClear(player);

        if (hasExecutedWaves)
            return;
        hasExecutedWaves = true;

        if (monsterGateActive && botDoor != null)
            CloseBotDoor();

        if (stageCase != NormalStageCase.ChoiceResult)
        {
            ExecuteMonsterWaves();
        }
    }

    public void NotifyNormalMonsterDied(int count = 1)
    {
        if (!monsterGateActive)
        {
            Debug.LogWarning($"[NormalStage] NotifyNormalMonsterDied 호출됨. 하지만 monsterGateActive가 false입니다. count={count}");
            return;
        }

        if (count <= 0)
            count = 1;

        remainingMonsterCount = Mathf.Max(0, remainingMonsterCount - count);
        Debug.Log($"[NormalStage] NotifyNormalMonsterDied: count={count}, remainingMonsterCount={remainingMonsterCount}");

        if (remainingMonsterCount == 0)
        {
            Debug.Log("[NormalStage] 모든 몬스터 처치 완료! 문 개방 및 보상 생성 준비");
            OpenTopDoor();
            if (botDoor != null)
                OpenBotDoor();
            monsterGateActive = false;
            
            // 몬스터 처치 완료 후, 꽝 전투 방(MonsterNoReward)이 아닐 때만 보상 스폰!
            if (stageCase != NormalStageCase.MonsterNoReward)
            {
                ResultSummon();
            }
            else
            {
                Debug.Log("[NormalStage] 이 방은 꽝 전투 방(MonsterNoReward)입니다. 보상이 스폰되지 않습니다.");
            }
        }
    }

    public void ResultSummon()
    {
        SpawnUnifiedResults();
    }

    private void SpawnUnifiedResults()
    {
        Debug.Log($"[NormalStage] SpawnUnifiedResults() 실행됨. roomTheme={roomTheme}, stageCase={stageCase}");
        // 이전 소환 오브젝트 정리
        foreach (var obj in spawnedRewards)
        {
            if (obj != null) Destroy(obj);
        }
        spawnedRewards.Clear();

        if (spawnedBalanceScale != null)
        {
            Destroy(spawnedBalanceScale);
            spawnedBalanceScale = null;
        }

        // 2. 테마에 따른 보상 구성 및 스폰 갯수 결정
        List<RewardType> chosenTypes = new List<RewardType>();
        bool allowBalanceScale = false;

        if (roomTheme == RoomTheme.Shop)
        {
            chosenTypes.Add(RewardType.Shop);
        }
        else if (roomTheme == RoomTheme.Transfusion)
        {
            chosenTypes.Add(RewardType.Transfuser);
        }
        else if (roomTheme == RoomTheme.DNA)
        {
            chosenTypes.Add(RewardType.AugmentBox);
        }
        else if (roomTheme == RoomTheme.Coin)
        {
            chosenTypes.Add(RewardType.CoinBox);
        }
        else if (roomTheme == RoomTheme.Box)
        {
            // 골드(CoinBox) 또는 증강(AugmentBox) 중 무작위 선택
            RewardType[] chestTypes = { RewardType.CoinBox, RewardType.AugmentBox };
            chosenTypes.Add(chestTypes[Random.Range(0, chestTypes.Length)]);
        }
        else if (roomTheme == RoomTheme.Potion)
        {
            chosenTypes.Add(RewardType.Potion);
        }
        else // Mystery (?)
        {
            allowBalanceScale = true;

            int maxRewards = ResolveMaxRewards();
            List<RewardType> pool = new List<RewardType>()
            {
                RewardType.RandomBox,
                RewardType.CoinBox,
                RewardType.AugmentBox,
                RewardType.Shop,
                RewardType.Transfuser,
                RewardType.Potion
            };

            // 기본 1개 선택 및 보상 제거
            RewardType baseType = pool[Random.Range(0, pool.Count)];
            chosenTypes.Add(baseType);
            pool.Remove(baseType);

            // 20% 확률 연쇄 추가 스폰 롤링
            int spawnedCount = 1;
            while (spawnedCount < maxRewards && pool.Count > 0)
            {
                if (Random.value <= 0.2f) // 20%
                {
                    RewardType extraType = pool[Random.Range(0, pool.Count)];
                    chosenTypes.Add(extraType);
                    pool.Remove(extraType);
                    spawnedCount++;
                }
                else
                {
                    break;
                }
            }
        }

        // 4. 기믹 선택 (저울 양자택일 vs 연쇄 획득)
        Vector2 center = battleZone != null ? (Vector2)battleZone.bounds.center : (Vector2)transform.position;
        // bool doBalanceScale = allowBalanceScale && chosenTypes.Count >= 2 && Random.value <= balanceScaleGimmickChance && balanceScalePrefab != null;
        
        // 보상선택방(천칭) 기능 준비 전까지 완전히 등장하지 않도록 강제 비활성화
        bool doBalanceScale = false; 

        if (doBalanceScale)
        {
            // 기믹 B: 저울 양자택일방
            spawnedBalanceScale = Instantiate(balanceScalePrefab, center, Quaternion.identity, transform);
            var scaleComp = spawnedBalanceScale.GetComponent("BalanceScale");

            // 무작위로 선택된 보상 두 개를 소환
            GameObject leftObj = SpawnRewardObject(chosenTypes[0]);
            GameObject rightObj = SpawnRewardObject(chosenTypes[1]);

            if (leftObj != null) spawnedRewards.Add(leftObj);
            if (rightObj != null) spawnedRewards.Add(rightObj);

            if (scaleComp != null)
            {
                scaleComp.GetType().GetMethod("Setup")?.Invoke(scaleComp, new object[] { leftObj, rightObj });
            }
            else
            {
                Debug.LogWarning("[NormalStage] BalanceScale 컴포넌트가 프리팹에 없습니다.");
                LayoutUnifiedResults(spawnedRewards);
            }
        }
        else
        {
            // 기믹 A: 연쇄 획득방 (모두 획득 가능)
            foreach (var type in chosenTypes)
            {
                GameObject obj = SpawnRewardObject(type);
                if (obj != null)
                {
                    spawnedRewards.Add(obj);
                }
            }

            // 가로 정렬 배치
            LayoutUnifiedResults(spawnedRewards);
        }

        // 보상 프리팹 페이드인 애니메이션 및 1초간 상호작용 차단 코루틴 작동
        StartCoroutine(FadeInAndActivateRewards(spawnedRewards, spawnedBalanceScale));
    }

    private System.Collections.IEnumerator FadeInAndActivateRewards(List<GameObject> rewards, GameObject scaleObj)
    {
        // 1. 모든 보상 객체의 Collider2D 수집 후 비활성화
        List<Collider2D> colliders = new List<Collider2D>();
        List<SpriteRenderer> renderers = new List<SpriteRenderer>();

        foreach (var obj in rewards)
        {
            if (obj == null) continue;
            colliders.AddRange(obj.GetComponentsInChildren<Collider2D>(true));
            renderers.AddRange(obj.GetComponentsInChildren<SpriteRenderer>(true));
        }

        if (scaleObj != null)
        {
            colliders.AddRange(scaleObj.GetComponentsInChildren<Collider2D>(true));
            renderers.AddRange(scaleObj.GetComponentsInChildren<SpriteRenderer>(true));
        }

        // 콜라이더 비활성화 (물리 충돌/트리거 차단)
        foreach (var col in colliders)
        {
            if (col != null) col.enabled = false;
        }

        // 투명도 0으로 설정
        foreach (var sr in renderers)
        {
            if (sr != null)
            {
                Color color = sr.color;
                color.a = 0f;
                sr.color = color;
            }
        }

        // 2. 1초 동안 서서히 페이드인 (투명도 0% -> 100%)
        float duration = 1.0f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Clamp01(elapsed / duration);

            foreach (var sr in renderers)
            {
                if (sr != null)
                {
                    Color color = sr.color;
                    color.a = alpha;
                    sr.color = color;
                }
            }
            yield return null;
        }

        // 마지막 100% 알파 보장
        foreach (var sr in renderers)
        {
            if (sr != null)
            {
                Color color = sr.color;
                color.a = 1f;
                sr.color = color;
            }
        }

        // 3. 콜라이더 다시 활성화 (상호작용 잠금 해제)
        foreach (var col in colliders)
        {
            if (col != null) col.enabled = true;
        }
    }

    private int ResolveMaxRewards()
    {
        if (maxRewardOverride >= 1 && maxRewardOverride <= 4)
            return maxRewardOverride;

        // 1~5층: 2, 6~10층: 3, 11층~: 4
        if (roomNumber <= 5) return 2;
        if (roomNumber <= 10) return 3;
        return 4;
    }

    private GameObject SpawnRewardObject(RewardType type)
    {
        GameObject spawned = null;
        switch (type)
        {
            case RewardType.RandomBox:
                if (randomBoxPrefab != null)
                {
                    spawned = Instantiate(randomBoxPrefab, transform);
                    ConfigureResultBox(spawned, 0.5f, false);
                }
                else Debug.LogError("[NormalStage] randomBoxPrefab이 할당되지 않았습니다!");
                break;
            case RewardType.CoinBox:
                if (coinBoxPrefab != null)
                {
                    spawned = Instantiate(coinBoxPrefab, transform);
                    ConfigureResultBox(spawned, 1.0f, false);
                }
                else Debug.LogError("[NormalStage] coinBoxPrefab이 할당되지 않았습니다!");
                break;
            case RewardType.AugmentBox:
                if (augmentBoxPrefab != null)
                {
                    spawned = Instantiate(augmentBoxPrefab, transform);
                    ConfigureResultBox(spawned, 0f, true);
                }
                else Debug.LogError("[NormalStage] augmentBoxPrefab이 할당되지 않았습니다!");
                break;
            case RewardType.Shop:
                if (shopPrefab != null)
                {
                    spawned = Instantiate(shopPrefab, transform);
                    ShopController shop = spawned.GetComponent<ShopController>();
                    if (shop != null) shop.InitShop();
                }
                else Debug.LogError("[NormalStage] shopPrefab이 할당되지 않았습니다!");
                break;
            case RewardType.Transfuser:
                if (bloodTransfusionPrefab != null)
                {
                    spawned = Instantiate(bloodTransfusionPrefab, transform);
                    BloodTransfusionDevice bt = spawned.GetComponent<BloodTransfusionDevice>();
                    if (bt != null) bt.Init($"Stage-{roomNumber}", bt.name);
                }
                else Debug.LogError("[NormalStage] bloodTransfusionPrefab이 할당되지 않았습니다!");
                break;
            case RewardType.Potion:
                if (potionPrefab != null)
                {
                    spawned = Instantiate(potionPrefab, transform);
                    Potion pot = spawned.GetComponentInChildren<Potion>();
                    if (pot != null)
                    {
                        pot.InitFixed(0.5f); // 항상 50% 포션
                    }
                }
                else Debug.LogError("[NormalStage] potionPrefab이 할당되지 않았습니다!");
                break;
        }

        if (spawned != null)
        {
            spawned.SetActive(true);
        }
        return spawned;
    }

    private void ConfigureResultBox(GameObject boxObj, float coinChance, bool forceDna)
    {
        ResultBox box = boxObj.GetComponent<ResultBox>();
        if (box != null)
        {
            box.coinDropChance = coinChance;
            box.forceDNA = forceDna;
        }
    }

    private void LayoutUnifiedResults(List<GameObject> items)
    {
        if (items == null || items.Count == 0)
            return;

        Vector2 center = battleZone != null ? (Vector2)battleZone.bounds.center : (Vector2)transform.position;
        float spacing = Mathf.Max(0.1f, choiceResultSpacing);
        float startOffset = -((items.Count - 1) * 0.5f) * spacing;

        for (int i = 0; i < items.Count; i++)
        {
            GameObject target = items[i];
            if (target == null)
                continue;

            float offsetX = startOffset + (i * spacing);
            Vector3 pos = target.transform.position;
            target.transform.position = new Vector3(center.x + offsetX, center.y, pos.z);
        }
    }

    private void SetupMonsterGate()
    {
        if (stageCase == NormalStageCase.ChoiceResult)
        {
            remainingMonsterCount = 0;
            monsterGateActive = false;
            OpenTopDoor();
            if (botDoor != null)
                OpenBotDoor();
            return;
        }

        // 전투 방인 경우 초기 게이트 상태만 설정 (실제 몬스터 마릿수는 외부 소환 그룹이 설정함)
        // 기본적으로는 외부 스폰 시스템이 SetRequiredMonsterCount를 호출하여 채워주기 전까지 대기 상태
        monsterGateActive = true;
        CloseTopDoor();
        // 입장 전이므로 botDoor(입구)는 닫지 않음. InCheckClear에서 닫힙니다.
    }

    private int GetCurrentFloor()
    {
        return Mathf.Abs(roomNumber) + 1;
    }

    private void PrepareMonsterWaves()
    {
        if (monsterGroup == null && availableMonsterGroups != null && availableMonsterGroups.Count > 0)
        {
            int currentFloor = GetCurrentFloor();
            List<MonsterGroupSO> matchingGroups = new List<MonsterGroupSO>();
            
            foreach (var group in availableMonsterGroups)
            {
                if (group != null && currentFloor >= group.targetFloorMin && currentFloor <= group.targetFloorMax)
                {
                    matchingGroups.Add(group);
                }
            }

            if (matchingGroups.Count > 0)
            {
                monsterGroup = matchingGroups[Random.Range(0, matchingGroups.Count)];
                Debug.Log($"[NormalStage] 현재 {currentFloor}층에 적합한 몬스터 그룹 '{monsterGroup.groupName}'을(를) 자동 선택했습니다.");
            }
        }

        if (monsterGroup == null)
        {
            Debug.LogWarning("[NormalStage] monsterGroup이 설정되지 않아 스폰을 건너뜁니다.");
            SetRequiredMonsterCount(0);
            return;
        }

        int totalMonsters = 0;
        foreach (var wave in monsterGroup.waves)
        {
            if (wave != null && wave.spawnList != null)
            {
                totalMonsters += wave.spawnList.Count;
            }
        }

        SetRequiredMonsterCount(totalMonsters);
    }

    private void ExecuteMonsterWaves()
    {
        if (remainingMonsterCount > 0)
        {
            StartCoroutine(SpawnWavesCoroutine());
        }
    }

    private System.Collections.IEnumerator SpawnWavesCoroutine()
    {
        if (monsterGroup == null) yield break;

        Bounds bounds = battleZone != null ? battleZone.bounds : new Bounds(transform.position, new Vector3(10, 10, 0));

        for (int waveIndex = 0; waveIndex < monsterGroup.waves.Count; waveIndex++)
        {
            var wave = monsterGroup.waves[waveIndex];
            if (wave == null) continue;

            if (wave.delayBeforeWave > 0f)
            {
                yield return new WaitForSeconds(wave.delayBeforeWave);
            }

            if (wave.spawnList != null)
            {
                foreach (var spawnData in wave.spawnList)
                {
                    if (spawnData == null || spawnData.monsterPrefab == null) continue;

                    // nxn zone (battleZone) 내에서 무작위 위치 계산
                    float rx = Random.Range(bounds.min.x + 1f, bounds.max.x - 1f);
                    float ry = Random.Range(bounds.min.y + 1f, bounds.max.y - 1f);
                    Vector3 spawnPos = new Vector3(rx, ry, transform.position.z);

                    GameObject monsterObj = Instantiate(spawnData.monsterPrefab, spawnPos, Quaternion.identity, transform);
                    
                    EnemyBase enemy = monsterObj.GetComponent<EnemyBase>();
                    if (enemy != null)
                    {
                        enemy.ownerStage = this;
                    }
                }
            }
        }
    }
}
