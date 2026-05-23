using Match3.Data;
using UnityEngine;

namespace Match3.Systems
{
    public class DeadlockChecker
    {
        private readonly GridSystem _grid;
        private readonly MatchChecker _matchChecker;

        public DeadlockChecker(GridSystem grid, MatchChecker matchChecker)
        {
            _grid = grid;
            _matchChecker = matchChecker;
        }

        /// <summary>扫描全盘，是否存在至少一个合法交换</summary>
        public bool HasValidMove()
        {
            for (int r = 0; r < _grid.Rows; r++)
            {
                for (int c = 0; c < _grid.Cols; c++)
                {
                    if (_grid.IsBlocked(r, c)) continue;
                    if (_grid.IsEmpty(r, c)) continue;

                    // 只检查右和上，避免重复检测
                    if (CheckSwap(r, c, r, c + 1)) return true;
                    if (CheckSwap(r, c, r + 1, c)) return true;
                }
            }
            return false;
        }

        private bool CheckSwap(int r1, int c1, int r2, int c2)
        {
            if (!_grid.InBounds(r2, c2)) return false;
            if (_grid.IsBlocked(r2, c2)) return false;
            if (_grid.IsEmpty(r2, c2)) return false;
            return _matchChecker.WouldSwapMatch(r1, c1, r2, c2);
        }
    }
}