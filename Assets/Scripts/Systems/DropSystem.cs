using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

namespace Match3.Systems
{
    /// <summary>
    /// Computes and executes the downward gravity pass.
    /// Tiles above empty cells fall to fill them.
    /// </summary>
    public class DropSystem : MonoBehaviour
    {
        public static DropSystem Instance { get; private set; }

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

        public IEnumerator ApplyGravity()
        {
            AudioManager.Instance?.PlayDrop();
            var dropMap = new Dictionary<Vector2Int, Vector2Int>();

            for (int c = 0; c < _grid.Cols; c++)
            {
                int emptyRow = -1;
                for (int r = 0; r < _grid.Rows; r++)
                {
                    if (_grid.IsBlocked(r, c)) continue; // ¡û ¼ÓÕâÐÐ

                    if (_grid.IsEmpty(r, c))
                    {
                        if (emptyRow == -1) emptyRow = r;
                    }
                    else if (emptyRow != -1)
                    {
                        var tile = _grid.Get(r, c);
                        dropMap[new Vector2Int(emptyRow, c)] = new Vector2Int(r, c);
                        _grid.Set(emptyRow, c, tile);
                        _grid.Clear(r, c);
                        emptyRow++;
                    }
                }
            }

            if (dropMap.Count > 0)
            {
                var tween = _boardView.AnimateDrop(dropMap);
                yield return tween.WaitForCompletion();
            }
        }
    }
}
