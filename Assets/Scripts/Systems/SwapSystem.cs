using System;
using System.Collections;
using DG.Tweening;
using Match3.Core;
using Match3.Data;
using UnityEngine;

namespace Match3.Systems
{
    /// <summary>
    /// Performs a swap between two adjacent tiles.
    /// Validates adjacency and match-result; reverts on failure.
    /// </summary>
    public class SwapSystem : MonoBehaviour
    {
        public static SwapSystem Instance { get; private set; }

        private GridSystem _grid;
        private BoardView _boardView;
        private MatchChecker _matchChecker;
        private GameFlowController _flow;

        public event Action OnSwapSuccess;
        public event Action OnSwapFailed;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public void Initialize(GridSystem grid, BoardView boardView, MatchChecker checker, GameFlowController flow)
        {
            _grid = grid;
            _boardView = boardView;
            _matchChecker = checker;
            _flow = flow;
        }

        public bool AreAdjacent(int r1, int c1, int r2, int c2)
        {
            return (Mathf.Abs(r1 - r2) + Mathf.Abs(c1 - c2)) == 1;
        }

        /// <summary>Entry point from InputController.</summary>
        public void TrySwap(int r1, int c1, int r2, int c2)
        {
            // 目标格是屏蔽格直接忽略，不播任何动画
            if (!GridSystem.Instance.InBounds(r2, c2) || GridSystem.Instance.IsBlocked(r2, c2))
            {
                OnSwapFailed?.Invoke();
                return;
            }

            if (!AreAdjacent(r1, c1, r2, c2))
            {
                OnSwapFailed?.Invoke();
                return;
            }

            if (!_matchChecker.WouldSwapMatch(r1, c1, r2, c2))
            {
                StartCoroutine(AnimateFailedSwap(r1, c1, r2, c2));
                return;
            }

            StartCoroutine(PerformSwap(r1, c1, r2, c2));
        }

        private IEnumerator PerformSwap(int r1, int c1, int r2, int c2)
        {
            _flow.SetState(GameState.Swapping);

            // Update data
            _grid.Swap(r1, c1, r2, c2);
            AudioManager.Instance?.PlaySwap();
            // Animate
            var tween = _boardView.AnimateSwap(r1, c1, r2, c2);
            yield return tween.WaitForCompletion();

            OnSwapSuccess?.Invoke();
        }

        private IEnumerator AnimateFailedSwap(int r1, int c1, int r2, int c2)
        {
            _flow.SetState(GameState.Swapping);

            // Quick swap out
            var tweenOut = _boardView.AnimateSwap(r1, c1, r2, c2, 0.15f);
            yield return tweenOut.WaitForCompletion();

            // Revert data (no-op because data wasn't swapped)
            // Quick swap back
            var tweenBack = _boardView.AnimateSwap(r2, c2, r1, c1, 0.15f);
            yield return tweenBack.WaitForCompletion();

            OnSwapFailed?.Invoke();
            _flow.SetState(GameState.Idle);
        }

    }
}
