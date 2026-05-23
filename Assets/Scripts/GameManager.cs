using DG.Tweening;
using Match3.Core;
using Match3.Data;
using Match3.Systems;
using Match3.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Match3
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("多关卡配置")]
        [SerializeField] private List<LevelConfig> _levels = new List<LevelConfig>();

        [Header("System References")]
        [SerializeField] private GameFlowController _flowController;
        [SerializeField] private GridSystem _gridSystem;
        [SerializeField] private BoardView _boardView;
        [SerializeField] private SwapSystem _swapSystem;
        [SerializeField] private ClearSystem _clearSystem;
        [SerializeField] private DropSystem _dropSystem;
        [SerializeField] private SpawnSystem _spawnSystem;
        [SerializeField] private CascadeSystem _cascadeSystem;
        [SerializeField] private InputController _inputController;
        [SerializeField] private ScoreSystem _scoreSystem;
        [SerializeField] private LevelSystem _levelSystem;
        [SerializeField] private ItemManager _itemManager;

        private int _currentLevelIndex;
        private MatchChecker _matchChecker;
        private bool _isInitialized;

        // ─────────────────────────────────────────────────────────────

        private void Awake()
        {
            Instance = this;
            DOTween.SetTweensCapacity(3000, 200);
            DOTween.defaultRecyclable = true;  // 新增
        }

        private void Start()
        {
            _currentLevelIndex = Mathf.Clamp(
                DataManager.Instance.CurrentLevel - 1, 0, _levels.Count - 1);

            LoadLevel(_currentLevelIndex);
            StartGame();
        }

        private void OnDestroy()
        {
            if (_swapSystem != null)
            {
                _swapSystem.OnSwapSuccess -= OnSwapSuccess;
                _swapSystem.OnSwapFailed -= OnSwapFailed;
            }
            if (_itemManager != null)
            {
                _itemManager.OnTilesDestroyed -= OnItemDestroyTiles;
                _itemManager.OnTilesModified -= OnItemModifyTiles;
            }
        }

        // ── 关卡加载 ─────────────────────────────────────────────────

        public void LoadLevel(int levelIndex)
        {
            if (_levels == null || _levels.Count == 0)
            {
                Debug.LogError("[GameManager] 关卡列表为空！");
                return;
            }

            if (levelIndex >= _levels.Count)
            {
                _currentLevelIndex = 0;
                levelIndex = 0;
                DataManager.Instance.CurrentLevel = 1;
            }

            var cfg = _levels[levelIndex];
            int rows = cfg.Rows;
            int cols = cfg.Cols;

            _gridSystem.Initialize(rows, cols);
            _boardView.Initialize(rows, cols);
            _matchChecker = new MatchChecker(_gridSystem);
            _scoreSystem.Initialize(cfg);
            _levelSystem.Initialize(cfg, _flowController);

            // ✅ 只初始化一次 SpawnSystem，并传入 MatchChecker
            _spawnSystem.Initialize(_gridSystem, _boardView, cfg.TileTypeCount);
            _spawnSystem.SetMatchChecker(_matchChecker);

            if (!_isInitialized)
            {
                // 交换系统
                _swapSystem.Initialize(_gridSystem, _boardView, _matchChecker, _flowController);
                _swapSystem.OnSwapSuccess += OnSwapSuccess;
                _swapSystem.OnSwapFailed += OnSwapFailed;

                // 消除/下落
                _clearSystem.Initialize(_gridSystem, _boardView);
                _dropSystem.Initialize(_gridSystem, _boardView);

                // 连锁
                _cascadeSystem.Initialize(
                    _gridSystem, _boardView, _matchChecker,
                    _clearSystem, _dropSystem, _spawnSystem, _flowController);

                // 输入
                _inputController.Initialize(_flowController, _boardView, _swapSystem);

                // 关卡事件
                _levelSystem.OnWin += () => HUDController.Instance?.ShowResult(true);
                _levelSystem.OnLose += () => HUDController.Instance?.ShowResult(false);

                // 道具事件
                if (_itemManager != null)
                {
                    _itemManager.OnTilesDestroyed += OnItemDestroyTiles;
                    _itemManager.OnTilesModified += OnItemModifyTiles;
                }

                _isInitialized = true;
            }

            HUDController.Instance?.RefreshHUD();

        }

        public void StartGame()
        {
            _spawnSystem.FillBoard();
            _flowController.SetState(GameState.Idle);
        }

        // ── 关卡切换 ─────────────────────────────────────────────────

        public void EnterNextLevel()
        {
            _boardView.ClearAllTiles();
            _currentLevelIndex++;
            DataManager.Instance.CurrentLevel = _currentLevelIndex + 1;
            DataManager.Instance.SaveAll();
            LoadLevel(_currentLevelIndex);
            StartGame();
        }

        public void RestartCurrentLevel()
        {
            _boardView.ClearAllTiles();
            DataManager.Instance.SaveAll();
            LoadLevel(_currentLevelIndex);
            StartGame();
        }

        // ── 交换回调 ─────────────────────────────────────────────────

        private void OnSwapSuccess()
        {
            _levelSystem.ConsumeMove();
            _cascadeSystem.StartCascade();
        }

        private void OnSwapFailed()
        {
            _flowController.SetState(GameState.Idle);
        }

        // ── 道具回调 ─────────────────────────────────────────────────

        private void OnItemDestroyTiles(List<Vector2Int> clearedPositions)
        {
            _levelSystem.ConsumeMove();
            StartCoroutine(ItemDestroyRoutine(clearedPositions));
        }

        private void OnItemModifyTiles(List<Vector2Int> modifiedPositions, TileType newType)
        {
            _levelSystem.ConsumeMove();
            foreach (var pos in modifiedPositions)
            {
                var tile = _gridSystem.Get(pos.x, pos.y);
                if (tile != null) tile.Type = newType;
                _boardView.ChangeTileTypeAt(pos.x, pos.y, newType);
            }
            StartCoroutine(ItemModifyRoutine());
        }

        // ── 道具流程协程 ─────────────────────────────────────────────

        private IEnumerator ItemDestroyRoutine(List<Vector2Int> clearedPositions)
        {
            _flowController.SetState(GameState.Clearing);
            yield return StartCoroutine(
                _clearSystem.ClearMatches(new HashSet<Vector2Int>(clearedPositions), 1));

            _flowController.SetState(GameState.Dropping);
            yield return StartCoroutine(_dropSystem.ApplyGravity());

            _flowController.SetState(GameState.Spawning);
            yield return StartCoroutine(_spawnSystem.SpawnMissing());

            yield return StartCoroutine(CascadeAndFinish());
        }

        private IEnumerator ItemModifyRoutine()
        {
            _flowController.SetState(GameState.Checking);
            yield return new WaitForSeconds(0.1f);
            yield return StartCoroutine(CascadeAndFinish());
        }

        /// <summary>道具触发后的连锁检测，结束后检查关卡条件</summary>
        private IEnumerator CascadeAndFinish()
        {
            int cascade = 1;
            while (true)
            {
                _flowController.SetState(GameState.Checking);
                var matches = _matchChecker.FindAllMatches();
                if (matches.Count == 0) break;

                _flowController.SetState(GameState.Clearing);
                yield return StartCoroutine(_clearSystem.ClearMatches(matches, cascade));

                _flowController.SetState(GameState.Dropping);
                yield return StartCoroutine(_dropSystem.ApplyGravity());

                _flowController.SetState(GameState.Spawning);
                yield return StartCoroutine(_spawnSystem.SpawnMissing());

                cascade++;
            }

            _levelSystem.CheckConditions();

            if (_flowController.CurrentState != GameState.GameOver)
                _flowController.SetState(GameState.Idle);
        }
    }
}