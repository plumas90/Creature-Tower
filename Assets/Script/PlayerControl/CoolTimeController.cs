using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CoolTimeController : MonoBehaviour
{
    private TopDownCharacterController controller;


    public float curRollCool = 0;

    public float curReloadCool = 0;

    public float curAttackCool = 0;

    public float curSkillCool = 0;

    public float stackedTime = 0;
    //public bool isKeepCount; ���� ���� üũ - �� ������ 
    //private bool isCharging; ��¡ ��� ��� üũ
    //public int bulletNum; ��¡ ī��Ʈ

    // �߰�
    //public event Action CallTimeCountEvent;

    private void Awake()
    {
        controller = GetComponent<TopDownCharacterController>();
    }
    private void Start()
    {
        controller.OnEndRollEvent += RollCoolTime;
        controller.OnReloadEvent += ReloadCoolTime;
        controller.OnAttackEvent += AttackCoolTime;
        controller.OnEndSkillEvent += SkillCoolTime;
        //controller.OnAttackKeepEvent += TimeCount;
    }


    private void Update() // ��Ÿ�� �� �ð� ����
    {
        CountRollCoolTime();
        CountReloadCoolTime();
        CountAttackCoolTime();
        CountSkillCoolTime();
        //CountTimeNBullets();
    }

    private void RollCoolTime()
    {
        // TV 스킬의 강제 구르기(CompulsoryRoll)는 롤 자원/쿨과 분리한다.
        if (controller.IsSkillRollActive)
        {
            return;
        }

        // 이미 최대 스택이면 롤 쿨타임을 시작할 필요가 없다.
        if (controller.playerStatHandler.CurRollStack >= controller.playerStatHandler.MaxRollStack)
        {
            controller.playerStatHandler.CanRoll = true;
            controller.playerStatHandler.UseRoll = true;
            curRollCool = 0f;
            return;
        }

        float coolTime = controller.playerStatHandler.RollCoolTime.total;
        controller.playerStatHandler.CanRoll = (controller.playerStatHandler.CurRollStack > 0);

        // 이미 진행 중인 쿨타임이 있으면 중복 이벤트로 리셋하지 않는다.
        if (curRollCool <= 0f)
        {
            curRollCool = Mathf.Max(0.0001f, coolTime);
        }
        controller.playerStatHandler.UseRoll = false;
    }
    public void EndRollCoolTime()
    {
        // 이미 최대면 안전하게 상태만 정리한다.
        if (controller.playerStatHandler.CurRollStack >= controller.playerStatHandler.MaxRollStack)
        {
            controller.playerStatHandler.CurRollStack = controller.playerStatHandler.MaxRollStack;
            controller.playerStatHandler.CanRoll = true;
            controller.playerStatHandler.UseRoll = true;
            curRollCool = 0f;
            return;
        }

        controller.playerStatHandler.CurRollStack = Mathf.Min(
            controller.playerStatHandler.MaxRollStack,
            controller.playerStatHandler.CurRollStack + 1
        );
        controller.playerStatHandler.CanRoll = true;

        if (controller.playerStatHandler.CurRollStack < controller.playerStatHandler.MaxRollStack)
        {
            controller.playerStatHandler.UseRoll = false;
            curRollCool = Mathf.Max(0.0001f, controller.playerStatHandler.RollCoolTime.total);
        }
        else
        {
            controller.playerStatHandler.UseRoll = true;
            curRollCool = 0f;
        }
    }

    private void CountRollCoolTime()
    {
        if (curRollCool > 0)
        {
            curRollCool -= Time.deltaTime;
        }

        if (curRollCool <= 0f
            && controller.playerStatHandler.UseRoll == false
            && controller.playerStatHandler.CurRollStack < controller.playerStatHandler.MaxRollStack)
        {
            EndRollCoolTime();
        }
    }

    private void ReloadCoolTime()
    {
        float coolTime = controller.playerStatHandler.ReloadCoolTime.total;
        controller.playerStatHandler.CanReload = false;
        curReloadCool = coolTime;
    }
    private void EndReloadCoolTime()
    {
        controller.playerStatHandler.CanReload = true;
        controller.playerStatHandler.CurAmmo = controller.playerStatHandler.AmmoMax.total;
        controller.CallEndReloadEvent();
    }

    private void CountReloadCoolTime()
    {
        if (curReloadCool > 0)
        {
            curReloadCool -= Time.deltaTime;
        }
        if (controller.playerStatHandler.CanReload == false && curReloadCool <= 0f /*&& !isKeepCount*/)
        {
            EndReloadCoolTime();
        }
    }
    public void AttackCoolTime()
    {
        float coolTime = 1 / controller.playerStatHandler.AtkSpeed.total;
        controller.playerStatHandler.CanFire = false;
        curAttackCool = coolTime;
    }

    private void CountAttackCoolTime()
    {
        if (curAttackCool > 0)
        {
            curAttackCool -= Time.deltaTime;
        }
        if (controller.playerStatHandler.CanFire == false && curAttackCool <= 0)
        {
            EndAttackCoolTime();
        }
    }

    private void EndAttackCoolTime()
    {
        controller.playerStatHandler.CanFire = true;
        controller.CallAttackEndEvent();
    }

    private void SkillCoolTime()
    {
        // 이미 최대 스택이면 스킬 쿨타임을 시작할 필요가 없다.
        if (controller.playerStatHandler.CurSkillStack >= controller.playerStatHandler.MaxSkillStack)
        {
            controller.playerStatHandler.CanSkill = true;
            curSkillCool = 0f;
            return;
        }

        float coolTime = controller.playerStatHandler.SkillCoolTime.total;
        controller.playerStatHandler.CanSkill = (controller.playerStatHandler.CurSkillStack > 0);

        // 이미 진행 중인 쿨타임이 있으면 중복 이벤트로 누적하지 않는다.
        if (curSkillCool <= 0f)
        {
            curSkillCool = Mathf.Max(0.0001f, coolTime);
        }
    }

    private void EndSkillCoolTime()
    {
        // 이미 최대면 안전하게 상태만 정리한다.
        if (controller.playerStatHandler.CurSkillStack >= controller.playerStatHandler.MaxSkillStack)
        {
            controller.playerStatHandler.CurSkillStack = controller.playerStatHandler.MaxSkillStack;
            controller.playerStatHandler.CanSkill = true;
            curSkillCool = 0f;
            return;
        }

        controller.playerStatHandler.CurSkillStack = Mathf.Min(
            controller.playerStatHandler.MaxSkillStack,
            controller.playerStatHandler.CurSkillStack + 1
        );
        controller.playerStatHandler.CanSkill = true;

        if (controller.playerStatHandler.CurSkillStack < controller.playerStatHandler.MaxSkillStack)
        {
            curSkillCool = Mathf.Max(0.0001f, controller.playerStatHandler.SkillCoolTime.total);
        }
        else
        {
            curSkillCool = 0f;
        }
    }

    private void CountSkillCoolTime()
    {
        if (curSkillCool > 0)
        {
            curSkillCool -= Time.deltaTime;
        }

        if (curSkillCool <= 0f
            && controller.playerStatHandler.useSkill == false
            && controller.playerStatHandler.CurSkillStack < controller.playerStatHandler.MaxSkillStack)
        {
            EndSkillCoolTime();
        }
    }

    /*public void TimeCount(bool isCount)
    {
        if (isCount)
        {
            isKeepCount = true;
            stackedTime = 0;
            bulletNum = 0;
            controller.playerStatHandler.CanReload = false;
        }
        else
        {
            isKeepCount = false;
            controller.playerStatHandler.CanReload = true;
            Debug.Log($"���� ������ �ð� : {stackedTime}");
            Debug.Log($"���� �Ҹ� �� : {bulletNum}");
            Debug.Log($"���� �Ѿ� �� : {controller.playerStatHandler.CurAmmo}");
            //GetComponent<WeaponSystem>().ChargeCalculate(stackedTime);
            // ���⼭ ���� �̺�Ʈ�� �Ķ���ͷν�? ���� �����ؾ���.
        }
    }*/

    /*public IEnumerator CountBullets()
    {
        isCharging = true;
        Debug.Log($"���� ��ź �� : {controller.playerStatHandler.CurAmmo}");
        if (controller.playerStatHandler.CurAmmo >= 1)
        {
            controller.playerStatHandler.CurAmmo--;
            bulletNum++;
            Debug.Log($"�Ҹ� �״� �� {bulletNum}");
        }
        yield return new WaitForSeconds(0.15f);
        isCharging = false;
    }

    private void CountTimeNBullets()
    {
        if (isKeepCount)
        {
            stackedTime += Time.deltaTime;
            if (!isCharging)
            {
                StartCoroutine(CountBullets());
            }
        }
    }
    */
}

