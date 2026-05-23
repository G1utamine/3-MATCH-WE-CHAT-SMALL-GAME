using DG.Tweening;
using Match3.Core;
using System.Collections;
using UnityEngine;

namespace Match3.Systems
{
    public class CascadeSystem : MonoBehaviour
    {
        public static CascadeSystem Instance { get; private set; }

        private GridSystem _grid;
        private BoardView _boardView;
        private MatchChecker _matchChecker;
        private ClearSystem _clearSystem;
        private DropSystem _dropSystem;
        private SpawnSystem _spawnSystem;
        private GameFlowController _flow;
        private DeadlockChecker _deadlockChecker;

        [SerializeField] private int _maxShuffleAttempts = 10;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public void Initialize(
            GridSystem grid,
            BoardView boardView,
            MatchChecker matchChecker,
            ClearSystem clearSystem,
            DropSystem dropSystem,
            SpawnSystem spawnSystem,
            GameFlowController flow)
        {
            _grid = grid;
            _boardView = boardView;
            _matchChecker = matchChecker;
            _clearSystem = clearSystem;
            _dropSystem = dropSystem;
            _spawnSystem = spawnSystem;
            _flow = flow;
            _deadlockChecker = new DeadlockChecker(grid, matchChecker);
        }

        public void StartCascade()
        {
            StartCoroutine(CascadeLoop());
        }

        private IEnumerator CascadeLoop()
        {
            int cascadeLevel = 0;

            while (true)
            {
                _flow.SetState(GameState.Checking);
                var matches = _matchChecker.FindAllMatches();
                if (matches.Count == 0) break;

                _flow.SetState(GameState.Clearing);
                yield return StartCoroutine(_clearSystem.ClearMatches(matches, cascadeLevel));

                _flow.SetState(GameState.Dropping);
                yield return StartCoroutine(_dropSystem.ApplyGravity());

                _flow.SetState(GameState.Spawning);
                yield return StartCoroutine(_spawnSystem.SpawnMissing());

                cascadeLevel++;
                yield return null;
            }

            // Cascade 结束后检测无解
            yield return StartCoroutine(HandleDeadlockIfNeeded());

            LevelSystem.Instance?.CheckConditions();
            if (!_flow.IsGameOver)
                _flow.SetState(GameState.Idle);
        }

        private IEnumerator HandleDeadlockIfNeeded()
        {
            int attempts = 0;
            while (!_deadlockChecker.HasValidMove() && attempts < _maxShuffleAttempts)
            {
                yield return StartCoroutine(ShuffleBoard());
                attempts++;
            }

            if (attempts >= _maxShuffleAttempts)
                Debug.LogWarning("[Deadlock] 多次洗牌仍无解，可能颜色种类太少");
        }

        private IEnumerator ShuffleBoard()
        {
            // 收集所有有效Tile的数据
            var allTiles = new System.Collections.Generic.List<Match3.Data.TileData>();
            for (int r = 0; r < _grid.Rows; r++)
                for (int c = 0; c < _grid.Cols; c++)
                {
                    if (_grid.IsBlocked(r, c)) continue;
                    var tile = _grid.Get(r, c);
                    if (tile != null) allTiles.Add(tile);
                }

            // Fisher-Yates 洗牌
            for (int i = allTiles.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                var tmp = allTiles[i];
                allTiles[i] = allTiles[j];
                allTiles[j] = tmp;
            }

            // 把洗完的类型写回格子
            int index = 0;
            for (int r = 0; r < _grid.Rows; r++)
            {
                for (int c = 0; c < _grid.Cols; c++)
                {
                    if (_grid.IsBlocked(r, c)) continue;
                    var tile = _grid.Get(r, c);
                    if (tile == null) continue;
                    tile.Type = allTiles[index].Type;
                    _boardView.ChangeTileTypeAt(r, c, tile.Type);
                    index++;
                }
            }

            // 洗牌动画：全部抖一下
            yield return ShuffleAnimation();
        }

        private IEnumerator ShuffleAnimation()
        {
            var seq = DG.Tweening.DOTween.Sequence();
            for (int r = 0; r < _grid.Rows; r++)
                for (int c = 0; c < _grid.Cols; c++)
                {
                    var view = _boardView.GetView(r, c);
                    if (view == null) continue;
                    seq.Join(view.transform
                        .DOPunchScale(Vector3.one * 0.2f, 0.3f, 5, 0.5f));
                }
            yield return seq.WaitForCompletion();
        }
    }
}