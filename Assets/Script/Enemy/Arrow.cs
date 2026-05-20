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

    [Header("Hit Detection")]
    [Tooltip("箭矢移動時用球形掃描補判，避免速度快或 Collider 太細時穿過玩家。")]
    public float hitRadius = 0.15f;

    private bool hasHit = false;
    private Collider ownCollider;
    private Rigidbody rb;

    void Awake()
    {
        ownCollider = GetComponent<Collider>();
        rb = GetComponent<Rigidbody>();

        // 這個箭矢由程式控制位移；關掉物理重力/動態剛體可避免飛行高度被 Rigidbody 拉偏。
        if (rb != null)
        {
            rb.useGravity = false;
            rb.isKinematic = true;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        }
    }

    void Start()
    {
        if (direction.sqrMagnitude <= 0.001f)
            direction = transform.forward;

        direction.Normalize();

        // 面向飛行方向
        if (direction != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(direction);

        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        if (hasHit) return;

        Vector3 move = direction * speed * Time.deltaTime;
        Vector3 currentPos = transform.position;
        Vector3 nextPos = currentPos + move;

        // 補上連續碰撞偵測：即使一幀飛過玩家 Collider，也能命中。
        // 使用 SphereCastAll 可略過自己/弓箭手身上的非目標 Collider，避免第一個命中物擋掉玩家判定。
        RaycastHit[] hits = Physics.SphereCastAll(currentPos, hitRadius, direction, move.magnitude, ~0, QueryTriggerInteraction.Collide);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (RaycastHit hit in hits)
        {
            if (hit.collider != ownCollider && TryHandleHit(hit.collider))
                return;
        }

        transform.position = nextPos;
    }

    void OnTriggerEnter(Collider col)
    {
        TryHandleHit(col);
    }

    private bool TryHandleHit(Collider col)
    {
        if (hasHit || col == null || col == ownCollider)
            return false;

        bool hitPlayer = col.CompareTag("Player") || col.GetComponentInParent<PlayerLife>() != null;
        if (hitPlayer && damage > 0)
        {
            hasHit = true;
            col.SendMessageUpwards("Hurt", damage, SendMessageOptions.DontRequireReceiver);
            Destroy(gameObject);
            return true;
        }

        // 碰到牆壁或地板也銷毀
        if (col.CompareTag("Wall") || col.CompareTag("Ground"))
        {
            hasHit = true;
            Destroy(gameObject);
            return true;
        }

        return false;
    }
}
