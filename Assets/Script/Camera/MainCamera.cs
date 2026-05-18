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

        // 2. 카메라 타겟을 보스로 임시 변경 및 즉시 이동
        GameObject originalTarget = Target;
        Target = bossObj;
        FocusOnTargetInstant(bossObj);

        // 3. 인트로 타임 대기
        yield return new WaitForSeconds(introDuration);

        // 4. 카메라 타겟 원상 복구 및 플레이어 입력 재활성화
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