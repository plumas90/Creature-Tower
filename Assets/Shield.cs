using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Shield : MonoBehaviour
{
    // Start is called before the first frame update
    public PlayerStatControl statControl;
    public float shieldTime = 0;

    public bool shieldOn =false;

    public SpriteRenderer shieldRenderer;
    void Start()
    {
        gameObject.SetActive(false);
        statControl = GetComponentInParent<PlayerStatControl>();
        shieldRenderer = GetComponent<SpriteRenderer>();
    }

    // Update is called once per frame
    void Update()
    {
        if (shieldOn) 
        {
            shieldTime -= Time.deltaTime;
            if (shieldTime <= 0) 
            {
                shiledOff();
            }
        }
    }
    public void shiledOn(float Hp , float Time) 
    {
        statControl.InShieldHP = Hp;
        shieldTime = Time;
        shieldOn = true;
        gameObject.SetActive(true);
    }
    public void shiledOff() 
    {
        statControl.InShieldHP = 0;
        shieldOn = false;
        gameObject.SetActive(false);
    }

    public void SetColor(Color color)
    {
        shieldRenderer.color = color;
    }
}
