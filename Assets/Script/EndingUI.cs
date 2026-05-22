using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using TMPro;
using DG.Tweening;

/// <summary>
/// 完場 UI：顯示 HP / GEM / Total Score 動畫，並提供 PLAY AGAIN / QUIT 按鈕。
/// 把此腳本掛在場景中的空物件即可，會自動建立所有 UI 元素，不需手動拖 Inspector 欄位。
/// 由 BossController 在 GOLEM 死亡後 5 秒呼叫 ShowEnding(remainHP, gemCount)。
/// </summary>
public class EndingUI : MonoBehaviour
{
    // ── Inspector 欄位（選填；若未拖，Awake 自動建立）─────────────
    [Header("Panel (optional – auto-created if empty)")]
    [SerializeField] private GameObject rootPanel;
    [SerializeField] private CanvasGroup panelCanvasGroup;

    [Header("Score Texts (optional – auto-created if empty)")]
    [SerializeField] private TextMeshProUGUI hpLabelText;
    [SerializeField] private TextMeshProUGUI gemLabelText;
    [SerializeField] private TextMeshProUGUI scoreTitleText;    // "SCORE" 標籤
    [SerializeField] private TextMeshProUGUI totalScoreText;    // 8位數分數

    [Header("Buttons (optional – auto-created if empty)")]
    [SerializeField] private Button playAgainButton;
    [SerializeField] private Button quitButton;

    [Header("Font (optional – drag a TMP Font Asset for game-style look)")]
    [SerializeField] private TMP_FontAsset customFont;  // 留空使用預設字體；推薦 Press Start 2P

    // ── Score ──────────────────────────────────────────────────────
    [Header("Score Settings")]
    [SerializeField] private int hpMultiplier  = 10000;
    [SerializeField] private int gemMultiplier = 1000;
    [SerializeField] private int totalDigits   = 8;

    // ── Animation ─────────────────────────────────────────────────
    [Header("Animation Settings")]
    [SerializeField] private float panelFadeDuration  = 0.35f;
    [SerializeField] private float rowCountDuration   = 0.6f;
    [SerializeField] private float totalCountDuration = 1.8f;
    [SerializeField] private float betweenDelay       = 0.15f;
    [SerializeField] private Ease  totalEase          = Ease.OutCubic;
    [SerializeField] private bool  playOnStart        = false;

    // ── Replay ────────────────────────────────────────────────────
    [Header("Optional")]
    [SerializeField] private string replaySceneName = "";
    [SerializeField] private bool   useCurrentSceneIfReplaySceneEmpty = true;

    // ── Runtime ───────────────────────────────────────────────────
    private Sequence    currentSequence;
    private bool        isShowing;
    private AudioSource audioSource;
    private float       lastTickTime = -1f;
    private const float TickInterval = 0.05f;   // 每隔 0.05 秒最多播一次 tick 音效

    [Header("Sound (optional)")]
    [SerializeField] private AudioClip scoreTickSound;  // 跳字音效（拖入 Inspector 或留空）

    // ═══════════════════════════════════════════════════════════════
    // Unity callbacks
    // ═══════════════════════════════════════════════════════════════

    private void Awake()
    {
        EnsureEventSystem();

        // AudioSource：若物件上已有就用，否則自動加
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f; // 2D 音效
        }

        BuildUIIfNeeded();           // 若未在 Inspector 拖欄位，自動建立 UI

        if (playAgainButton != null)
            playAgainButton.onClick.AddListener(OnClickPlayAgain);
        if (quitButton != null)
            quitButton.onClick.AddListener(OnClickQuit);

