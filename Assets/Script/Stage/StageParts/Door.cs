using System.Collections;
using UnityEngine;

// BotDoor / TopDoor 오브젝트에 붙이는 컴포넌트
// 자식으로 LeftDoor(SpriteRenderer), RightDoor(SpriteRenderer) 오브젝트를 가짐
// left_door_1/2/3 중 하나, right_door_1/2/3 중 하나를 Inspector에서 지정
// 문 열림: LeftDoor는 왼쪽, RightDoor는 오른쪽으로 slideDistance만큼 이동
// 문 닫힘: 원래 위치로 복귀

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
    public float slideDistance = 1.5f;  // 열릴 때 좌우로 밀리는 거리
    public float slideDuration = 0.4f;

    private Vector3 leftClosed;
    private Vector3 rightClosed;
    private bool isOpen = false;

    private void Awake()
    {
        // 스프라이트 설정
        if (leftDoorSprites != null && leftDoorSprites.Length > spriteIndex && leftDoorSprites[spriteIndex] != null)
            leftDoor.sprite = leftDoorSprites[spriteIndex];
        if (rightDoorSprites != null && rightDoorSprites.Length > spriteIndex && rightDoorSprites[spriteIndex] != null)
            rightDoor.sprite = rightDoorSprites[spriteIndex];

        // 닫힌 상태 위치 기억
        leftClosed  = leftDoor.transform.localPosition;
        rightClosed = rightDoor.transform.localPosition;
    }

    public void Lock()
    {
        if (isOpen)
            StartCoroutine(SlideCoroutine(false));
    }

    public void UnLock()
    {
        if (!isOpen)
            StartCoroutine(SlideCoroutine(true));
    }

    private IEnumerator SlideCoroutine(bool opening)
    {
        isOpen = opening;

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
    }
}

