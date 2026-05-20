using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Shows a simple start menu before the game begins.
/// Attach this to any active GameObject in the first scene, or let it be created automatically before the scene loads.
/// </summary>
public class StartGameMenu : MonoBehaviour
{
    [Header("Menu Settings")]
    [SerializeField] private bool showMenuOnStart = true;
    [SerializeField] private string startButtonText = "START GAME";
    [SerializeField] private string quitButtonText = "QUIT";
    [SerializeField] private string coverResourceName = "COVER";
    [SerializeField] private string buttonResourceName = "BTNGOLD";
    [SerializeField] private string quitButtonResourceName = "BTNDARKBLUE";

    [Header("Cover Image")]
    [Tooltip("Optional cover sprite. If empty, the script will load a Sprite named COVER from a Resources folder.")]
    [SerializeField] private Sprite coverSprite;
    [SerializeField] private Sprite buttonSprite;
    [SerializeField] private Sprite quitButtonSprite;
    [Range(0f, 1f)]
    [SerializeField] private float coverDarkOverlayAlpha = 0.15f;

    [Header("Optional Existing UI")]
    [Tooltip("If assigned, this panel will be shown/hidden instead of creating the default menu UI.")]
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private Button startButton;
    [SerializeField] private Button quitButton;

    private Canvas generatedCanvas;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateAutomatically()
    {
        if (FindObjectOfType<StartGameMenu>() != null) return;

        GameObject menuObject = new GameObject("Start Game Menu");
        menuObject.AddComponent<StartGameMenu>();
        DontDestroyOnLoad(menuObject);
    }