        HideImmediate();
    }

    private void Start()
    {
        if (playOnStart)
            ShowEnding(3, 7);
    }

    private void OnDestroy()
    {
        if (playAgainButton != null)
            playAgainButton.onClick.RemoveListener(OnClickPlayAgain);
        if (quitButton != null)
            quitButton.onClick.RemoveListener(OnClickQuit);
        KillSequence();
    }

    // ═══════════════════════════════════════════════════════════════
    // Public API
    // ═══════════════════════════════════════════════════════════════

    public void ShowEnding(int remainHP, int gemCount)
    {
        KillSequence();
        isShowing = true;

        int hpScore    = Mathf.Max(0, remainHP)  * hpMultiplier;
        int gemScore   = Mathf.Max(0, gemCount)  * gemMultiplier;
        int totalScore = hpScore + gemScore;

        if (rootPanel != null)
            rootPanel.SetActive(true);

        if (panelCanvasGroup != null)
        {
            panelCanvasGroup.alpha          = 0f;
            panelCanvasGroup.interactable   = false;
            panelCanvasGroup.blocksRaycasts = true;
        }

        SetButtonsInteractable(false);

        if (hpLabelText   != null) hpLabelText.text    = "HP x " + hpMultiplier  + " = 0";
        if (gemLabelText  != null) gemLabelText.text   = "GEM x " + gemMultiplier + " = 0";
        if (totalScoreText!= null) totalScoreText.text = FormatScore(0);

        currentSequence = DOTween.Sequence();

        if (panelCanvasGroup != null)
            currentSequence.Append(panelCanvasGroup.DOFade(1f, panelFadeDuration));

        currentSequence.Append(DOVirtual.Int(0, hpScore, rowCountDuration, v =>
        {
            if (hpLabelText != null)
                hpLabelText.text = "HP x " + hpMultiplier + " = " + v;
            PlayTick();
        }));

        currentSequence.AppendInterval(betweenDelay);

        currentSequence.Append(DOVirtual.Int(0, gemScore, rowCountDuration, v =>
        {
            if (gemLabelText != null)
                gemLabelText.text = "GEM x " + gemMultiplier + " = " + v;
            PlayTick();
        }));

        currentSequence.AppendInterval(betweenDelay);

        currentSequence.Append(DOVirtual.Int(0, totalScore, totalCountDuration, v =>
        {
            if (totalScoreText != null)
                totalScoreText.text = FormatScore(v);
            PlayTick();
        }).SetEase(totalEase));

        currentSequence.AppendCallback(() =>
        {
            SetButtonsInteractable(true);
            if (panelCanvasGroup != null)
                panelCanvasGroup.interactable = true;
        });
    }

    public void HideImmediate()
    {
        KillSequence();
        isShowing = false;

        if (rootPanel != null)
            rootPanel.SetActive(false);

        if (panelCanvasGroup != null)
        {
            panelCanvasGroup.alpha          = 0f;
            panelCanvasGroup.interactable   = false;
            panelCanvasGroup.blocksRaycasts = false;
        }

        SetButtonsInteractable(false);

        if (hpLabelText   != null) hpLabelText.text    = "HP x " + hpMultiplier  + " = 0";
        if (gemLabelText  != null) gemLabelText.text   = "GEM x " + gemMultiplier + " = 0";
        if (totalScoreText!= null) totalScoreText.text = FormatScore(0);
    }

    // ═══════════════════════════════════════════════════════════════
    // Button callbacks
    // ═══════════════════════════════════════════════════════════════

    public void OnClickPlayAgain()
    {
        if (!isShowing) return;

        string target = replaySceneName;
        if (string.IsNullOrWhiteSpace(target) && useCurrentSceneIfReplaySceneEmpty)
            target = SceneManager.GetActiveScene().name;

        if (!string.IsNullOrWhiteSpace(target))
        {
            HideImmediate();
            SceneManager.LoadScene(target);
        }
    }

    public void OnClickQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ═══════════════════════════════════════════════════════════════
    // Auto UI builder
    // ═══════════════════════════════════════════════════════════════

    private void BuildUIIfNeeded()
    {
        // 若所有欄位都已在 Inspector 拖好，就不自動建立
        bool allAssigned = rootPanel      != null
                        && hpLabelText    != null
                        && gemLabelText   != null
                        && scoreTitleText != null
                        && totalScoreText != null
                        && playAgainButton != null
                        && quitButton      != null;
        if (allAssigned) return;

        Debug.Log("[EndingUI] Inspector 欄位未完全設定，自動建立 UI。");

        // ── Canvas ────────────────────────────────────────────────
        GameObject canvasGo = new GameObject("Ending Canvas", typeof(RectTransform));
        canvasGo.transform.SetParent(transform, false);

        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;

        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode        = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight  = 0.5f;

        canvasGo.AddComponent<GraphicRaycaster>();

        // ── Root Panel ────────────────────────────────────────────
        if (rootPanel == null)
        {
            rootPanel = new GameObject("Ending Panel", typeof(RectTransform));
            rootPanel.transform.SetParent(canvasGo.transform, false);

            // 半透明深色背景
            Image bg = rootPanel.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.72f);

            RectTransform rt = rootPanel.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        // ── Canvas Group ──────────────────────────────────────────
        if (panelCanvasGroup == null)
            panelCanvasGroup = rootPanel.AddComponent<CanvasGroup>();

        // ── Score Texts ───────────────────────────────────────────
        // 垂直佈局（由上到下）：
        //  0.72 – HP Label
        //  0.60 – GEM Label
        //  0.48 – "SCORE" 標題
        //  0.37 – 8位數分數
        //  0.15 – 按鈕列

        // HP：桃紅色，36pt（Press Start 2P 比預設字體寬，用 36pt 確保在螢幕內）
        if (hpLabelText == null)
        {
            hpLabelText = CreateTMPText("HP Label",
                rootPanel.transform,
                new Vector2(0.5f, 0.74f), new Vector2(1600f, 75f), 36,
                new Color(1f, 0.412f, 0.706f));   // 桃紅色 #FF69B4
            hpLabelText.fontStyle = FontStyles.Bold;
        }

        // GEM：淺藍色，36pt
        if (gemLabelText == null)
        {
            gemLabelText = CreateTMPText("GEM Label",
                rootPanel.transform,
                new Vector2(0.5f, 0.62f), new Vector2(1600f, 75f), 36,
                new Color(0.357f, 0.784f, 0.961f)); // 淺藍色 #5BC8F5
            gemLabelText.fontStyle = FontStyles.Bold;
        }

        // "SCORE" 標題（橙色，字距拉大）
        if (scoreTitleText == null)
        {
            scoreTitleText = CreateTMPText("Score Title",
                rootPanel.transform,
                new Vector2(0.5f, 0.47f), new Vector2(600f, 70f), 42,
                new Color(1f, 0.65f, 0f));       // 橙色
            scoreTitleText.text      = "<cspace=0.25em>S C O R E</cspace>";
            scoreTitleText.fontStyle = FontStyles.Bold;
        }

        // 8位等寬數字（黃色，大字）
        if (totalScoreText == null)
        {
            totalScoreText = CreateTMPText("Total Score",
                rootPanel.transform,
                new Vector2(0.5f, 0.36f), new Vector2(960f, 120f), 80,
                Color.yellow);
            totalScoreText.fontStyle = FontStyles.Bold;
        }

        // 套用自定義字體（若有設定）
        ApplyFont(hpLabelText);
        ApplyFont(gemLabelText);
        ApplyFont(scoreTitleText);
        ApplyFont(totalScoreText);

        // ── Buttons ───────────────────────────────────────────────
        if (playAgainButton == null)
        {
            // 屏幕上方：anchorPos=(0.5, 1)，pivot=(1,1)，距頂部 -60px，向左偏 -20px
            playAgainButton = CreateImageButton("PlayAgainBtn",
                rootPanel.transform, "BTN_PLAYAGAIN",
                new Vector2(0.5f, 1f), new Vector2(-20f, -60f), new Vector2(760f, 240f),
                new Vector2(1f, 1f));
        }

        if (quitButton == null)
        {
            // 屏幕上方：anchorPos=(0.5, 1)，pivot=(0,1)，距頂部 -60px，向右偏 +20px
            quitButton = CreateImageButton("QuitBtn",
                rootPanel.transform, "BTN_QUIT",
                new Vector2(0.5f, 1f), new Vector2(20f, -60f), new Vector2(760f, 240f),
                new Vector2(0f, 1f));
        }
    }

    // ── Helper: 建立 TextMeshProUGUI ──────────────────────────────
    private TextMeshProUGUI CreateTMPText(string objName, Transform parent,
        Vector2 anchorCenter, Vector2 size, int fontSize,
        Color? color = null)
    {
        GameObject go = new GameObject(objName, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin        = anchorCenter;
        rt.anchorMax        = anchorCenter;
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta        = size;

        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.alignment             = TextAlignmentOptions.Center;
        tmp.fontSize              = fontSize;
        tmp.color                 = color ?? Color.white;
        tmp.richText              = true;          // 確保 <mspace> / <cspace> 標籤生效
        tmp.enableWordWrapping    = false;
        tmp.overflowMode          = TextOverflowModes.Overflow;  // 允許超出邊框，不截斷
        tmp.text                  = "...";
        return tmp;
    }

    // ── Helper: 建立純圖片按鈕 ────────────────────────────────────
    private Button CreateImageButton(string objName, Transform parent,
        string spriteName,
        Vector2 anchorPos, Vector2 offset, Vector2 size,
        Vector2 pivot)
    {
        Sprite spr = LoadSprite(spriteName);

        GameObject go = new GameObject(objName, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin        = anchorPos;
        rt.anchorMax        = anchorPos;
        rt.pivot            = pivot;
        rt.anchoredPosition = offset;
        rt.sizeDelta        = size;

        Image img = go.AddComponent<Image>();
        if (spr != null)
        {
            img.sprite         = spr;
            img.type           = Image.Type.Simple;
            img.preserveAspect = true;
            img.color          = Color.white;
        }
        else
        {
            img.color = new Color(0.3f, 0.3f, 0.8f, 0.85f);
            Debug.LogWarning($"[EndingUI] 找不到圖片 Resources/{spriteName}，使用純色備用。");
        }

        Button btn = go.AddComponent<Button>();
        ColorBlock cb  = btn.colors;
        cb.normalColor      = Color.white;
        cb.highlightedColor = new Color(1f, 1f, 0.8f, 1f);
        cb.pressedColor     = new Color(0.75f, 0.75f, 0.75f, 1f);
        btn.colors = cb;

        return btn;
    }

    // ── Helper: 載入 Sprite ───────────────────────────────────────
    private Sprite LoadSprite(string name)
    {
        if (string.IsNullOrEmpty(name)) return null;
        Sprite spr = Resources.Load<Sprite>(name);
        if (spr != null) return spr;
        Texture2D tex = Resources.Load<Texture2D>(name);
        if (tex == null) return null;
        return Sprite.Create(tex,
            new Rect(0f, 0f, tex.width, tex.height),
            new Vector2(0.5f, 0.5f), 100f);
    }

    // ═══════════════════════════════════════════════════════════════
    // Helpers
    // ═══════════════════════════════════════════════════════════════

    private void SetButtonsInteractable(bool v)
    {
        if (playAgainButton != null) playAgainButton.interactable = v;
        if (quitButton      != null) quitButton.interactable      = v;
    }

    private string FormatScore(int v)
    {
        v = Mathf.Max(0, v);
        // <mspace=0.72em> 讓每個數字佔相同寬度，做出 8-digit 等寬效果
        string digits = v.ToString("D" + Mathf.Max(1, totalDigits));
        return "<mspace=0.72em>" + digits + "</mspace>";
    }

    /// <summary>
    /// 跳字時播放音效，頻率上限由 TickInterval 控制，避免每幀都觸發。
    /// </summary>
    private void PlayTick()
    {
        if (scoreTickSound == null || audioSource == null) return;
        if (Time.unscaledTime - lastTickTime < TickInterval) return;
        lastTickTime = Time.unscaledTime;
        audioSource.PlayOneShot(scoreTickSound, 0.7f);
    }

    /// <summary>
    /// 若有設定 customFont，套用到指定的 TMP 元素。
    /// </summary>
    private void ApplyFont(TextMeshProUGUI tmp)
    {
        if (tmp == null || customFont == null) return;
        tmp.font = customFont;
    }

    private void KillSequence()
    {
        if (currentSequence != null && currentSequence.IsActive())
            currentSequence.Kill();
        currentSequence = null;
    }

    private static void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() != null) return;
        GameObject esGo = new GameObject("EventSystem");
        esGo.AddComponent<EventSystem>();
        esGo.AddComponent<StandaloneInputModule>();
    }
}
