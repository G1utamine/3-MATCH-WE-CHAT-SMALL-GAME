using UnityEngine;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("音频源")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource[] sfxSources;

    [Header("音频剪辑")]
    [SerializeField] private AudioClip bgmMenu;
    [SerializeField] private AudioClip bgmGame;
    [SerializeField] private AudioClip clickSfx;
    [SerializeField] private AudioClip swapSfx;
    [SerializeField] private AudioClip matchSfx;
    [SerializeField] private AudioClip dropSfx;
    [SerializeField] private AudioClip winSfx;
    [SerializeField] private AudioClip loseSfx;
    [SerializeField] private AudioClip itemSfx;
    [Header("道具音效")]
    [SerializeField] private AudioClip explosionSfx;      // 炸弹爆炸
    [SerializeField] private AudioClip lightningStrikeSfx; // 闪电击中
    [SerializeField] private AudioClip potionBreakSfx;     // 药水破碎
    [Header("音量设置")]
    [Range(0f, 1f)] public float bgmVolume = 0.3f;
    [Range(0f, 1f)] public float sfxVolume = 0.5f;

    [Header("音效轮询设置")]
    [SerializeField] private int sfxSourceCount = 4;

    [Header("下落音效冷却")]
    [SerializeField] private float dropSfxCooldown = 0.08f; // 每 0.08 秒最多播放一次

    private string currentScene;
    private int sfxIndex = 0;
    private float lastDropTime = -1f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (bgmSource == null) bgmSource = gameObject.AddComponent<AudioSource>();
        bgmSource.loop = true;
        bgmSource.playOnAwake = false;

        if (sfxSources == null || sfxSources.Length == 0)
        {
            sfxSources = new AudioSource[sfxSourceCount];
            for (int i = 0; i < sfxSourceCount; i++)
            {
                sfxSources[i] = gameObject.AddComponent<AudioSource>();
                sfxSources[i].playOnAwake = false;
            }
        }

        LoadVolume();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        Scene currentSceneObj = SceneManager.GetActiveScene();
        OnSceneLoaded(currentSceneObj, LoadSceneMode.Single);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        string sceneName = scene.name;
        if (currentScene == sceneName) return;
        currentScene = sceneName;

        if (sceneName == "MainMenu")
        {
            PlayBGM(bgmMenu);
        }
        else if (sceneName == "GameScene")
        {
            PlayBGM(bgmGame);
        }
    }

    public void PlayBGM(AudioClip clip)
    {
        if (clip == null) return;
        if (bgmSource.clip == clip && bgmSource.isPlaying) return;
        bgmSource.clip = clip;
        bgmSource.volume = bgmVolume;
        bgmSource.Play();
    }

    public void PlaySFX(AudioClip clip, float pitch = 1f)
    {
        if (clip == null) return;

        var source = sfxSources[sfxIndex];
        source.pitch = pitch;
        source.PlayOneShot(clip, sfxVolume);

        sfxIndex = (sfxIndex + 1) % sfxSources.Length;
    }

    // 带冷却的下落音效（防止叠加）
    public void PlayDrop()
    {
        if (dropSfx == null) return;

        float now = Time.time;
        if (lastDropTime >= 0 && now - lastDropTime < dropSfxCooldown) return;

        lastDropTime = now;
        PlaySFX(dropSfx);
    }

    public void SetBGMVolume(float vol)
    {
        bgmVolume = Mathf.Clamp01(vol);
        if (bgmSource != null) bgmSource.volume = bgmVolume;
        PlayerPrefs.SetFloat("BGMVolume", bgmVolume);
    }

    public void SetSFXVolume(float vol)
    {
        sfxVolume = Mathf.Clamp01(vol);
        PlayerPrefs.SetFloat("SFXVolume", sfxVolume);
    }

    private void LoadVolume()
    {
        bgmVolume = PlayerPrefs.GetFloat("BGMVolume", 0.3f);
        sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 0.5f);
        if (bgmSource) bgmSource.volume = bgmVolume;
    }

    // ========== 便捷调用方法 ==========
    public void ButtonClick()
    {
        PlaySFX(clickSfx);
    }

    public void PlaySwap() => PlaySFX(swapSfx);
    public void PlayMatch() => PlaySFX(matchSfx);
    // public void PlayDrop() => PlaySFX(dropSfx); // 已被上面的带冷却版本替代
    public void PlayWin() => PlaySFX(winSfx);
    public void PlayLose() => PlaySFX(loseSfx);
    public void PlayItem() => PlaySFX(itemSfx);
    // 道具生效音效
    public void PlayExplosion() => PlaySFX(explosionSfx);
    public void PlayLightningStrike() => PlaySFX(lightningStrikeSfx);
    public void PlayPotionBreak() => PlaySFX(potionBreakSfx);
}