using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using UnityEngine;

public class MainCamera : MonoBehaviour
{
    public GameObject Target;               // ī�޶� ����ٴ� Ÿ��

    public float offsetX = 0.0f;            // ī�޶��� x��ǥ
    public float offsetY = 0.0f;            // ī�޶��� y��ǥ
    public float offsetZ = -10.0f;          // ī�޶��� z��ǥ

    public float CameraSpeed = 10.0f;       // ī�޶��� �ӵ�
    Vector3 TargetPos;                      // Ÿ���� ��ġ
    Vector3 OtherTargetPos;
    int currentOtherTargetViewID;

    private void Start()
    {
        SetInitialTarget();
    }

    private void SetInitialTarget()
    {
        // GameManager가 이미 playerOBJ를 가지고 있으면 자동 할당
        if (Target == null && GameManager.Instance != null && GameManager.Instance.playerOBJ != null)
        {
            Target = GameManager.Instance.playerOBJ;
            Debug.Log("[MainCamera] Auto-assigned target from GameManager.playerOBJ on Start");
        }
    }


    public void Update()
    {
        if (Target == null)
        {
            // Target이 없으면 카메라 이동 안함
            return;
        }

        TargetPos = new Vector3(
            Target.transform.position.x + offsetX,
            Target.transform.position.y + offsetY,
            Target.transform.position.z + offsetZ
        );
        transform.position = Vector3.Lerp(transform.position, TargetPos, Time.deltaTime * CameraSpeed);
    }


    public void FocusOnPlayerInstant()
    {
        if (Target == null || (GameManager.Instance != null && Target != GameManager.Instance.playerOBJ))
        {
            SetInitialTarget();
        }

        if (Target != null)
        {
            transform.position = new Vector3(
                Target.transform.position.x + offsetX,
                Target.transform.position.y + offsetY,
                offsetZ
            );
        }
    }

    public void FocusOnTargetInstant(GameObject customTarget)
    {
        if (customTarget != null)
        {
            transform.position = new Vector3(
                customTarget.transform.position.x + offsetX,
                customTarget.transform.position.y + offsetY,
                offsetZ
            );
        }
    }

    public void StartBossIntroTracking(GameObject bossObj, float introDuration)
    {
        StartCoroutine(CoBossIntroSequence(bossObj, introDuration));
    }

