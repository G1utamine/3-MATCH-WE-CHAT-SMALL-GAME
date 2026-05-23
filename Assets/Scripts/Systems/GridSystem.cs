using System.Collections.Generic;
using Match3.Data;
using UnityEngine;

namespace Match3.Systems
{
    /// <summary>
    /// Owns the raw data grid. Does NOT know about GameObjects or visuals.
    /// Every system reads/writes tile data through here.
    /// </summary>
    public class GridSystem : MonoBehaviour
    {
        public static GridSystem Instance { get; private set; }

        [HideInInspector] public int Rows;
        [HideInInspector] public int Cols;

        private TileData[,] _grid;
        private bool[,] _isBlocked; // 预计算屏蔽格

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public void Initialize(int rows, int cols)
        {
            Rows = rows;
            Cols = cols;
            _grid = new TileData[rows, cols];
            PrecomputeBlockedCells();
        }

        private void PrecomputeBlockedCells()
        {
            _isBlocked = new bool[Rows, Cols];
            int cornerSize = 1; // 与 IsBlocked 逻辑一致
            for (int r = 0; r < Rows; r++)
            {
                for (int c = 0; c < Cols; c++)
                {
                    bool topLeft = r >= Rows - cornerSize && c < cornerSize;
                    bool topRight = r >= Rows - cornerSize && c >= Cols - cornerSize;
                    bool bottomLeft = r < cornerSize && c < cornerSize;
                    bool bottomRight = r < cornerSize && c >= Cols - cornerSize;
                    _isBlocked[r, c] = topLeft || topRight || bottomLeft || bottomRight;
                }
            }
        }

        public TileData Get(int row, int col)
        {
            if (!InBounds(row, col)) return null;
            return _grid[row, col];
        }

        public void Set(int row, int col, TileData data)
        {
            if (!InBounds(row, col)) return;
            _grid[row, col] = data;
            if (data != null)
            {
                data.Row = row;
                data.Col = col;
            }
        }

        public bool InBounds(int row, int col) =>
            row >= 0 && row < Rows && col >= 0 && col < Cols;

        public bool IsEmpty(int row, int col) =>
            InBounds(row, col) && (_grid[row, col] == null || _grid[row, col].State == TileState.Empty);

        /// <summary>Returns all empty positions (bottom-up, left-right).</summary>
        public List<Vector2Int> GetEmptyPositions()
        {
            var result = new List<Vector2Int>();
            for (int r = 0; r < Rows; r++)
                for (int c = 0; c < Cols; c++)
                    if (IsEmpty(r, c))
                        result.Add(new Vector2Int(r, c));
            return result;
        }

        public void Swap(int r1, int c1, int r2, int c2)
        {
            if (!InBounds(r1, c1) || !InBounds(r2, c2))
            {
                Debug.LogWarning($"[GridSystem] Swap 越界：({r1},{c1}) <-> ({r2},{c2})");
                return;
            }

            var tmp = _grid[r1, c1];
            _grid[r1, c1] = _grid[r2, c2];
            _grid[r2, c2] = tmp;

            if (_grid[r1, c1] != null) { _grid[r1, c1].Row = r1; _grid[r1, c1].Col = c1; }
            if (_grid[r2, c2] != null) { _grid[r2, c2].Row = r2; _grid[r2, c2].Col = c2; }
        }

        public void Clear(int row, int col)
        {
            _grid[row, col] = null;
        }

        public void DebugPrint()
        {
            string s = "Grid:\n";
            for (int r = Rows - 1; r >= 0; r--)
            {
                for (int c = 0; c < Cols; c++)
                    s += (_grid[r, c] != null ? ((int)_grid[r, c].Type).ToString() : "_") + " ";
                s += "\n";
            }
        }

        /// <summary>该格子是否是被屏蔽的角落（不参与游戏）</summary>
        public bool IsBlocked(int row, int col)
        {
            if (!InBounds(row, col)) return true;
            return _isBlocked[row, col];
        }
    }
}