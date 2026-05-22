using UnityEngine;
using System.Collections;

public class BGMManager : MonoBehaviour
{
    public static BGMManager Instance;

    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private float fadeDuration = 0.8f;
    [SerializeField] [Range(0f, 1f)] private float targetVolume = 1f;

    private Coroutine fadeCoroutine;
    private AudioClip currentClip;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        // 如果你要跨場景保留音樂，可打開這行
        // DontDestroyOnLoad(gameObject);

        // 自動取得 AudioSource（如果沒有在 Inspector 指定）
        if (bgmSource == null)
        {
            bgmSource = GetComponent<AudioSource>();
            if (bgmSource == null)
            {
                bgmSource = gameObject.AddComponent<AudioSource>();
                bgmSource.loop = true;
                bgmSource.playOnAwake = false;
                Debug.LogWarning("[BGMManager] 沒有在 Inspector 指定 AudioSource，已自動建立。請確認 AudioSource 設定正確。", this);
            }
        }

        // 確保 AudioSource 不會在 Awake 就播放（Play On Awake 應關閉）
        bgmSource.playOnAwake = false;

        // fadeDuration 保護：避免除以零
        if (fadeDuration <= 0f)
        {
            Debug.LogWarning("[BGMManager] fadeDuration 不可小於或等於 0，已重設為 0.8。", this);
            fadeDuration = 0.8f;
        }
    }

    public void PlayBGM(AudioClip newClip)
    {
        if (bgmSource == null)
        {
            Debug.LogError("[BGMManager] bgmSource 是 null！請在 BGMManager GameObject 上加上 AudioSource 並指定到 bgmSource 欄位。", this);
            return;
        }

        if (newClip == null)
        {
            Debug.LogWarning("[BGMManager] PlayBGM 收到 null AudioClip，請確認 RoomMusicTrigger 的 roomBGM 欄位有指定音樂。", this);
            return;
        }

        if (currentClip == newClip)
        {
            Debug.Log($"[BGMManager] 已在播放 {newClip.name}，跳過切換。");
            return;
        }

        Debug.Log($"[BGMManager] 切換 BGM → {newClip.name}");
        currentClip = newClip;

        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        fadeCoroutine = StartCoroutine(FadeToNewClip(newClip));
    }

    private IEnumerator FadeToNewClip(AudioClip newClip)
    {
        // --- Fade Out ---
        if (bgmSource.isPlaying)
        {
            float startVol = bgmSource.volume;
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                bgmSource.volume = Mathf.Lerp(startVol, 0f, elapsed / fadeDuration);
                yield return null;
            }
        }

        bgmSource.volume = 0f;
        bgmSource.Stop();
        bgmSource.clip = newClip;
        bgmSource.Play();

        // --- Fade In ---
        float fadeElapsed = 0f;
        while (fadeElapsed < fadeDuration)
        {
            fadeElapsed += Time.deltaTime;
            bgmSource.volume = Mathf.Lerp(0f, targetVolume, fadeElapsed / fadeDuration);
            yield return null;
        }

        bgmSource.volume = targetVolume;
        Debug.Log($"[BGMManager] BGM 播放完成：{newClip.name}，音量：{targetVolume}");
    }

    /// <summary>停止BGM並淡出</summary>
    public void StopBGM()
    {
        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        fadeCoroutine = StartCoroutine(FadeOut());
    }

    private IEnumerator FadeOut()
    {
        float startVol = bgmSource.volume;
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            bgmSource.volume = Mathf.Lerp(startVol, 0f, elapsed / fadeDuration);
            yield return null;
        }
        bgmSource.Stop();
        bgmSource.volume = 0f;
        currentClip = null;
    }
}
