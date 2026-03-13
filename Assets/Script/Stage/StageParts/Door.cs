using System.Collections;
using UnityEngine;

// BotDoor / TopDoor 오브젝트에 붙이는 컴포넌트
// 자식으로 LeftDoor(SpriteRenderer), RightDoor(SpriteRenderer) 오브젝트를 가짐
// 평상시: 플레이어가 openDistance 이내로 접근하면 열리고, closeDistance 초과 시 닫힘
// 보스전 중: Lock() 호출 시 강제 잠금 (근접 감지 비활성)
// 보스 처치 후: UnLock() 호출 시 강제 잠금 해제 (근접 감지 재활성)

public class Door : MonoBehaviour
{
    [Header("Door Parts")]
    public SpriteRenderer leftDoor;
    public SpriteRenderer rightDoor;

    [Header("Sprites (Choose 1 of 3)")]
    public Sprite[] leftDoorSprites;   // left_door_1, 2, 3
    public Sprite[] rightDoorSprites;  // right_door_1, 2, 3
    [Range(0, 2)] public int spriteIndex = 0; // 0=1번, 1=2번, 2=3번

    [Header("Slide Settings")]
    public float slideDistance = 1.5f;
    public float slideDuration = 0.4f;

    [Header("Proximity Settings")]
    public float openDistance  = 1.25f;  // 이 거리 이내면 자동 열림
    public float closeDistance = 1.75f;  // 이 거리 초과 시 자동 닫힘 (히스테리시스)

    private Vector3 leftClosed;
    private Vector3 rightClosed;
    private bool isOpen   = false;
    private bool isLocked = false;  // true = 근접 감지 비활성, 강제 닫힘 유지
    private bool isSliding = false;
    private Transform playerTransform;

    private void Awake()
    {
        if (leftDoorSprites != null && leftDoorSprites.Length > spriteIndex && leftDoorSprites[spriteIndex] != null)
            leftDoor.sprite = leftDoorSprites[spriteIndex];
        if (rightDoorSprites != null && rightDoorSprites.Length > spriteIndex && rightDoorSprites[spriteIndex] != null)
            rightDoor.sprite = rightDoorSprites[spriteIndex];

        leftClosed  = leftDoor.transform.localPosition;
        rightClosed = rightDoor.transform.localPosition;
    }

    private void Update()
    {
        if (isLocked) return;

        // 플레이어 레퍼런스 지연 취득 (씬 전환 또는 테스트씬 대응)
        if (playerTransform == null)
        {
            var psc = FindObjectOfType<PlayerStatControl>();
            if (psc != null) playerTransform = psc.transform;
            else return;
        }

        float dist = Vector2.Distance(transform.position, playerTransform.position);

        if (!isOpen && !isSliding && dist <= openDistance)
            StartCoroutine(SlideCoroutine(true));
        else if (isOpen && !isSliding && dist > closeDistance)
            StartCoroutine(SlideCoroutine(false));
    }

    /// <summary>보스 전투 시작 등 강제 잠금. 근접 감지 비활성화 + 문이 열려 있으면 닫음.</summary>
    public void Lock()
    {
        isLocked = true;
        if (isOpen && !isSliding)
            StartCoroutine(SlideCoroutine(false));
    }

    /// <summary>보스 처치 후 잠금 해제. 근접 감지 재활성화 (이후 Update가 자동으로 열어줌).</summary>
    public void UnLock()
    {
        isLocked = false;
        playerTransform = null; // 재탐색 강제 (캐릭터 재소환 대응)
    }

    private IEnumerator SlideCoroutine(bool opening)
    {
        isSliding = true;
        isOpen    = opening;

        Vector3 leftTarget  = opening ? leftClosed  + Vector3.left  * slideDistance : leftClosed;
        Vector3 rightTarget = opening ? rightClosed + Vector3.right * slideDistance : rightClosed;

        Vector3 leftStart  = leftDoor.transform.localPosition;
        Vector3 rightStart = rightDoor.transform.localPosition;

        float elapsed = 0f;
        while (elapsed < slideDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / slideDuration);
            leftDoor.transform.localPosition  = Vector3.Lerp(leftStart,  leftTarget,  t);
            rightDoor.transform.localPosition = Vector3.Lerp(rightStart, rightTarget, t);
            yield return null;
        }
        leftDoor.transform.localPosition  = leftTarget;
        rightDoor.transform.localPosition = rightTarget;

        isSliding = false;
    }
}

