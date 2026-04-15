using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChoiceSlot : MonoBehaviour//������ �񸣴� ���� ���� �������� �ش����� ���� �ҷ����� ������� �ش� ������ ȣ����
{
    public TextMeshProUGUI Name;
    public TextMeshProUGUI Info;
    public IAugment stat;
    public bool Ispick = false;
    public int listIndex;
    public ResultManager Parent;
    int rare;
    int symbolNum;

    public Sprite tier1;
    public Sprite tier2;
    public Sprite tier3;

    public Sprite symbolStat;
    public Sprite symbolAll;
    public Sprite symbolSniper;
    public Sprite symbolShotgun;
    public Sprite symbolsoldier;

    public Sprite symbolSniperOption;
    public Sprite symbolShotgunOption;
    public Sprite symbolsoldierOption;

    public Image bodyImage;
    public Image symbolImage;
    public Image symbolImageOption;
    public GameObject symbolOptionObj;
    private Color defaultBodyColor = Color.white;
    private bool bodyColorCached;
    //[Range(1, 3)] int StatType;

    private void OnEnable()// �̸�,����,�� ������Ʈ
    {
        if (stat == null)
        {
            Debug.LogWarning($"[ChoiceSlot] stat is null on '{name}'. Slot will be hidden.");
            gameObject.SetActive(false);
            return;
        }

        if (Name == null || Info == null)
        {
            Debug.LogWarning($"[ChoiceSlot] Text reference is missing on '{name}'.");
            return;
        }

        Name.text = stat.Name;
        Ispick = false;
        Info.text = stat.func;
        rare = stat.Rare;
        symbolNum = stat.Code / 1000;
        if (symbolNum == 0) 
        {
            if (stat.Code >= 900) 
            {
                symbolNum = 9;
            }
        }
        bodyImage = gameObject.GetComponent<Image>();
        if (!bodyColorCached && bodyImage != null)
        {
            defaultBodyColor = bodyImage.color;
            bodyColorCached = true;
        }
        SetSelected(false, defaultBodyColor);
        if (symbolOptionObj != null)
            symbolOptionObj.SetActive(true);

        switch (rare)
        {
            case 1:
                bodyImage.sprite = tier1;
                break;

            case 2:
                bodyImage.sprite = tier2;
                break;

            case 3:
                bodyImage.sprite = tier3;
                break;
        }
        switch (symbolNum)
        {
            case 0:
                symbolImage.sprite = symbolAll;
                if (symbolOptionObj != null)
                    symbolOptionObj.SetActive(false);
                break;
            case 1:
                symbolImage.sprite= symbolSniper;
                symbolImageOption.sprite = symbolSniperOption;
                break;

            case 2:
                symbolImage.sprite = symbolsoldier;
                symbolImageOption.sprite = symbolsoldierOption;
                break;

            case 3:
                symbolImage.sprite = symbolShotgun;
                symbolImageOption.sprite = symbolShotgunOption;
                break;
            case 9:
                symbolImage.sprite = symbolStat;
                if (symbolOptionObj != null)
                    symbolOptionObj.SetActive(false);
                break;
        }
    }
    public void pick()
    {
        if (stat == null)
            return;

        if (Parent != null)
        {
            Parent.OnChoiceSlotClicked(this);
            return;
        }

        int code = stat.Code;
        Debug.Log($"{code}");
        AugmentManager.Instance.AugmentCall(code);
        Ispick = true;
        if (ResultManager.Instance != null)
            ResultManager.Instance.close();
    }

    public void SetSelected(bool selected, Color selectedColor)
    {
        if (bodyImage == null)
            bodyImage = gameObject.GetComponent<Image>();
        if (bodyImage == null)
            return;

        Color baseColor = bodyColorCached ? defaultBodyColor : Color.white;
        bodyImage.color = selected ? selectedColor : baseColor;
    }
    
}
