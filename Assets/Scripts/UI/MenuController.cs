using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

#if UNITY_WEBGL && !UNITY_EDITOR
using WeChatWASM;
#endif

namespace Match3.Systems
{
    public class MenuController : MonoBehaviour
    {
        [Header("按钮")]
        [SerializeField] private Button newGameButton;
        [SerializeField] private Button continueGameButton;
        [SerializeField] private Button quitGameButton;

        [Header("道具格子数据 (Number)")]
        [SerializeField] private TMP_Text bombNumberText;
        [SerializeField] private TMP_Text lightningNumberText;
        [SerializeField] private TMP_Text potionNumberText;

        [Header("当前关卡")]
        [SerializeField] private TMP_Text currentLevelText;

        [Header("场景名称")]
        [SerializeField] private string gameSceneName = "GameScene";

        private void Start()
        {
            // 只保留带音效的监听（一个按钮一个监听）
            newGameButton.onClick.AddListener(() => { AudioManager.Instance?.ButtonClick(); NewGame(); });
            continueGameButton.onClick.AddListener(() => { AudioManager.Instance?.ButtonClick(); ContinueGame(); });
            quitGameButton.onClick.AddListener(() => { AudioManager.Instance?.ButtonClick(); QuitGame(); });

            RefreshData();
        }

        private void NewGame()
        {
            DataManager.Instance.StartNewGame();

            if (UnityEngine.EventSystems.EventSystem.current != null)
            {
                UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
            }

            SceneManager.LoadScene(gameSceneName);
        }

        private void ContinueGame()
        {
            if (UnityEngine.EventSystems.EventSystem.current != null)
            {
                UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
            }

            SceneManager.LoadScene(gameSceneName);
        }

        private void QuitGame()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
    // 微信小游戏环境：调用微信退出API
    WX.ExitMiniProgram(new ExitMiniProgramOption
    {
        success = (res) =>
        {
            Debug.Log("[Menu] 游戏退出成功");
        },
        fail = (err) =>
        {
            Debug.LogError($"[Menu] 游戏退出失败: {err.errMsg}");
        }
    });
#else
            // 编辑器或其他平台：使用 Application.Quit
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
#endif
        }

        private void RefreshData()
        {
            bombNumberText.text = DataManager.Instance.BombCount.ToString();
            lightningNumberText.text = DataManager.Instance.LightningCount.ToString();
            potionNumberText.text = DataManager.Instance.PotionCount.ToString();
            currentLevelText.text = "关卡:" + DataManager.Instance.CurrentLevel;
        }
    }
}