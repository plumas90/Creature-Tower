using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using UnityEngine;

public class PlayerDebuffControl : MonoBehaviour
{
    public ParticleSystem _speedParticle;
    public ParticleSystem _TwoMoonParticle;
    public ParticleSystem _HealParticle;

    float speedTime = 0;
    float checkSpeedTime;

    float twoMoonTIME = 0;
    float checkMoonTime;

    float HealTime = 0;
    float checkHealTime;

    bool readyHeal;
    bool readyMoon;
    bool readySpeed;
    // Start is called before the first frame update
    public enum buffName
    {
        Speed =0,
        TwoMoon=1,
        Heal=2,
    }
    public void Init(buffName i,float time)
    {
        if (buffName.Speed == i)
        {
            SpeedBuffOn();
            if (speedTime >= 0 && speedTime <= time)
            {
                speedTime = time;
                checkSpeedTime = 0;
                readySpeed = true;
            }
        }
        else if (buffName.TwoMoon == i)
        {
            TwoMoonBuffOn();
            if (twoMoonTIME >= 0 && twoMoonTIME <= time)
            {
                twoMoonTIME = time;
                checkMoonTime = 0;
                readyMoon = true;
            }
        }
        else if (buffName.Heal == i) 
        {
            HealBuffOn();
            //photonView.RPC("HealBuffOn", RpcTarget.All);
            if (HealTime >= 0 && HealTime <= time)
            {
                HealTime = time;
                checkHealTime = 0;
                readyHeal = true;
            }
        }

    }
    private void Update()
    {
        if (readyMoon) 
        {
            checkMoonTime += Time.deltaTime;
            if (checkMoonTime >= twoMoonTIME)
            {
                TwoMoonOff();
            }
        }
        if (readySpeed)
        {
            checkSpeedTime += Time.deltaTime;
            if (checkSpeedTime >= speedTime)
            {
                SpeedOff();
            }
        }
        if (readyHeal) 
        {
            checkHealTime += Time.deltaTime;
            if (checkHealTime >= HealTime) 
            
            {
                HealOff();
            }
        }

    }
    private void SpeedOff() 
    {
        SpeedBuffOff();
        checkSpeedTime = 0f;
        speedTime = 0f;
        readySpeed = false;
    }
    private void TwoMoonOff()
    {
        TwoMoonBuffOff();
        _TwoMoonParticle.gameObject.SetActive(false);
        checkMoonTime = 0f;
        twoMoonTIME = 0f;
        readyMoon = false;
    }
    private void HealOff()
    {
        HealBuffOff();
        checkHealTime = 0f;
        HealTime = 0f;
        readyHeal = false;
    }

    public void SpeedBuffOn()
    {
        _speedParticle.gameObject.SetActive(true);
    }
    public void SpeedBuffOff()
    {
        _speedParticle.gameObject.SetActive(false);
    }
    public void TwoMoonBuffOn()
    {
        _TwoMoonParticle.gameObject.SetActive(true);
    }
    public void TwoMoonBuffOff()
    {
        _TwoMoonParticle.gameObject.SetActive(false);
    }
    public void HealBuffOn()
    {
        _HealParticle.gameObject.SetActive(true);
    }
    public void HealBuffOff()
    {
        _HealParticle.gameObject.SetActive(false);
    }
}