    private void Awake()
    {
        if (FindObjectsOfType<StartGameMenu>().Length > 1)
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);
        EnsureEventSystemExists();
        EnsureMenuExists();
    }

    private void Start()
    {
        if (showMenuOnStart)
        {
            ShowMenu();
        }
        else
        {
            StartGame();
        }
    }

    public void ShowMenu()
    {
        Time.timeScale = 0f;
        Time.fixedDeltaTime = 0.02f;

        if (menuPanel != null)
        {
            menuPanel.SetActive(true);
        }
    }

    public void StartGame()
    {
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;

        if (menuPanel != null)
        {
            menuPanel.SetActive(false);
        }
    }

    public void QuitGame()
    {
        Time.timeScale = 1f;

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void EnsureMenuExists()
    {
        if (menuPanel == null)
        {
            CreateDefaultMenu();
        }

        if (startButton != null)
        {
            startButton.onClick.RemoveListener(StartGame);
            startButton.onClick.AddListener(StartGame);
        }

        if (quitButton != null)
        {
            quitButton.onClick.RemoveListener(QuitGame);
            quitButton.onClick.AddListener(QuitGame);
        }
    }

    private void CreateDefaultMenu()
    {
        GameObject canvasObject = new GameObject("Start Menu Canvas", typeof(RectTransform));
        generatedCanvas = canvasObject.AddComponent<Canvas>();
        generatedCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        generatedCanvas.sortingOrder = 1000;

        CanvasScaler canvasScaler = canvasObject.AddComponent<CanvasScaler>();
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasScaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();
        DontDestroyOnLoad(canvasObject);

        menuPanel = new GameObject("Menu Panel", typeof(RectTransform));
        menuPanel.transform.SetParent(canvasObject.transform, false);

        Image background = menuPanel.AddComponent<Image>();
        background.raycastTarget = false;
        Sprite menuCover = GetCoverSprite();
        if (menuCover != null)
        {
            background.sprite = menuCover;
            background.type = Image.Type.Simple;
            background.preserveAspect = false;
            background.color = Color.white;
            background.transform.SetAsFirstSibling();
            Debug.Log($"StartGameMenu: loaded cover image '{coverResourceName}' as menu background.");
        }
        else
        {
            background.color = new Color(0f, 0f, 0f, 0.75f);
            Debug.LogWarning($"StartGameMenu: could not load cover image '{coverResourceName}'. Make sure it is inside Assets/Resources and named COVER.");
        }

        RectTransform panelRect = menuPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        if (menuCover != null && coverDarkOverlayAlpha > 0f)
        {
            CreateDarkOverlay(menuPanel.transform);
        }

        startButton = CreateButton("Start Game Button", startButtonText, menuPanel.transform);
        RectTransform buttonRect = startButton.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
        buttonRect.pivot = new Vector2(0.5f, 0.5f);
        buttonRect.anchoredPosition = new Vector2(0f, -210f);
        buttonRect.sizeDelta = new Vector2(520f, 260f);

        quitButton = CreateButton("Quit Button", quitButtonText, menuPanel.transform, GetQuitButtonSprite(), Color.white);
        RectTransform quitButtonRect = quitButton.GetComponent<RectTransform>();
        quitButtonRect.anchorMin = new Vector2(0.5f, 0.5f);
        quitButtonRect.anchorMax = new Vector2(0.5f, 0.5f);
        quitButtonRect.pivot = new Vector2(0.5f, 0.5f);
        quitButtonRect.anchoredPosition = new Vector2(0f, -385f);
        quitButtonRect.sizeDelta = new Vector2(360f, 180f);
    }

    private Sprite GetCoverSprite()
    {
        return GetSprite(coverSprite, coverResourceName);
    }

    private Sprite GetButtonSprite()
    {
        return GetSprite(buttonSprite, buttonResourceName);
    }

    private Sprite GetQuitButtonSprite()
    {
        Sprite sprite = GetSprite(quitButtonSprite, quitButtonResourceName);
        return sprite != null ? sprite : GetButtonSprite();
    }

    private Sprite GetSprite(Sprite assignedSprite, string resourceName)
    {
        if (assignedSprite != null) return assignedSprite;
        if (string.IsNullOrWhiteSpace(resourceName)) return null;

        Sprite sprite = Resources.Load<Sprite>(resourceName);
        if (sprite != null) return sprite;

        Texture2D texture = Resources.Load<Texture2D>(resourceName);
        if (texture == null)
        {
            texture = LoadTextureFromFile(resourceName);
        }

        if (texture == null) return null;

        return Sprite.Create(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            100f
        );
    }

    private Texture2D LoadTextureFromFile(string resourceName)
    {
        string resourcesPath = Path.Combine(Application.dataPath, "Resources", resourceName + ".png");
        string assetsPath = Path.Combine(Application.dataPath, resourceName + ".png");

        string coverPath = File.Exists(resourcesPath) ? resourcesPath : assetsPath;
        if (!File.Exists(coverPath)) return null;

        byte[] imageBytes = File.ReadAllBytes(coverPath);
        Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);

        if (!texture.LoadImage(imageBytes))
        {
            Destroy(texture);
            return null;
        }

        texture.name = resourceName;
        return texture;
    }

    private void CreateDarkOverlay(Transform parent)
    {
        GameObject overlayObject = new GameObject("Dark Overlay", typeof(RectTransform));
        overlayObject.transform.SetParent(parent, false);

        Image overlay = overlayObject.AddComponent<Image>();
        overlay.color = new Color(0f, 0f, 0f, coverDarkOverlayAlpha);
        overlay.raycastTarget = false;

        RectTransform overlayRect = overlayObject.GetComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;
    }

    private Text CreateText(string objectName, string content, Transform parent, int fontSize, FontStyle fontStyle)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform));
        textObject.transform.SetParent(parent, false);

        Text text = textObject.AddComponent<Text>();
        text.text = content;
        text.alignment = TextAnchor.MiddleCenter;
        text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.color = Color.white;

        return text;
    }

    private Button CreateButton(string objectName, string label, Transform parent)
    {
        return CreateButton(objectName, label, parent, GetButtonSprite(), Color.black);
    }

    private Button CreateButton(string objectName, string label, Transform parent, Sprite buttonBackground, Color labelColor)
    {
        GameObject buttonObject = new GameObject(objectName, typeof(RectTransform));
        buttonObject.transform.SetParent(parent, false);

        Image buttonImage = buttonObject.AddComponent<Image>();
        if (buttonBackground != null)
        {
            buttonImage.sprite = buttonBackground;
            buttonImage.type = Image.Type.Simple;
            buttonImage.preserveAspect = true;
            buttonImage.color = Color.white;
        }
        else
        {
            buttonImage.color = new Color(0.95f, 0.7f, 0.2f, 1f);
            Debug.LogWarning($"StartGameMenu: could not load button image '{buttonResourceName}'. Make sure it is inside Assets/Resources.");
        }

        Button button = buttonObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 1f, 1f, 0.9f);
        colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;

        Text buttonLabel = CreateText("Text", label, buttonObject.transform, 42, FontStyle.Bold);
        buttonLabel.color = labelColor;

        RectTransform labelRect = buttonLabel.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        return button;
    }

    private void EnsureEventSystemExists()
    {
        if (FindObjectOfType<EventSystem>() != null) return;

        GameObject eventSystemObject = new GameObject("EventSystem");
        eventSystemObject.AddComponent<EventSystem>();
        eventSystemObject.AddComponent<StandaloneInputModule>();
        DontDestroyOnLoad(eventSystemObject);
    }

    private void OnDestroy()
    {
        if (startButton != null)
        {
            startButton.onClick.RemoveListener(StartGame);
        }

        if (quitButton != null)
        {
            quitButton.onClick.RemoveListener(QuitGame);
        }
    }
}