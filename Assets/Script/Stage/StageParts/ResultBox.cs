using System.Collections;
using UnityEngine;

public class ResultBox : MonoBehaviour
{
    public bool isRareBox = false;
    public bool forceDNA = false; // 보스방에서 확정 DNA용
    public float coinDropChance = 0.5f; // 일반 상자에서 코인이 나올 확률

    [Header("Spawns")]
    public GameObject childDNA; // 자식 오브젝트 (Square)

    [Header("Animation (Placeholders)")]
    public Sprite closedSprite;
    public Sprite openedSprite;
    public SpriteRenderer boxSpriteRenderer; // 자식 오브젝트 (Box)의 SpriteRenderer

    [Header("Push Back")]
    public float pushForce = 1.5f;

    private bool isOpened = false;

    public System.Action OnOpened;

    private void Awake()
    {
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

        StartCoroutine(SpawnRewardRoutine());
    }

    private IEnumerator SpawnRewardRoutine()
    {
        // Wait a short moment to let the player get pushed back and show open animation
        yield return new WaitForSeconds(0.5f);

        if (boxSpriteRenderer != null && openedSprite != null)
        {
            boxSpriteRenderer.sprite = openedSprite;
        }

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
        if (childDNA == null) return;

        ResultDNA resultDna = childDNA.GetComponent<ResultDNA>();
        if (resultDna != null)
        {
            // 일반 상자(isRareBox == false)에서도 5% 확률로 레어 DNA가 나올 수 있음
            bool spawnRareDna = isRareBox || (Random.value < 0.05f);
            resultDna.Init(spawnRareDna);
        }
        else
        {
            childDNA.SetActive(true);
        }
    }

    private void SpawnCoins()
    {
        if (GameManager.Instance != null)
        {
            // 5 ± 2원 (3~7원) 드랍
            int coinAmount = Random.Range(3, 8);
            GameManager.Instance.SpawnCoinsForAmount(transform.position, coinAmount);
        }
    }
}
