using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StartMenu : MonoBehaviour
{
    public OptionUI OptionUi;
    public GameObject credit;

    public void StartBtn() 
    {
        SceneManager.LoadScene("MainScene");
    }

    public void OptionBtn() 
    {
        OptionUi.OpenOptionUI();
    }

    public void CreditBtn() 
    {
        //to do 완성 시키기
    }
        
}