    private IEnumerator CoBossIntroSequence(GameObject bossObj, float introDuration)
    {
        // 1. 플레이어 입력 비활성화
        PlayerInputController playerInput = null;
        if (GameManager.Instance != null && GameManager.Instance.playerOBJ != null)
        {
            playerInput = GameManager.Instance.playerOBJ.GetComponent<PlayerInputController>();
            if (playerInput != null)
            {
                playerInput.InputOff();
            }
        }

        // 플레이어 렌더러 일시 비활성화 (보이지 않게 처리)
        SpriteRenderer[] playerRenderers = null;
        if (GameManager.Instance != null && GameManager.Instance.playerOBJ != null)
        {
            playerRenderers = GameManager.Instance.playerOBJ.GetComponentsInChildren<SpriteRenderer>(true);
            foreach (var r in playerRenderers)
            {
                r.enabled = false;
            }
        }

        // 2. UI 숨기기
        if (GameManager.Instance != null && GameManager.Instance.playerUiManager != null)
        {
            GameManager.Instance.playerUiManager.gameObject.SetActive(false);
        }

        // 3. 거대 검은색 배경 오브젝트 생성 (카메라 자식으로 두어 화면 전체를 빈틈없이 100% 불투명 검정으로 덮음)
        GameObject blackBg = new GameObject("IntroBlackBG");
        blackBg.transform.SetParent(this.transform, false);
        blackBg.transform.localPosition = new Vector3(0f, 0f, 1f); // 카메라(-10) 바로 앞(Z=-9)에 완벽 밀착 배치
        SpriteRenderer bgSr = blackBg.AddComponent<SpriteRenderer>();
        
        Texture2D blackTex = new Texture2D(1, 1);
        blackTex.SetPixel(0, 0, Color.black);
        blackTex.Apply();
        Sprite blackSprite = Sprite.Create(blackTex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f); // Pixels Per Unit을 1f로 명시하여 1x1 유닛 크기 확보
        bgSr.sprite = blackSprite;
        bgSr.color = Color.black; // 100% 완전 불투명 순수 검정색
        
        // 카메라 해상도 및 aspect ratio (종횡비)에 정확히 비례해서 꽉 채우도록 설정
        Camera cam = GetComponent<Camera>();
        if (cam != null)
        {
            float height = cam.orthographicSize * 2f;
            float width = height * cam.aspect;
            blackBg.transform.localScale = new Vector3(width * 2f, height * 2f, 1f); // 여유 마진 2배로 확장
        }
        else
        {
            blackBg.transform.localScale = new Vector3(1000f, 1000f, 1f);
        }
        
        bgSr.sortingOrder = 500; // 플레이어/배경 맵(보통 0~100)보다 높게

        // 4. 보스의 소팅 오더를 일시적으로 극도로 올려서 검은색 배경 위로 보이기
        Dictionary<SpriteRenderer, int> originalOrders = new Dictionary<SpriteRenderer, int>();
        if (bossObj != null)
        {
            SpriteRenderer[] renderers = bossObj.GetComponentsInChildren<SpriteRenderer>(true);
            foreach (var r in renderers)
            {
                originalOrders[r] = r.sortingOrder;
                r.sortingOrder += 1000; // 검은색 배경(500)보다 훨씬 위로 올림
            }
        }

        // 5. 카메라 타겟을 보스로 임시 변경 및 즉시 이동
        GameObject originalTarget = Target;
        if (bossObj != null)
        {
            Target = bossObj;
            FocusOnTargetInstant(bossObj);
        }

        // 6. 인트로 타임 대기
        yield return new WaitForSeconds(introDuration);

        // 7. 보스의 소팅 오더 복원
        foreach (var kvp in originalOrders)
        {
            if (kvp.Key != null)
            {
                kvp.Key.sortingOrder = kvp.Value;
            }
        }

        // 플레이어 렌더러 복원
        if (playerRenderers != null)
        {
            foreach (var r in playerRenderers)
            {
                if (r != null) r.enabled = true;
            }
        }

        // 8. 검은색 배경 제거
        if (blackBg != null)
        {
            Destroy(blackBg);
        }

        // 9. UI 복원
        if (GameManager.Instance != null && GameManager.Instance.playerUiManager != null)
        {
            GameManager.Instance.playerUiManager.gameObject.SetActive(true);
        }

        // 10. 카메라 타겟 원상 복구 및 플레이어 입력 재활성화
        Target = originalTarget;
        if (playerInput != null)
        {
            playerInput.InputOn();
        }
        FocusOnPlayerInstant();
    }

    // ī޶  ,  , 2 , Ÿٸ ٲ

    /*public void ChangeTarget()
    {
        var playerInfoDictionary = GameManager.Instance.playerInfoDictionary; //ӸŴ ÷̾  ųʸ ޾ƿ

        // Ÿ  Ʈ
        if (Input.GetKeyDown(KeyCode.Q)) // Ű  
        {
            bool foundNewTarget = false;
            foreach (var viewID in playerInfoDictionary.Keys)
            {
                if (viewID != GameManager.Instance.clientPlayer.gameObject.GetPhotonView().ViewID // ƴϰų,   ִ Ÿ ƴ 쿡 ۵
                    && viewID != currentOtherTargetViewID)
                {
                    OtherTargetPos = new Vector3(playerInfoDictionary[viewID].position.x, playerInfoDictionary[viewID].position.y, offsetZ);
                    currentOtherTargetViewID = viewID;
                    foundNewTarget = true;
                    break; // ù ° ٸ ÷̾ ϵ 
                }
            }

            // Q Է  ٸ ÷̾ ã   ʱ Ÿ
            if (!foundNewTarget)
            {
                SetInitialTarget();
            }
        }

        //ƹ Էµ ° OtherTargetPos  Ÿ ؼ Ʈ
        if (currentOtherTargetViewID != null)
            OtherTargetPos = new Vector3(playerInfoDictionary[currentOtherTargetViewID].position.x, playerInfoDictionary[currentOtherTargetViewID].position.y, offsetZ);
    }
    */
}