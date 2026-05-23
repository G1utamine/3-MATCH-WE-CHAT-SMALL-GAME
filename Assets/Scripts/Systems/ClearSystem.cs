using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Match3.Core;
using Match3.Data;
using UnityEngine;

namespace Match3.Systems
{
    public class ClearSystem : MonoBehaviour
    {
        public static ClearSystem Instance { get; private set; }

        private GridSystem _grid;
        private BoardView _boardView;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public void Initialize(GridSystem grid, BoardView boardView)
        {
            _grid = grid;
            _boardView = boardView;
        }

        /// <summary>Clears positions, returns count cleared.</summary>
        public IEnumerator ClearMatches(HashSet<Vector2Int> positions, int cascadeLevel)
        {
            // 标记为待清除
            foreach (var pos in positions)
            {
                var tile = _grid.Get(pos.x, pos.y);
                if (tile != null) tile.State = TileState.PendingClear;
            }

            // 加分数
            ScoreSystem.Instance?.AddScore(positions.Count, cascadeLevel);

            // ✅ 只播放一次消除音效
            AudioManager.Instance?.PlayMatch();

            // 播放消除特效
            if (EffectManager.Instance != null)
            {
                foreach (var pos in positions)
                {
                    var tile = _grid.Get(pos.x, pos.y);
                    if (tile == null) continue;
                    Vector3 worldPos = BoardView.Instance.GridToWorld(pos.x, pos.y);
                    EffectManager.Instance.PlayClearEffect(worldPos, tile.Type);
                }
            }

            // 播放消除动画
            var posList = new List<Vector2Int>(positions);
            var tween = _boardView.AnimateClear(posList);
            yield return tween.WaitForCompletion();

            // 清除数据
            foreach (var pos in positions)
                _grid.Clear(pos.x, pos.y);
        }
    }
}