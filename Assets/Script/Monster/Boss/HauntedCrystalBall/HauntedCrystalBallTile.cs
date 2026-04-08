using System.Collections;
using UnityEngine;

public class HauntedCrystalBallTile : MonoBehaviour
{
    private float damage;
    private float warningTime;
    private float activeTime;
    private bool isActive = false;
    private SpriteRenderer spriteRenderer;

    [Header("Sprite Settings")]
    [Tooltip("경고 상태 스프라이트 (노란색 등)")]
    public Sprite warningSprite;

    [Tooltip("활성 상태 스프라이트 (빨간색 등)")]
    public Sprite activeSprite;

    [Tooltip("경고 상태 색상")]
    public Color warningColor = new Color(1f, 1f, 0f, 0.5f); // 노란색 반투명

    [Tooltip("활성 상태 색상")]
    public Color activeColor = new Color(1f, 0f, 0f, 1f); // 빨간색 불투명

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
    }

    public void Initialize(float dmg, float warnTime, float actTime)
    {
        damage = dmg;
        warningTime = warnTime;
        activeTime = actTime;
        isActive = false;

        // 초기 경고 상태 설정
        if (spriteRenderer != null)
        {
            if (warningSprite != null)
                spriteRenderer.sprite = warningSprite;
            spriteRenderer.color = warningColor;
        }

        StartCoroutine(CoTileSequence());
    }

    private IEnumerator CoTileSequence()
    {
        // 1. 경고 시간 대기
        yield return new WaitForSeconds(warningTime);

        // 2. 활성화 (스프라이트 변경 + 데미지 활성)
        isActive = true;
        if (spriteRenderer != null)
        {
            if (activeSprite != null)
                spriteRenderer.sprite = activeSprite;
            spriteRenderer.color = activeColor;
        }

        Debug.Log($"[HauntedCrystalBallTile] Tile activated at {transform.position}");

        // 3. 활성 시간 대기
        yield return new WaitForSeconds(activeTime);

        // 4. 사라짐
        Destroy(gameObject);
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        // 활성 상태일 때만 데미지
        if (!isActive)
            return;

        PlayerStatControl playerStat = collision.GetComponent<PlayerStatControl>();
        if (playerStat == null)
            playerStat = collision.GetComponentInParent<PlayerStatControl>();

        if (playerStat != null)
        {
            // 플레이어에게 데미지 (지속 접촉)
            playerStat.TryApplyContactDamage(damage, gameObject.GetInstanceID());
        }
    }
}
