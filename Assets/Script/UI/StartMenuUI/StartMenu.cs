using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StartMenu : MonoBehaviour
{
    public OptionUI OptionUi;
    public GameObject credit;
    
    [Header("UI References")]
    [Tooltip("StartMenu UI GameObject (게임 시작 시 비활성화됨)")]
    public GameObject startMenuUI;

    public void StartBtn() 
    {
        // StartMenu UI 비활성화
        if (startMenuUI != null)
        {
            startMenuUI.SetActive(false);
            Debug.Log("[StartMenu] StartMenu UI deactivated");
        }
        
        // MainScene 로드 (CharPickUI는 MainScene에서 자동 활성화됨)
        SceneManager.sceneLoaded += OnMainSceneLoaded;
        SceneManager.LoadScene("MainScene");
    }
    
    private void OnMainSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "MainScene")
        {
            SceneManager.sceneLoaded -= OnMainSceneLoaded;
            
            // MainScene에서 CharPickUI 찾아서 활성화
            CharPickUI charPickUI = FindFirstObjectByType<CharPickUI>(FindObjectsInactive.Include);
            if (charPickUI != null)
            {
                charPickUI.gameObject.SetActive(true);
                Debug.Log("[StartMenu] CharPickUI activated in MainScene");
            }
            else
            {
                Debug.LogWarning("[StartMenu] CharPickUI not found in MainScene!");
            }
        }
    }

    public void OptionBtn() 
    {
        OptionUi.OpenOptionUI();
    }

    public void CreditBtn() 
    {
        //to do 완성 시키기
    }
    
    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnMainSceneLoaded;
    }
}
