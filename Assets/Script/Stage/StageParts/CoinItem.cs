using System.Collections;
using UnityEngine;

public class CoinItem : MonoBehaviour
{
    public int coinValue = 1;
    public float scatterForce = 3f;
    public float magnetRadius = 2f;
    public float magnetSpeed = 8f;
    
    private bool isCollected = false;
    private bool isScattering = true;
    private Rigidbody2D rb;
    private Transform playerTarget;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }
        rb.gravityScale = 0f;
        rb.linearDamping = 2f;
        rb.freezeRotation = true;
    }

    public void Init(int value)
    {
        coinValue = value;
        isCollected = false;
        isScattering = true;
        
        // Scatter in a random direction
        Vector2 randomDir = Random.insideUnitCircle.normalized;
        rb.AddForce(randomDir * scatterForce, ForceMode2D.Impulse);
        
        StartCoroutine(StopScattering());
    }

    private IEnumerator StopScattering()
    {
        yield return new WaitForSeconds(0.5f);
        isScattering = false;
        rb.linearVelocity = Vector2.zero;
    }

    private void Update()
    {
        if (isCollected) return;

        if (!isScattering && playerTarget == null)
        {
            FindPlayerInRadius();
        }

        if (playerTarget != null)
        {
            transform.position = Vector3.MoveTowards(transform.position, playerTarget.position, magnetSpeed * Time.deltaTime);
        }
    }

    private void FindPlayerInRadius()
    {
        Collider2D[] cols = Physics2D.OverlapCircleAll(transform.position, magnetRadius);
        for (int i = 0; i < cols.Length; i++)
        {
            if (cols[i].GetComponentInParent<PlayerStatControl>() != null)
            {
                playerTarget = cols[i].transform;
                break;
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isCollected) return;

        PlayerStatControl player = collision.GetComponentInParent<PlayerStatControl>();
        if (player != null)
        {
            isCollected = true;
            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddGold(coinValue);
            }
            Destroy(gameObject);
        }
    }
}
