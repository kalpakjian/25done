using UnityEngine;

/// <summary>
/// 放在有 BoxCollider (Is Trigger = true) 的 GameObject 上。
/// 當 Player 進入此 Trigger 時，呼叫 BGMManager 切換背景音樂。
///
/// 使用前必須確認：
/// 1. 此 GameObject 有 Collider，且 Is Trigger = true
/// 2. Player 的根物件 Tag 是 "Player"
/// 3. 場景中有 BGMManager GameObject（帶有 BGMManager.cs + AudioSource）
/// 4. roomBGM 欄位已指定 AudioClip
/// </summary>
public class RoomMusicTrigger : MonoBehaviour
{
    [SerializeField] private AudioClip roomBGM;
    [SerializeField] private string playerTag = "Player";

    private void Awake()
    {
        // 檢查 Collider 是否存在且是 Trigger
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            Debug.LogError($"[RoomMusicTrigger] '{gameObject.name}' 沒有 Collider！請加上 Collider 並勾選 Is Trigger。", this);
        }
        else if (!col.isTrigger)
        {
            Debug.LogWarning($"[RoomMusicTrigger] '{gameObject.name}' 的 Collider 沒有勾選 Is Trigger！OnTriggerEnter 不會觸發。", this);
        }

        // 提醒 BGM 欄位沒設定
        if (roomBGM == null)
        {
            Debug.LogWarning($"[RoomMusicTrigger] '{gameObject.name}' 的 roomBGM 欄位沒有指定 AudioClip！進入此房間將不會播放音樂。", this);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // 確認是 Player
        if (!other.CompareTag(playerTag))
            return;

        Debug.Log($"[RoomMusicTrigger] Player 進入 '{gameObject.name}'，嘗試播放 BGM：{(roomBGM != null ? roomBGM.name : "null")}");

        // 確認 BGMManager 是否存在
        if (BGMManager.Instance == null)
        {
            Debug.LogError("[RoomMusicTrigger] BGMManager.Instance 是 null！請在場景中建立一個 GameObject，掛上 BGMManager.cs 和 AudioSource 組件。");
            return;
        }

        // 確認 AudioClip 是否設定
        if (roomBGM == null)
        {
            Debug.LogWarning($"[RoomMusicTrigger] '{gameObject.name}' 的 roomBGM 是 null，無法播放音樂。請在 Inspector 指定 AudioClip。", this);
            return;
        }

        BGMManager.Instance.PlayBGM(roomBGM);
    }

#if UNITY_EDITOR
    // 在 Scene 視圖中顯示 Trigger 範圍，方便除錯
    private void OnDrawGizmos()
    {
        Collider col = GetComponent<Collider>();
        if (col == null) return;

        UnityEditor.Handles.color = new Color(0.2f, 0.8f, 1f, 0.25f);
        if (col is BoxCollider box)
        {
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.4f);
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(box.center, box.size);
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.9f);
            Gizmos.DrawWireCube(box.center, box.size);
        }

        // 顯示 BGM 名稱標籤
        UnityEditor.Handles.color = Color.cyan;
        string label = roomBGM != null ? $"♪ {roomBGM.name}" : "⚠ No BGM set";
        UnityEditor.Handles.Label(transform.position + Vector3.up * 1.5f, label);
    }
#endif
}
