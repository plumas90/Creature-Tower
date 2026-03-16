using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Localization.Components;
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

    private int charNum = 0;
    private Vector3 mainPickPosition = new Vector3 (0, 0, 0);
    //public Vector3 leftPickPosition = new Vector3(-225, 100, 0);
    //public Vector3 rightPickPosition = new Vector3(250, 100, 0);

    private Vector2 mainScale = new Vector2 (1, 1);
    //public Vector2 otherScale = new Vector2(0.75f, 0.75f);


    private PlayerSO _playerso;

    public PlayerSlot[] pickList = new PlayerSlot[] { };

    public GameObject playerTV;
    public GameObject playerCharlie;
    public GameObject playerKimKilWhan;

    public LocalizeStringEvent skillinfo;

    public void LocalizeTextString(string localcode)
    {
        //GetComponent<LocalizeStringEvent>().StringReference
        skillinfo.StringReference
            .SetReference("TABLE", localcode);
    }
    private void Awake()
    {
        PlayerSOSet();
        TextSet();
    }
    public void PlayerSOSet()
    {
        _playerso = pickList[0].playerSO;
    }

    public void TextSet() 
    {
        NameText.text = _playerso.CharaterName;
        hpText.text = _playerso.hp.ToString();
        atkText.text = _playerso.atk.ToString();
        speedText.text = _playerso.unitSpeed.ToString();
        atkspeedText.text = _playerso.atkSpeed.ToString();
        reloadTimeText.text = _playerso.reloadCoolTime.ToString();
        rollTimeText.text = _playerso.rollCoolTime.ToString();
        //skillInfoText.text = 
        switch (_playerso.CharacterClass) 
        {
            case (int)ClassName.TV:
                LocalizeTextString("SkillTV");
                break;

            case (int)ClassName.Charlie:
                LocalizeTextString("SkillCharlie");
                break;

            case (int)ClassName.KimKilWhan:
                LocalizeTextString("SkillKim");
                break;

        }

    }

    public void LeftBtn()
    {
        Bubble();
        ColorSet();
        PlayerSOSet();
        TextSet();
    }
    public void RightBtn() 
    {
        BubbleReverse();
        ColorSet();
        PlayerSOSet();
        TextSet();
    }

    public void Bubble()
    {


        //�·� �б�
        Vector3[] localScales = new Vector3[pickList.Length];
        Vector3[] anchoredPositions = new Vector3[pickList.Length];
        for (int i = 0; i <= pickList.Length - 1; i++)
        {
            localScales[i] = pickList[i].rectTransform.localScale;
            anchoredPositions[i] = pickList[i].rectTransform.anchoredPosition;
        }

            //��������ĭ
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

        //��� �б� �ڷ� ��ĭ
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
    public void PickButton() 
    {
        // 현재 중앙 슬롯 기준으로 최종 선택 캐릭터를 다시 확정한다.
        PlayerSOSet();
        if (_playerso == null)
            return;

        GameObject selectedPlayer = null;

        switch (_playerso.CharacterClass) 
        {
            case (int)ClassName.TV:
                selectedPlayer = playerTV;
                if (playerTV != null) playerTV.SetActive(true);
                if (playerCharlie != null) Destroy(playerCharlie);
                if (playerKimKilWhan != null) Destroy(playerKimKilWhan);
                break;

            case (int)ClassName.Charlie:
                selectedPlayer = playerCharlie;
                if (playerCharlie != null) playerCharlie.SetActive(true);
                if (playerTV != null) Destroy(playerTV);
                if (playerKimKilWhan != null) Destroy(playerKimKilWhan);
                break;

            case (int)ClassName.KimKilWhan:
                selectedPlayer = playerKimKilWhan;
                if (playerKimKilWhan != null) playerKimKilWhan.SetActive(true);
                if (playerTV != null) Destroy(playerTV);
                if (playerCharlie != null) Destroy(playerCharlie);
                break;

        }

        if (selectedPlayer != null && GameManager.Instance != null)
        {
            GameManager.Instance.Init(selectedPlayer);
        }

        // 메인 씬/테스트 씬 모두 선택 완료 즉시 캐릭터 선택 UI를 닫는다.
        OffPickUi();
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
