using UnityEngine;

/// <summary>
/// 箭矢投射物 – 由 Rogue 射出，碰到 Player 時造成傷害後銷毀。
/// 飛行超過 lifetime 秒也會自動銷毀。
/// </summary>
public class Arrow : MonoBehaviour
{
    [HideInInspector] public float damage;
    [HideInInspector] public Vector3 direction;
    public float speed = 20f;
    public float lifetime = 5f;

    void Start()
    {
        // 面向飛行方向
        if (direction != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(direction);

        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        transform.position += direction.normalized * speed * Time.deltaTime;
    }

    void OnTriggerEnter(Collider col)
    {
        if (col.CompareTag("Player") && damage > 0)
        {
            col.SendMessage("Hurt", damage);
            Destroy(gameObject);
        }

        // 碰到牆壁或地板也銷毀
        if (col.CompareTag("Wall") || col.CompareTag("Ground"))
        {
            Destroy(gameObject);
        }
    }
}
