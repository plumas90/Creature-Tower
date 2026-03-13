using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Localization.Settings;

public class OptionUI : MonoBehaviour
{
    public static OptionUI Instance;
    bool IsOpen;
    // Start is called before the first frame update
    public void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            GameObject root = transform.root != null ? transform.root.gameObject : gameObject;
            DontDestroyOnLoad(root);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    void Start()
    {
        CloseOptionUI();
        IsOpen = false;
    }

    public void OnEscUI(InputValue value)
    {
        if (IsOpen)
            CloseOptionUI();
        else
            OpenOptionUI();
    }

    public void OpenOptionUI() 
    {
        gameObject.SetActive(true);
        IsOpen=true;
    }
    public void CloseOptionUI() 
    {
        gameObject.SetActive(false);
        IsOpen=false;
    }
    public void ChangeLanguageEn() 
    {
        LocalizationSettings.SelectedLocale =
        LocalizationSettings.AvailableLocales.Locales[0];
    }
    public void ChangeLanguageKr()
    {
        LocalizationSettings.SelectedLocale =
        LocalizationSettings.AvailableLocales.Locales[1];
    }

}
