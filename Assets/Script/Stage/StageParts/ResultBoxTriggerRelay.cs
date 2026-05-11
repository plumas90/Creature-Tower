using UnityEngine;

// Box 자식 오브젝트에 붙어서 충돌 이벤트를 부모 ResultBox로 전달
public class ResultBoxTriggerRelay : MonoBehaviour
{
    private ResultBox owner;

    private void Awake()
    {
        owner = GetComponentInParent<ResultBox>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (owner == null)
            owner = GetComponentInParent<ResultBox>();

        if (owner != null)
            owner.OnBoxTriggerEnter(collision);
    }
}
