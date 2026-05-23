using System.Collections.Generic;
using Match3.Data;
using UnityEngine;

namespace Match3.Systems
{
    public class MatchChecker
    {
        private readonly GridSystem _grid;
        private static readonly HashSet<Vector2Int> _matchesBuffer = new HashSet<Vector2Int>();

        public MatchChecker(GridSystem grid)
        {
            _grid = grid;
        }

        public bool HasAnyMatch() => FindAllMatches().Count > 0;

        public bool WouldSwapMatch(int r1, int c1, int r2, int c2)
        {
            _grid.Swap(r1, c1, r2, c2);
            bool result = HasMatchAround(r1, c1) || HasMatchAround(r2, c2);
            _grid.Swap(r1, c1, r2, c2);
            return result;
        }

        /// <summary>检测某个格子是否存在三消（基于当前棋盘）。</summary>
        public bool HasMatchAround(int row, int col)
        {
            var tile = _grid.Get(row, col);
            if (tile == null) return false;
            TileType type = tile.Type;

            int hCount = 1;
            for (int dc = 1; dc <= 2; dc++)
            {
                if (!MatchesType(row, col + dc, type)) break;
                hCount++;
            }
            for (int dc = 1; dc <= 2; dc++)
            {
                if (!MatchesType(row, col - dc, type)) break;
                hCount++;
            }
            if (hCount >= 3) return true;

            int vCount = 1;
            for (int dr = 1; dr <= 2; dr++)
            {
                if (!MatchesType(row + dr, col, type)) break;
                vCount++;
            }
            for (int dr = 1; dr <= 2; dr++)
            {
                if (!MatchesType(row - dr, col, type)) break;
                vCount++;
            }
            return vCount >= 3;
        }

        private bool MatchesType(int row, int col, TileType type)
        {
            if (!_grid.InBounds(row, col)) return false;
            if (_grid.IsBlocked(row, col)) return false;
            var tile = _grid.Get(row, col);
            return tile != null && tile.Type == type && tile.State != TileState.Empty;
        }

        public HashSet<Vector2Int> FindAllMatches()
        {
            _matchesBuffer.Clear();

            // 横向
            for (int r = 0; r < _grid.Rows; r++)
            {
                for (int c = 0; c <= _grid.Cols - 3; c++)
                {
                    if (_grid.IsBlocked(r, c)) continue;
                    var tile = _grid.Get(r, c);
                    if (tile == null || tile.State == TileState.Empty) continue;

                    int len = 1;
                    while (c + len < _grid.Cols &&
                           !_grid.IsBlocked(r, c + len) &&
                           _grid.Get(r, c + len)?.Type == tile.Type &&
                           _grid.Get(r, c + len)?.State != TileState.Empty)
                        len++;

                    if (len >= 3)
                    {
                        for (int i = 0; i < len; i++)
                            _matchesBuffer.Add(new Vector2Int(r, c + i));
                        c += len - 1;
                    }
                }
            }

            // 纵向
            for (int c = 0; c < _grid.Cols; c++)
            {
                for (int r = 0; r <= _grid.Rows - 3; r++)
                {
                    if (_grid.IsBlocked(r, c)) continue;
                    var tile = _grid.Get(r, c);
                    if (tile == null || tile.State == TileState.Empty) continue;

                    int len = 1;
                    while (r + len < _grid.Rows &&
                           !_grid.IsBlocked(r + len, c) &&
                           _grid.Get(r + len, c)?.Type == tile.Type &&
                           _grid.Get(r + len, c)?.State != TileState.Empty)
                        len++;

                    if (len >= 3)
                    {
                        for (int i = 0; i < len; i++)
                            _matchesBuffer.Add(new Vector2Int(r + i, c));
                        r += len - 1;
                    }
                }
            }

            return _matchesBuffer;
        }
    }
}