using System.Collections;
using UnityEngine;

public class ResultBox : MonoBehaviour
{
    public bool isRareBox = false;
    public bool forceDNA = false; // 보스방에서 확정 DNA용
    public float coinDropChance = 0.5f; // 일반 상자에서 코인이 나올 확률

    [Header("Spawns")]
    public GameObject dnaPrefab; // 동적 스폰할 DNA 프리팹

    [Header("Animation (Placeholders)")]
    public Sprite closedSprite;
    public Sprite openedSprite;
    public Sprite[] openingSprites; // The opening animation sequence (e.g. chest_opening_1 to 7)
    public float animationFps = 12f;
    public SpriteRenderer boxSpriteRenderer; // 자식 오브젝트 (Box)의 SpriteRenderer

    [Header("Push Back")]
    public float pushForce = 5f; // AddForce(Impulse) 크기 (1/3로 조정됨)

    [Header("Reward Probabilities")]
    public float rareDnaChance = 0.01f; // 일반 상자에서 레어 DNA가 나올 확률 (기본 1%)

    private bool isOpened = false;

    public System.Action OnOpened;

    private void Awake()
    {
        if (boxSpriteRenderer == null)
        {
            boxSpriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (boxSpriteRenderer != null && closedSprite != null)
        {
            boxSpriteRenderer.sprite = closedSprite;
        }
    }

    // Box 자식의 Relay에서도 호출할 수 있도록 public
    public void OnBoxTriggerEnter(Collider2D collision)
    {
        if (isOpened || !gameObject.activeInHierarchy) return;

        PlayerStatControl player = collision.GetComponentInParent<PlayerStatControl>();
        if (player != null)
        {
            OpenBox(player);
        }
    }

    // 부모에 콜라이더가 있는 경우 직접 발동 (fallback)
    private void OnTriggerEnter2D(Collider2D collision)
    {
        OnBoxTriggerEnter(collision);
    }

    private void OpenBox(PlayerStatControl player)
    {
        isOpened = true;
        OnOpened?.Invoke();

        // Push player back slightly using Knockback coroutine
        Vector2 pushDir = (player.transform.position - transform.position).normalized;
        player.StartKnockback(pushDir, pushForce);

        StartCoroutine(CoPlayOpeningAnimation());
        StartCoroutine(SpawnRewardRoutine());
    }

    private IEnumerator CoPlayOpeningAnimation()
    {
        if (openingSprites != null && openingSprites.Length > 0 && boxSpriteRenderer != null)
        {
            float delay = 1f / animationFps;
            for (int i = 0; i < openingSprites.Length; i++)
            {
                if (openingSprites[i] != null)
                {
                    boxSpriteRenderer.sprite = openingSprites[i];
                }
                yield return new WaitForSeconds(delay);
            }
        }

        if (boxSpriteRenderer != null && openedSprite != null)
        {
            boxSpriteRenderer.sprite = openedSprite;
        }
    }

    private IEnumerator SpawnRewardRoutine()
    {
        Debug.Log($"[ResultBox] SpawnRewardRoutine started. isOpened={isOpened}, forceDNA={forceDNA}, isRareBox={isRareBox}");
        // Wait a short moment to let the player get pushed back and show open animation
        yield return new WaitForSeconds(0.5f);

        if (boxSpriteRenderer != null && openedSprite != null)
        {
            boxSpriteRenderer.sprite = openedSprite;
            Debug.Log("[ResultBox] Changed sprite to openedSprite.");
        }
        else
        {
            Debug.LogWarning($"[ResultBox] Failed to change sprite. boxSpriteRenderer={boxSpriteRenderer != null}, openedSprite={openedSprite != null}");
        }

        bool shouldSpawnDNA = isRareBox || forceDNA || (Random.value > coinDropChance);
        Debug.Log($"[ResultBox] Reward decision: shouldSpawnDNA={shouldSpawnDNA} (isRareBox={isRareBox}, forceDNA={forceDNA}, coinDropChance={coinDropChance})");

        if (shouldSpawnDNA)
        {
            SpawnDNA();
        }
        else
        {
            SpawnCoins();
        }
    }

    private void SpawnDNA()
    {
        GameObject targetDnaObj = null;

        if (dnaPrefab != null)
        {
            Vector3 spawnPos = transform.TransformPoint(new Vector3(0f, 0.5f, 0f));
            targetDnaObj = Instantiate(dnaPrefab, spawnPos, Quaternion.identity);
            Debug.Log($"[ResultBox] Instantiated dnaPrefab successfully at {spawnPos}.");
        }
        else
        {
            Debug.LogError("[ResultBox] SpawnDNA failed: dnaPrefab is null!");
        }

        if (targetDnaObj != null)
        {
            ResultDNA resultDna = targetDnaObj.GetComponent<ResultDNA>();
            if (resultDna != null)
            {
                // isRareBox가 true이거나 설정된 확률(rareDnaChance)에 당첨되었을 때 레어 DNA 소환
                bool spawnRareDna = isRareBox || (Random.value < rareDnaChance);
                Debug.Log($"[ResultBox] Initializing ResultDNA. isRare={spawnRareDna}");
                resultDna.Init(spawnRareDna);
            }
            else
            {
                Debug.LogWarning("[ResultBox] Spawned DNA object does not have ResultDNA component!");
            }
        }
    }

    private void SpawnCoins()
    {
        int coinAmount = Random.Range(3, 8);
        if (GameManager.Instance != null)
        {
            Debug.Log($"[ResultBox] Spawning {coinAmount} coins via GameManager at {transform.position}.");
            GameManager.Instance.SpawnCoinsForAmount(transform.position, coinAmount);
        }
        else if (TestGameManager.Instance != null)
        {
            Debug.Log($"[ResultBox] Spawning {coinAmount} coins via TestGameManager at {transform.position}.");
            TestGameManager.Instance.SpawnCoinsForAmount(transform.position, coinAmount);
        }
        else
        {
            Debug.LogError("[ResultBox] SpawnCoins failed: Both GameManager.Instance and TestGameManager.Instance are null!");
        }
    }
}
