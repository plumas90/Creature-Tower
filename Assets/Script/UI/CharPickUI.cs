using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public class CharPickUI : MonoBehaviour
{
    public TextMeshProUGUI NameText;
    public TextMeshProUGUI hpText;
    public TextMeshProUGUI atkText;
    public TextMeshProUGUI speedText;
    public TextMeshProUGUI atkspeedText;
    public TextMeshProUGUI reloadTimeText;
    public TextMeshProUGUI rollTimeText;
    public TextMeshProUGUI skillInfoText;

    public int charNum = 0;
    public Vector3 mainPickPosition = new Vector3 (0, 0, 0);
    //public Vector3 leftPickPosition = new Vector3(-225, 100, 0);
    //public Vector3 rightPickPosition = new Vector3(250, 100, 0);

    public Vector2 mainScale = new Vector2 (1, 1);
    //public Vector2 otherScale = new Vector2(0.75f, 0.75f);

    public PlayerSlot[] pickList = new PlayerSlot[] { };
    private void Awake()
    {
        PlayerSO playerSO = pickList[0].playerSO;
        TextSet(playerSO);
    }

    public void TextSet(PlayerSO playerso) 
    {
        NameText.text = playerso.CharaterName;
        hpText.text = playerso.hp.ToString();
        atkText.text = playerso.atk.ToString();
        speedText.text = playerso.unitSpeed.ToString();
        atkspeedText.text = playerso.atkSpeed.ToString();
        reloadTimeText.text = playerso.reloadCoolTime.ToString();
        rollTimeText.text = playerso.rollCoolTime.ToString();
        //skillInfoText.text = 
    }

    public void LeftBtn()
    {

        Bubble();
        ColorSet();
        TextSet(pickList[0].playerSO);
    }
    public void RightBtn() 
    {
        BubbleReverse();
        ColorSet();
        TextSet(pickList[0].playerSO);
    }

    public void Bubble()
    {


        //좌로 밀기
        Vector3[] localScales = new Vector3[pickList.Length];
        Vector3[] anchoredPositions = new Vector3[pickList.Length];
        for (int i = 0; i <= pickList.Length - 1; i++)
        {
            localScales[i] = pickList[i].rectTransform.localScale;
            anchoredPositions[i] = pickList[i].rectTransform.anchoredPosition;
        }

            //앞으로한칸
        PlayerSlot temp = pickList[pickList.Length - 1];
        Vector3 tempRectTransform = pickList[0].rectTransform.anchoredPosition;
        Vector3 tempScale = pickList[0].rectTransform.localScale;

        for (int i = pickList.Length - 2; i >= 0; i--)
        {pickList[i + 1] = pickList[i];}

        pickList[0].rectTransform.anchoredPosition = tempRectTransform;
        pickList[0].rectTransform.localScale = tempScale;
        pickList[0] = temp;

        for (int i = 0; i <= pickList.Length-1; i++)
        {
            pickList[i].rectTransform.localScale = localScales[i];
            pickList[i].rectTransform.anchoredPosition = anchoredPositions[i];
        }
    }

    public void ColorSet() 
    {

        for (int i = 0; i <= pickList.Length-1; ++i) 
        {
            Color color3 = pickList[i].character.color;
            color3.a = 0.5f;
            pickList[i].character.color = color3;

            Color color4 = pickList[i].weapon.color;
            color4.a = 0.5f;
            pickList[i].weapon.color = color4;
        }


        Color color = pickList[0].character.color;
        color.a = 255f;
        pickList[0].character.color = color;

        Color color2 = pickList[0].weapon.color;
        color2.a = 255f;
        pickList[0].weapon.color = color2;

    }
    public void pick() 
    {
        if (pickList[0].playerSO != null) 
        {
            PlayerSO CurSO = pickList[0].playerSO;
        }
    }
    public void BubbleReverse()
    {
        Vector3[] localScales = new Vector3[pickList.Length];
        Vector3[] anchoredPositions = new Vector3[pickList.Length];
        for (int i = 0; i < pickList.Length; i++)
        {
            localScales[i] = pickList[i].rectTransform.localScale;
            anchoredPositions[i] = pickList[i].rectTransform.anchoredPosition;
        }
        Debug.Log(localScales.Length);

        //우로 밀기 뒤로 한칸
        PlayerSlot temp = pickList[0];
        Vector3 tempRectTransform = pickList[pickList.Length - 1].rectTransform.anchoredPosition;
        Vector3 tempScale = pickList[pickList.Length - 1].rectTransform.localScale;
        for (int i = 0; i < pickList.Length - 1; i++)
        {
            pickList[i] = pickList[i + 1];
        }
        pickList[pickList.Length - 1].rectTransform.anchoredPosition = tempRectTransform;
        pickList[pickList.Length - 1].rectTransform.localScale = tempScale;
        pickList[pickList.Length - 1] = temp;

        for (int i = 0; i < pickList.Length; i++)
        {
            pickList[i].rectTransform.localScale = localScales[i];
            pickList[i].rectTransform.anchoredPosition = anchoredPositions[i];
        }

    }
    
    public void OnPickUi() 
    {
        this.gameObject.SetActive(true);
    }
    public void OffPickUi() 
    {
        this.gameObject.SetActive(false);
    }
}
