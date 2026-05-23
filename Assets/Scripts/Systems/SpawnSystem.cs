using System.Collections;
using System.Collections.Generic;
using Match3.Data;
using UnityEngine;
using DG.Tweening;

namespace Match3.Systems
{
    public class SpawnSystem : MonoBehaviour
    {
        public static SpawnSystem Instance { get; private set; }

        private GridSystem _grid;
        private BoardView _boardView;
        private MatchChecker _matchChecker; // 新增依赖
        private int _typeCount;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public void Initialize(GridSystem grid, BoardView boardView, int tileTypeCount)
        {
            _grid = grid;
            _boardView = boardView;
            _typeCount = tileTypeCount;
            _matchChecker = new MatchChecker(_grid); // 创建临时检查器，或从外部传入
        }

        // 重新设置 MatchChecker（由于 GridSystem 可能重建）
        public void SetMatchChecker(MatchChecker checker)
        {
            _matchChecker = checker;
        }

        private TileType GetSafeRandomType(int row, int col)
        {
            for (int attempt = 0; attempt < 10; attempt++)
            {
                var type = (TileType)Random.Range(0, _typeCount);
                if (!WouldMatch(row, col, type))
                    return type;
            }
            return (TileType)Random.Range(0, _typeCount);
        }

        private bool WouldMatch(int row, int col, TileType type)
        {
            // 复用 MatchChecker 的逻辑
            var original = _grid.Get(row, col);
            _grid.Set(row, col, new TileData(type, row, col));
            bool result = _matchChecker.HasMatchAround(row, col);
            _grid.Set(row, col, original);
            return result;
        }

        public void FillBoard()
        {
            for (int r = 0; r < _grid.Rows; r++)
            {
                for (int c = 0; c < _grid.Cols; c++)
                {
                    if (_grid.IsBlocked(r, c)) continue;
                    var type = GetSafeRandomType(r, c);
                    var data = new TileData(type, r, c);
                    _grid.Set(r, c, data);
                    _boardView.SpawnView(data);
                }
            }
        }

        public IEnumerator SpawnMissing()
        {
            var newTiles = new List<TileData>();
            for (int c = 0; c < _grid.Cols; c++)
            {
                for (int r = 0; r < _grid.Rows; r++)
                {
                    if (_grid.IsBlocked(r, c)) continue;
                    if (_grid.IsEmpty(r, c))
                    {
                        var type = GetSafeRandomType(r, c);
                        var data = new TileData(type, r, c);
                        _grid.Set(r, c, data);
                        newTiles.Add(data);
                    }
                }
            }
            if (newTiles.Count > 0)
            {
                var tween = _boardView.AnimateSpawn(newTiles);
                yield return tween.WaitForCompletion();
            }
        }
    }
}