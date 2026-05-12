using System.Collections;
using UnityEngine;

public class CoinItem : MonoBehaviour
{
    public enum CoinType
    {
        Won1  = 1,
        Won5  = 5,
        Won10 = 10
    }

    [Header("Coin Sprites")]
    public Sprite coin1Sprite;  // 1원 스프라이트
    public Sprite coin5Sprite;  // 5원 스프라이트
    public Sprite coin10Sprite; // 10원 스프라이트

    [Header("Coin Settings")]
    public CoinType coinType = CoinType.Won1;
    public float scatterForce = 3f;
    public float magnetRadius = 2f;
    public float magnetSpeed = 8f;

    // 코인 타입별 색상
    private static readonly Color Color1Won  = new Color(0.72f, 0.45f, 0.20f); // 구리색  (1원)
    private static readonly Color Color5Won  = new Color(0.75f, 0.75f, 0.75f); // 은색    (5원)
    private static readonly Color Color10Won = new Color(0.95f, 0.80f, 0.20f); // 금색   (10원)

    // 코인 타입별 스케일 배율
    private static readonly float Scale1Won  = 0.5f;
    private static readonly float Scale5Won  = 0.65f;
    private static readonly float Scale10Won = 0.85f;

    public int coinValue => (int)coinType;

    private bool isCollected = false;
    private bool isScattering = true;
    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private Transform playerTarget;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
            rb = gameObject.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.linearDamping = 2f;
        rb.freezeRotation = true;

        sr = GetComponent<SpriteRenderer>();
        if (sr == null)
            sr = gameObject.AddComponent<SpriteRenderer>();

        // 바닥 SortingLayer 설정 (World_GroundFX = 가장 하단 동적 레이어)
        sr.sortingLayerName = "World_GroundFX";
        sr.sortingOrder = 0;
    }

    /// <summary>
    /// 코인 타입으로 초기화. GameManager.SpawnCoinsForAmount() 에서 호출한다.
    /// </summary>
    public void Init(CoinType type)
    {
        coinType = type;
        isCollected = false;
        isScattering = true;

        ApplyVisuals();

        // 랜덤 방향으로 튀어나가기
        // 금액이 클수록 조금 더 멀리 흩어짐 (비례 연출)
        float forceMult = 1f + ((int)type - 1) * 0.04f; // 1원=1.0, 5원=1.16, 10원=1.36
        Vector2 randomDir = Random.insideUnitCircle.normalized;
        rb.AddForce(randomDir * scatterForce * forceMult, ForceMode2D.Impulse);

        StartCoroutine(StopScattering());
    }

    /// <summary>
    /// 레거시 호환용 — int 값으로 가장 가까운 코인 타입을 추정한다.
    /// </summary>
    public void Init(int value)
    {
        CoinType t = value >= 10 ? CoinType.Won10
                   : value >= 5  ? CoinType.Won5
                   : CoinType.Won1;
        Init(t);
    }

    private void ApplyVisuals()
    {
        Color tint;
        float scale;
        Sprite sprite;

        switch (coinType)
        {
            case CoinType.Won10:
                tint   = Color10Won;
                scale  = Scale10Won;
                sprite = coin10Sprite;
                break;
            case CoinType.Won5:
                tint   = Color5Won;
                scale  = Scale5Won;
                sprite = coin5Sprite;
                break;
            default: // Won1
                tint   = Color1Won;
                scale  = Scale1Won;
                sprite = coin1Sprite;
                break;
        }

        if (sr != null)
        {
            // 스프라이트가 할당돼 있으면 적용, 없으면 색상 틴트로 폴백
            if (sprite != null)
            {
                sr.sprite = sprite;
                sr.color  = Color.white; // 스프라이트 자체 색상 유지
            }
            else
            {
                sr.color = tint;
            }
        }

        transform.localScale = Vector3.one * scale;
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
            FindPlayerInRadius();

        if (playerTarget != null)
            transform.position = Vector3.MoveTowards(transform.position, playerTarget.position, magnetSpeed * Time.deltaTime);
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
                GameManager.Instance.AddGold(coinValue);
            Destroy(gameObject);
        }
    }
}
