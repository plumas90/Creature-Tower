using UnityEngine;

public class GhostKnightSword : MonoBehaviour
{
    public enum SwordMovementMode { Pendulum, LinearInward, TargetedLaunch }
    private SwordMovementMode movementMode = SwordMovementMode.Pendulum;

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

    private Vector3 startPoint;
    private float linearSpeed;
    private Vector3 launchDirection;

    public bool IsFinished { get; private set; } = false;

    public void Initialize(Vector3 centerPoint, float r, bool startLeft, float dmg, float prd, float rotSpd, float delaySec, int swings)
    {
        movementMode = SwordMovementMode.Pendulum;
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

    public void InitializeLinearInward(Vector3 centerPoint, Vector3 spawnPoint, float dmg, float speedVal, float rotSpd, float delaySec, bool flipSprite)
    {
        movementMode = SwordMovementMode.LinearInward;
        center = centerPoint;
        startPoint = spawnPoint;
        damage = dmg;
        linearSpeed = speedVal;
        selfRotationSpeed = rotSpd;
        startDelay = delaySec;
        startTime = Time.time;
        IsFinished = false;
        isInitialized = true;

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr == null)
        {
            sr = GetComponentInChildren<SpriteRenderer>();
        }
        if (sr != null)
        {
            sr.flipX = flipSprite;
        }

        transform.position = startPoint;
    }

    public void InitializeTargetedLaunch(Vector3 spawnPoint, Vector3 direction, float dmg, float speedVal, float delaySec)
    {
        movementMode = SwordMovementMode.TargetedLaunch;
        startPoint = spawnPoint;
        launchDirection = direction.normalized;
        damage = dmg;
        linearSpeed = speedVal;
        startDelay = delaySec;
        startTime = Time.time;
        IsFinished = false;
        isInitialized = true;

        // Set rotation so that "top-right" (45 degrees on the sprite) faces the launchDirection
        float targetAngle = Mathf.Atan2(launchDirection.y, launchDirection.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, targetAngle - 45f);

        transform.position = startPoint;
    }

    public void UpdateStartPoint(Vector3 pos)
    {
        startPoint = pos;
        transform.position = pos;
    }

    private void Update()
    {
        if (!isInitialized || IsFinished) return;

        float elapsed = Time.time - startTime;
        if (elapsed < startDelay)
        {
            // Stay at the starting position and do not rotate or move
            if (movementMode == SwordMovementMode.LinearInward || movementMode == SwordMovementMode.TargetedLaunch)
            {
                transform.position = startPoint;
            }
            else
            {
                UpdatePosition(0f);
            }
            return;
        }

        float activeElapsed = elapsed - startDelay;

        if (movementMode == SwordMovementMode.LinearInward)
        {
            transform.position = Vector3.MoveTowards(transform.position, center, linearSpeed * Time.deltaTime);
            transform.Rotate(Vector3.forward, selfRotationSpeed * Time.deltaTime);

            if (Vector3.Distance(transform.position, center) < 0.02f)
            {
                IsFinished = true;
                Destroy(gameObject);
            }
        }
        else if (movementMode == SwordMovementMode.TargetedLaunch)
        {
            transform.position += launchDirection * (linearSpeed * Time.deltaTime);
            
            // Self-destruct if it flies too far (e.g. 20 units away from startPoint) to prevent memory leak
            if (Vector3.Distance(transform.position, startPoint) > 20f)
            {
                IsFinished = true;
                Destroy(gameObject);
            }
        }
        else
        {
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
