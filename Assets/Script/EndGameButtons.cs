using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

/// <summary>
/// 完場按鈕 UI：GOLEM 被打倒後 5 秒由 BossController 呼叫 EndGameButtons.Show()。
/// 自動建立 Canvas，在屏幕上方顯示 PLAY AGAIN 與 QUIT 兩個按鈕。
/// 遊戲不暫停，Player 可繼續活動。
/// </summary>
public class EndGameButtons : MonoBehaviour
{
    private static EndGameButtons _instance;

    private GameObject _panel;
    private bool _shown = false;

    // ──────────────────────────────────────────────────────────────
    // Singleton 初始化：從場景任意腳本呼叫 Show() 即可，不需預先放置
    // ──────────────────────────────────────────────────────────────
    private static EndGameButtons GetInstance()
    {
        if (_instance != null) return _instance;

        GameObject go = new GameObject("EndGameButtons");
        _instance = go.AddComponent<EndGameButtons>();
        DontDestroyOnLoad(go);
        return _instance;
    }

    /// <summary>
    /// 由外部（BossController）呼叫，顯示完場按鈕 UI。
    /// </summary>
    public static void Show()
    {
        GetInstance().ShowButtons();
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
        BuildUI();
    }

    private void OnEnable()
    {
        // 每次場景載入完成後，重置顯示狀態，讓下一局可以再次顯示按鈕
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 隱藏按鈕面板，重置旗標，下次打死 GOLEM 可再次觸發
        _shown = false;
        if (_panel != null)
            _panel.SetActive(false);
    }

    private void BuildUI()
    {
        // ── EventSystem ──────────────────────────────────────────
        if (FindObjectOfType<EventSystem>() == null)
        {
            GameObject esGo = new GameObject("EventSystem");
            esGo.AddComponent<EventSystem>();
            esGo.AddComponent<StandaloneInputModule>();
            DontDestroyOnLoad(esGo);
        }

        // ── Canvas ───────────────────────────────────────────────
        GameObject canvasGo = new GameObject("EndGame Canvas", typeof(RectTransform));
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;

        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGo.AddComponent<GraphicRaycaster>();
        DontDestroyOnLoad(canvasGo);

        // ── Panel（整個 Canvas 不添加背景，只當容器）─────────────
        _panel = new GameObject("EndGame Panel", typeof(RectTransform));
        _panel.transform.SetParent(canvasGo.transform, false);

        RectTransform panelRect = _panel.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        // ── PLAY AGAIN 按鈕（左，圖片已含文字）──────────────────
        // 兩個按鈕以屏幕中心為基準，各往左右偏移
        Button playAgainBtn = CreateImageButton("PlayAgainBtn", _panel.transform, "BTN_PLAYAGAIN");
        RectTransform playRect = playAgainBtn.GetComponent<RectTransform>();
        playRect.anchorMin        = new Vector2(0.5f, 1f);
        playRect.anchorMax        = new Vector2(0.5f, 1f);
        playRect.pivot            = new Vector2(1f, 1f);         // 右對齊，向左延伸
        playRect.anchoredPosition = new Vector2(-20f, -60f);     // 距中心 20px、距頂 60px
        playRect.sizeDelta        = new Vector2(760f, 240f);     // ← 修改此處可調整按鈕大小（寬 x 高）
        playAgainBtn.onClick.AddListener(OnPlayAgain);

        // ── QUIT 按鈕（右，圖片已含文字）────────────────────────
        Button quitBtn = CreateImageButton("QuitBtn", _panel.transform, "BTN_QUIT");
        RectTransform quitRect = quitBtn.GetComponent<RectTransform>();
        quitRect.anchorMin        = new Vector2(0.5f, 1f);
        quitRect.anchorMax        = new Vector2(0.5f, 1f);
        quitRect.pivot            = new Vector2(0f, 1f);         // 左對齊，向右延伸
        quitRect.anchoredPosition = new Vector2(20f, -60f);      // 距中心 20px、距頂 60px
        quitRect.sizeDelta        = new Vector2(760f, 240f);     // ← 修改此處可調整按鈕大小（寬 x 高）
        quitBtn.onClick.AddListener(OnQuit);

        // 預設隱藏
        _panel.SetActive(false);
    }

    /// <summary>
    /// 從 Resources 載入圖片，先嘗試 Sprite，失敗則改用 Texture2D 建立（與 StartGameMenu 相同邏輯）。
    /// </summary>
    private Sprite LoadSprite(string spriteName)
    {
        if (string.IsNullOrEmpty(spriteName)) return null;

        Sprite spr = Resources.Load<Sprite>(spriteName);
        if (spr != null) return spr;

        Texture2D tex = Resources.Load<Texture2D>(spriteName);
        if (tex == null) return null;

        return Sprite.Create(tex,
            new Rect(0f, 0f, tex.width, tex.height),
            new Vector2(0.5f, 0.5f), 100f);
    }

    /// <summary>
    /// 建立純圖片按鈕（圖片上已有文字，不加額外 Text 組件）。
    /// spriteName：Resources 資料夾內的圖片名稱（不含副檔名）。
    /// </summary>
    private Button CreateImageButton(string objName, Transform parent, string spriteName)
    {
        Sprite btnSprite = LoadSprite(spriteName);

        GameObject go = new GameObject(objName, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        Image img = go.AddComponent<Image>();
        if (btnSprite != null)
        {
            img.sprite = btnSprite;
            img.type = Image.Type.Simple;
            img.preserveAspect = true;
            img.color = Color.white;
        }
        else
        {
            // 找不到圖片時用醒目純色作備用
            img.color = new Color(0.2f, 0.4f, 0.8f, 0.85f);
            Debug.LogWarning($"[EndGameButtons] 找不到按鈕圖片 Resources/{spriteName}，使用純色備用。");
        }

        Button btn = go.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.normalColor      = Color.white;
        cb.highlightedColor = new Color(1f, 1f, 0.8f, 1f);
        cb.pressedColor     = new Color(0.75f, 0.75f, 0.75f, 1f);
        cb.selectedColor    = cb.highlightedColor;
        btn.colors = cb;

        return btn;
    }

    private void ShowButtons()
    {
        if (_shown) return;
        _shown = true;

        if (_panel != null)
            _panel.SetActive(true);
    }

    private void OnPlayAgain()
    {
        // 立即隱藏按鈕，1 秒後才載入場景
        if (_panel != null) _panel.SetActive(false);
        StartCoroutine(LoadSceneAfterDelay(1f));
    }

    private System.Collections.IEnumerator LoadSceneAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void OnQuit()
    {
        // 立即隱藏按鈕
        if (_panel != null) _panel.SetActive(false);
        Time.timeScale = 1f;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
