using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public class CharPickUI : MonoBehaviour
{
    public Vector3 mainPickPosition = new Vector3 (0, 0, 0);
    //public Vector3 leftPickPosition = new Vector3(-225, 100, 0);
    //public Vector3 rightPickPosition = new Vector3(250, 100, 0);

    public Vector2 mainScale = new Vector2 (1, 1);
    //public Vector2 otherScale = new Vector2(0.75f, 0.75f);

    public PlayerSlot[] pickList = new PlayerSlot[] { };

    public Color mainColor = new Color(255, 255, 255, 255);
    public Color otherColor = new Color(255,255,255,180);

    // Start is called before the first frame update
    void Start()
    {


    }

    // Update is called once per frame

    public void LeftBtn()
    {
        Bubble();
        SetInfo();
        ColorSet();
    }
    public void RightBtn() 
    {
        BubbleReverse();
        SetInfo();
        ColorSet();
    }
    public void Bubble()
    {
        //좌로 밀기 앞으로 한칸
        PlayerSlot temp = pickList[0];
        Vector3 tempRectTransform = temp.rectTransform.anchoredPosition;
        Vector3 tempScale = temp.rectTransform.localScale;
        for (int i = 0; i < pickList.Length - 1; i++)
        {
            pickList[i].rectTransform.localScale = pickList[i + 1].rectTransform.localScale;
            pickList[i].rectTransform.anchoredPosition = pickList[i + 1].rectTransform.anchoredPosition;
            pickList[i] = pickList[i + 1];
        }
        pickList[pickList.Length - 1].rectTransform.anchoredPosition = tempRectTransform;
        pickList[pickList.Length - 1].rectTransform.localScale = tempScale;
        pickList[pickList.Length - 1] = temp;
    }
    public void ColorSet() 
    {
        Color color = pickList[0].character.color;
        color.a = 255f;
        pickList[0].character.color = color;

        Color color2 = pickList[0].weapon.color;
        color2.a = 255f;
        pickList[0].weapon.color = color2;

        //pickList[0].weapon.color = mainColor;


        for (int i = 1; i <= pickList.Length-1; ++i) 
        {
            Debug.Log("들어오긴해?");
            Color color3 = pickList[i].character.color;
            color3.a = 0.5f;
            pickList[i].character.color = color3;

            Color color4 = pickList[i].weapon.color;
            color4.a = 0.5f;
            pickList[i].weapon.color = color4;
            //pickList[i].character.color = otherColor;
            //pickList[i].weapon.color = otherColor;
        }
    }
    public void SetInfo() 
    {
        if (pickList[0].playerSO != null) 
        {
            PlayerSO CurSO = pickList[0].playerSO;
        }
    }
    public void BubbleReverse() 
    {
        //우로 밀기

       //뒤로한칸
        PlayerSlot temp = pickList[pickList.Length - 1];
        Vector3 tempRectTransform = temp.rectTransform.anchoredPosition;
        Vector3 tempScale = temp.rectTransform.localScale;

        for (int i = pickList.Length - 2; i >= 0; i--) 
        {
            pickList[i + 1].rectTransform.localScale = pickList[i].rectTransform.localScale;
            pickList[i + 1].rectTransform.anchoredPosition = pickList[i].rectTransform.anchoredPosition;
            pickList[i + 1] = pickList[i];
        }
        pickList[0].rectTransform.anchoredPosition = tempRectTransform;
        pickList[0].rectTransform.localScale = tempScale;
        pickList[0] = temp;


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
