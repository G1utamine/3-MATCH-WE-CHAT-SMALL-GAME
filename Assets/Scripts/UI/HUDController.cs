using Match3.Systems;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Match3.UI
{
    public class HUDController : MonoBehaviour
    {
        public static HUDController Instance { get; private set; }

        [Header("分数进度条")]
        [SerializeField] private Slider _scoreSlider;
        [SerializeField] private TextMeshProUGUI _scoreText;

        [Header("HUD 星星")]
        [SerializeField] private GameObject _star1On;
        [SerializeField] private GameObject _star2On;
        [SerializeField] private GameObject _star3On;

        [Header("步数")]
        [SerializeField] private TextMeshProUGUI _movesText;

        [Header("暂停按钮")]
        [SerializeField] private Button _stopButton;

        [Header("StopPanel")]
        [SerializeField] private GameObject _stopPanel;
        [SerializeField] private Button _stopRetryButton;
        [SerializeField] private Button _quitButton;
        [SerializeField] private Slider _sfxSlider;
        [SerializeField] private Slider _bgmSlider;

        [Header("ResultPanel")]
        [SerializeField] private GameObject _resultPanel;
        [SerializeField] private TextMeshProUGUI _resultTitleText;
        [SerializeField] private TextMeshProUGUI _resultScoreText;
        [SerializeField] private GameObject _resultStar1On;
        [SerializeField] private GameObject _resultStar2On;
        [SerializeField] private GameObject _resultStar3On;
        [SerializeField] private Button _resultRetryButton;
        [SerializeField] private Button _nextButton;

        private int _targetScore;
        // 星星状态缓存
        private bool _star1Active, _star2Active, _star3Active;
        private bool _resultStar1Active, _resultStar2Active, _resultStar3Active;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            _stopPanel.SetActive(false);
            _resultPanel.SetActive(false);

            _stopButton.onClick.AddListener(OpenStopPanel);
            _stopRetryButton.onClick.AddListener(RetryGame);
            _quitButton.onClick.AddListener(OnQuitClicked);
            _sfxSlider.onValueChanged.AddListener(OnSfxChanged);
            _bgmSlider.onValueChanged.AddListener(OnBgmChanged);
            _resultRetryButton.onClick.AddListener(RetryGame);
            _nextButton.onClick.AddListener(OnNextClicked);
        }

        private void Start()
        {
            ScoreSystem.Instance.OnScoreChanged += UpdateScore;
            LevelSystem.Instance.OnMovesChanged += UpdateMoves;
            RefreshHUD();
        }

        private void OnDestroy()
        {
            if (ScoreSystem.Instance != null)
                ScoreSystem.Instance.OnScoreChanged -= UpdateScore;
            if (LevelSystem.Instance != null)
                LevelSystem.Instance.OnMovesChanged -= UpdateMoves;
        }

        public void RefreshHUD()
        {
            _resultPanel.SetActive(false);
            _stopPanel.SetActive(false);
            _targetScore = LevelSystem.Instance.Config.TargetScore;
            _scoreSlider.minValue = 0;
            _scoreSlider.maxValue = _targetScore;
            UpdateScore(0);
            UpdateMoves(LevelSystem.Instance.MovesRemaining);
            // 重置星星缓存
            _star1Active = _star2Active = _star3Active = false;
            _resultStar1Active = _resultStar2Active = _resultStar3Active = false;
        }

        private void UpdateScore(int score)
        {
            _scoreSlider.value = Mathf.Min(score, _targetScore);
            _scoreText.text = $"{score}";

            float ratio = (float)score / _targetScore;
            SetStar(_star1On, ratio >= 0.2f, ref _star1Active);
            SetStar(_star2On, ratio >= 0.5f, ref _star2Active);
            SetStar(_star3On, ratio >= 0.8f, ref _star3Active);
        }

        private void UpdateMoves(int moves)
        {
            _movesText.text = $"{moves}";
        }

        private void SetStar(GameObject starOn, bool on, ref bool cache)
        {
            if (starOn == null) return;
            if (cache == on) return;
            cache = on;
            starOn.SetActive(on);
        }

        private void OpenStopPanel()
        {
            AudioManager.Instance?.ButtonClick(); // 添加按钮音效
            bool isOpen = _stopPanel.activeSelf;
            _stopPanel.SetActive(!isOpen);
            Time.timeScale = isOpen ? 1f : 0f;
        }

        private void OnQuitClicked()
        {
            AudioManager.Instance?.ButtonClick(); // 添加按钮音效
            Time.timeScale = 1f;
            SceneManager.LoadScene("MainMenu");
        }

        public void ShowResult(bool won)
        {
            _resultPanel.SetActive(true);
            _resultTitleText.text = won ? "通关" : "失败";
            _resultScoreText.text = $"{ScoreSystem.Instance.CurrentScore}";

            // 互斥显示：胜利显示下一关，失败显示重试
            _nextButton.gameObject.SetActive(won);
            _resultRetryButton.gameObject.SetActive(!won);

            float ratio = (float)ScoreSystem.Instance.CurrentScore / _targetScore;
            SetStar(_resultStar1On, ratio >= 0.2f, ref _resultStar1Active);
            SetStar(_resultStar2On, ratio >= 0.5f, ref _resultStar2Active);
            SetStar(_resultStar3On, ratio >= 0.8f, ref _resultStar3Active);
        }

        private void OnNextClicked()
        {
            AudioManager.Instance?.ButtonClick(); // 添加按钮音效
            _resultPanel.SetActive(false);
            Time.timeScale = 1f;
            GameManager.Instance?.EnterNextLevel();
        }

        private void RetryGame()
        {
            AudioManager.Instance?.ButtonClick(); // 添加按钮音效
            Time.timeScale = 1f;
            _stopPanel.SetActive(false);
            _resultPanel.SetActive(false);
            GameManager.Instance?.RestartCurrentLevel();
        }

        private void OnSfxChanged(float value)
        {
            AudioManager.Instance?.SetSFXVolume(value);
        }

        private void OnBgmChanged(float value)
        {
            AudioManager.Instance?.SetBGMVolume(value);
        }
    }
}