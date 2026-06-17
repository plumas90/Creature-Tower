using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HumanAttackintelligentmissile : MonoBehaviour
{
    bool targeting;
    GameObject target;
    Bullet _bullet;
    private float turningForce;
    public bool ready;
    int targetName;

    // 유도 추적 타이머
    private float trackingTimer = 0f;
    private const float MAX_TRACKING_DURATION = 1.5f; // 최대 추적 시간

    public void init(int i)
    {
        if (i == 1)
        {
            //Debug.Log("인공지능 미사일 초기화222");
            targeting = false;
            _bullet = GetComponentInParent<Bullet>();
            turningForce = 5f;
            targetName = 7;
            ready = true;
        }
        else
        {
            //Debug.Log("인공지능 미사일 초기화");
            //transform.localScale = new Vector3(5, 5, 0);
            targeting = false;
            _bullet = GetComponentInParent<Bullet>();
            turningForce = 5f;
            targetName = 8;
            ready = true;
        }
        trackingTimer = 0f;
    }

    void Update()
    {
        if (targeting)
        {
            if (target == null) { return; }

            // 지정된 최대 추적 시간이 지나면 유도를 해제하고 직진
            trackingTimer += Time.deltaTime;
            if (trackingTimer >= MAX_TRACKING_DURATION)
            {
                targeting = false;
                target = null;
                return;
            }

            Vector2 dir = (target.transform.position - transform.position).normalized;
            _bullet.gameObject.transform.right = Vector3.Slerp(_bullet.gameObject.transform.right.normalized, dir, turningForce * Time.deltaTime);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (ready && !targeting && collision.gameObject.layer == targetName)
        {
            targeting = true;
            target = collision.gameObject;
            trackingTimer = 0f; // 추적 시작 시점부터 타이머 누적
        }
    }
}
