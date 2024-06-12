using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerSlot : MonoBehaviour
{
    // Start is called before the first frame update
    public PlayerSO playerSO;
    public RectTransform rectTransform;
    public Image character;
    public Image weapon;
    private void Awake()
    {
        rectTransform =this.GetComponent<RectTransform>();
    }
}
