using System.Collections.Generic;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [SerializeField] private List<UIBase> layers;

    public List<UIBase> Layer
    {
        get { return layers; }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        Initialize();
    }

    private void Initialize()
    {
        if (layers == null)
            return;

        foreach (var layer in layers)
        {
            if (layer == null)
                continue;

            layer.Initialize();
            layer.Close();
        }
    }

    public void OpenOne<T>() where T : UIBase
    {
        if (layers == null)
            return;

        foreach (var layer in layers)
        {
            if (layer == null)
                continue;

            T temp = layer.GetComponent<T>();
            if (temp == null)
                layer.Close();
            else
                temp.Open();
        }
    }

    public void Open<T>() where T : UIBase
    {
        if (layers == null)
            return;

        foreach (var layer in layers)
        {
            if (layer == null)
                continue;

            T temp = layer.GetComponent<T>();
            if (temp != null)
                layer.Open();
        }
    }

    public void Close<T>() where T : UIBase
    {
        if (layers == null)
            return;

        foreach (var layer in layers)
        {
            if (layer == null)
                continue;

            T temp = layer.GetComponent<T>();
            if (temp != null)
                layer.Close();
        }
    }

    public void OpenMainGameUI()
    {
        if (layers == null)
            return;

        foreach (var layer in layers)
            if (layer != null && layer.GetComponent<UIMainGame>() != null)
                layer.Open();
    }

    public void CloseMainGameUI()
    {
        if (layers == null)
            return;

        foreach (var layer in layers)
            if (layer != null && layer.GetComponent<UIMainGame>() != null)
                layer.Close();
    }

    public T GetUIComponent<T>() where T : MonoBehaviour
    {
        Debug.Log("[UIManager] Find Start: " + typeof(T));

        if (layers == null)
            return null;

        foreach (var layer in layers)
        {
            if (layer == null)
                continue;

            T component = layer.GetComponent<T>();
            if (component != null)
            {
                Debug.Log("[UIManager] Find Success: " + typeof(T));
                return component;
            }
        }

        Debug.Log("[UIManager] Find Fail: " + typeof(T));
        return null;
    }

    public GameObject GetUIObject(string name)
    {
        Debug.Log("[UIManager] Find Start: " + name);

        if (layers == null)
            return null;

        foreach (var layer in layers)
        {
            if (layer == null)
                continue;

            if (layer.name == name)
            {
                Debug.Log("[UIManager] Find Success: " + name);
                return layer.gameObject;
            }
        }

        Debug.Log("[UIManager] Find Fail. Check parameter. " + name);
        return null;
    }
}
