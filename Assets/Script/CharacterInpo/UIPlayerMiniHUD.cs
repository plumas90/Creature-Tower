using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UIPlayerMiniHUD : MonoBehaviour
{
    [SerializeField] private List<GameObject> elements;
    [SerializeField] private GameObject player;
    private PlayerStatControl statHandler;
    private TopDownCharacterController playerController;

    public GameObject Player
    {
        get { return player; }
    }

    // Start is called before the first frame update
    void Start()
    {
       InitializeData();
    }

    public void InitializeData()
    {

        foreach (var element in elements)
        {
            var temp = element.GetComponent<UIBase>();
            if (temp != null)
            {
                //Debug.Log("[CheckInterface] Init " + element.GetType());
                temp.Initialize();
            }
        }

        playerController = player.GetComponent<TopDownCharacterController>();
        statHandler = player.GetComponent<PlayerStatControl>();

        OpenChild<UIPlayerMiniHP>();
        InitializeEvent();
    }

    public void InitializeEvent()
    {
        playerController.OnReloadEvent += OpenChild<UIReloadHUD>;
    }

    public void OpenChild<T>() where T : UIBase
    {
        var new_elements = player.GetComponentInChildren<UIPlayerMiniHUD>().elements;
        foreach (var element in new_elements)
        {
            //Debug.Log("[CheckInterface] " + element.GetType().Name + "/" + element.GetComponentInChildren<T>().name);
            T temp = element.GetComponent<T>();
            if (temp == null)
                element.SetActive(false);
            else
                temp.Open();
        }
    }
}
