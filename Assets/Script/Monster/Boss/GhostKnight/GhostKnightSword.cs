using UnityEngine;

public class GhostKnightSword : MonoBehaviour
{
    private Vector3 center;
    private float radius;
    private bool startOnLeft;
    private float damage;
    private float swingPeriod;
    private float selfRotationSpeed;
    private float startTime;
    private float startDelay;
    private int maxSwings;
    private bool isInitialized = false;

    public bool IsFinished { get; private set; } = false;

    public void Initialize(Vector3 centerPoint, float r, bool startLeft, float dmg, float prd, float rotSpd, float delaySec, int swings)
    {
        center = centerPoint;
        radius = r;
        startOnLeft = startLeft;
        damage = dmg;
        swingPeriod = Mathf.Max(0.1f, prd);
        selfRotationSpeed = rotSpd;
        startDelay = delaySec;
        maxSwings = Mathf.Max(1, swings);
        startTime = Time.time;
        IsFinished = false;
        isInitialized = true;

        // Apply flip for right-starting sword (which is on the right side of the boss)
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr == null)
        {
            sr = GetComponentInChildren<SpriteRenderer>();
        }
        if (sr != null && !startOnLeft)
        {
            sr.flipX = true;
        }

        UpdatePosition(0f);
    }

    private void Update()
    {
        if (!isInitialized || IsFinished) return;

        float elapsed = Time.time - startTime;
        if (elapsed < startDelay)
        {
            // Stay at the starting position and do not rotate or move
            UpdatePosition(0f);
            return;
        }

        float activeElapsed = elapsed - startDelay;
        float totalActiveDuration = maxSwings * (swingPeriod / 2f);

        if (activeElapsed >= totalActiveDuration)
        {
            activeElapsed = totalActiveDuration;
            UpdatePosition(activeElapsed);
            IsFinished = true;
            return;
        }

        // Start moving and rotating
        UpdatePosition(activeElapsed);
        transform.Rotate(Vector3.forward, selfRotationSpeed * Time.deltaTime);
    }

    private void UpdatePosition(float activeElapsed)
    {
        float omega = (2f * Mathf.PI) / swingPeriod;
        float theta;

        if (startOnLeft)
        {
            // Left peak starts at -PI, swings to 0, and back to -PI
            theta = -Mathf.PI / 2f - (Mathf.PI / 2f) * Mathf.Cos(omega * activeElapsed);
        }
        else
        {
            // Right peak starts at 0, swings to -PI, and back to 0
            theta = -Mathf.PI / 2f + (Mathf.PI / 2f) * Mathf.Cos(omega * activeElapsed);
        }

        Vector3 offset = new Vector3(radius * Mathf.Cos(theta), radius * Mathf.Sin(theta), 0f);
        transform.position = center + offset;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        TryDamage(collision);
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        TryDamage(collision);
    }

    private void TryDamage(Collider2D collision)
    {
        PlayerStatControl player = collision.GetComponentInParent<PlayerStatControl>();
        if (player != null)
        {
            player.TryApplyContactDamage(damage, gameObject.GetInstanceID());
        }
    }
}
