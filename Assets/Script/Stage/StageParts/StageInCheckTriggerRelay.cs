using UnityEngine;

/// <summary>
/// StageInCheckPoint의 자식 콜라이더 트리거를 부모 StageInCheckPoint로 전달한다.
/// </summary>
[DisallowMultipleComponent]
public class StageInCheckTriggerRelay : MonoBehaviour
{
    public StageInCheckPoint target;

    private void Awake()
    {
        if (target == null)
            target = GetComponentInParent<StageInCheckPoint>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (target != null)
            target.HandleTriggerEnter(collision);
    }
}
