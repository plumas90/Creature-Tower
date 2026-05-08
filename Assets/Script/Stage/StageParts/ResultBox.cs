using System.Collections;
using UnityEngine;

public class ResultBox : MonoBehaviour
{
    public bool isRareBox = false;
    public bool forceDNA = false; // 보스방에서 확정 DNA용
    public float coinDropChance = 0.5f; // 일반 상자에서 코인이 나올 확률

    [Header("Spawns")]
    public GameObject resultDNAPrefab;

    [Header("Animation (Placeholders)")]
    public Sprite closedSprite;
    public Sprite openedSprite;
    private SpriteRenderer sr;

    [Header("Push Back")]
    public float pushForce = 5f;

    private bool isOpened = false;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr != null && closedSprite != null)
        {
            sr.sprite = closedSprite;
        }

        Collider2D col = GetComponent<Collider2D>();
        if (col == null)
        {
            col = gameObject.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isOpened) return;

        PlayerStatControl player = collision.GetComponentInParent<PlayerStatControl>();
        if (player != null)
        {
            OpenBox(player);
        }
    }

    private void OpenBox(PlayerStatControl player)
    {
        isOpened = true;

        if (sr != null && openedSprite != null)
        {
            sr.sprite = openedSprite;
        }

        // Push player back slightly
        Vector2 pushDir = (player.transform.position - transform.position).normalized;
        Rigidbody2D playerRb = player.GetComponent<Rigidbody2D>();
        if (playerRb != null)
        {
            playerRb.AddForce(pushDir * pushForce, ForceMode2D.Impulse);
        }
        else
        {
            // Fallback push
            player.transform.position += (Vector3)(pushDir * 1f);
        }

        StartCoroutine(SpawnRewardRoutine());
    }

    private IEnumerator SpawnRewardRoutine()
    {
        // Wait a short moment to let the player get pushed back
        yield return new WaitForSeconds(0.2f);

        if (isRareBox || forceDNA || Random.value > coinDropChance)
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
        if (resultDNAPrefab == null) return;

        GameObject dnaObj = Instantiate(resultDNAPrefab, transform.position + Vector3.up * 0.5f, Quaternion.identity);
        ResultDNA resultDna = dnaObj.GetComponent<ResultDNA>();
        if (resultDna != null)
        {
            // 일반 상자(isRareBox == false)에서도 5% 확률로 레어 DNA가 나올 수 있음
            bool spawnRareDna = isRareBox || (Random.value < 0.05f);
            resultDna.Init(spawnRareDna);
        }
    }

    private void SpawnCoins()
    {
        if (GameManager.Instance != null)
        {
            int coinAmount = Random.Range(3, 8); // 5 +- 2
            GameManager.Instance.SpawnCoins(transform.position, coinAmount);
        }
    }
}
