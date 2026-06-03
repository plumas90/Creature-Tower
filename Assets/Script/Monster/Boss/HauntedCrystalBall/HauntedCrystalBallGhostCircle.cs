using System.Collections;
using UnityEngine;

public class HauntedCrystalBallGhostCircle : MonoBehaviour
{
    private Vector2 centerPoint;
    private float radius;
    private float rotationSpeed;
    private float damage;
    private float currentAngle;
    private float startAngle;
    private float targetAngle;
    private bool isLeft; // 왼쪽 구인지 오른쪽 구인지
    private bool isRotating = false;
    private bool hasHit = false;

    [Header("Sprite Animation")]
    [SerializeField] private Sprite[] idleSprites;
    [SerializeField] private float animationFps = 10f;
    private SpriteRenderer spriteRenderer;
    private float animTimer;
    private int currentFrame;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    /// <summary>
    /// 회전구 초기화
    /// </summary>
    /// <param name="center">회전 중심 (보스 위치)</param>
    /// <param name="rad">회전 반지름</param>
    /// <param name="rotSpeed">회전 속도 (도/초)</param>
    /// <param name="dmg">데미지</param>
    /// <param name="left">왼쪽 구 여부</param>
    /// <param name="waitTime">회전 시작 전 대기 시간</param>
    public void Initialize(Vector2 center, float rad, float rotSpeed, float dmg, bool left, float waitTime)
    {
        centerPoint = center;
        radius = rad;
        rotationSpeed = rotSpeed;
        damage = dmg;
        isLeft = left;
        hasHit = false;

        // 시작 각도: 왼쪽 구는 180도(왼쪽), 오른쪽 구는 0도(오른쪽)
        startAngle = isLeft ? 180f : 0f;
        currentAngle = startAngle;

        // 목표 각도: 왼쪽 구는 0도(오른쪽 위치), 오른쪽 구는 180도(왼쪽 위치)
        targetAngle = isLeft ? 0f : 180f;

        // 시작 위치 설정
        UpdatePosition();

        if (idleSprites != null && idleSprites.Length > 0 && spriteRenderer != null)
        {
            currentFrame = 0;
            animTimer = 0f;
            spriteRenderer.sprite = idleSprites[0];
        }

        StartCoroutine(CoWaitAndRotate(waitTime));
    }

    private IEnumerator CoWaitAndRotate(float waitTime)
    {
        yield return new WaitForSeconds(waitTime);
        isRotating = true;
    }

    private void Update()
    {
        // 프레임 애니메이션 재생 (이동/회전 상태와 무관하게 항상 재생)
        if (idleSprites != null && idleSprites.Length > 0 && spriteRenderer != null)
        {
            animTimer += Time.deltaTime;
            float frameDelay = 1f / animationFps;
            if (animTimer >= frameDelay)
            {
                animTimer -= frameDelay;
                currentFrame = (currentFrame + 1) % idleSprites.Length;
                spriteRenderer.sprite = idleSprites[currentFrame];
            }
        }

        if (!isRotating || hasHit)
            return;

        // 시계 방향으로 회전 (각도 감소)
        float deltaAngle = rotationSpeed * Time.deltaTime;
        
        if (isLeft)
        {
            // 왼쪽 구: 180 → 0 (시계방향)
            currentAngle -= deltaAngle;
            if (currentAngle <= targetAngle)
            {
                currentAngle = targetAngle;
                Destroy(gameObject);
                return;
            }
        }
        else
        {
            // 오른쪽 구: 0 → 180 (시계방향, 아래로 돌아서 왼쪽으로)
            // 시계방향이므로 각도 증가가 아니라 감소 (-360도 방향)
            currentAngle -= deltaAngle;
            if (currentAngle <= -180f)
            {
                Destroy(gameObject);
                return;
            }
        }

        UpdatePosition();
    }

    private void UpdatePosition()
    {
        float radians = currentAngle * Mathf.Deg2Rad;
        float x = centerPoint.x + radius * Mathf.Cos(radians);
        float y = centerPoint.y + radius * Mathf.Sin(radians);
        transform.position = new Vector3(x, y, transform.position.z);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (hasHit)
            return;

        // 플레이어 충돌 체크
        PlayerStatControl playerStat = collision.GetComponent<PlayerStatControl>();
        if (playerStat == null)
            playerStat = collision.GetComponentInParent<PlayerStatControl>();

        if (playerStat != null)
        {
            // 플레이어에게 데미지
            playerStat.TryApplyContactDamage(damage, gameObject.GetInstanceID());
            hasHit = true;
            Destroy(gameObject);
        }
    }
}
