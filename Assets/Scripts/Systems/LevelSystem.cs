using System;
using Match3.Core;
using Match3.Data;
using UnityEngine;

namespace Match3.Systems
{
    public class LevelSystem : MonoBehaviour
    {
        public static LevelSystem Instance { get; private set; }
        public LevelConfig Config { get; private set; }
        public int MovesRemaining { get; private set; }
        public int TargetScore { get; private set; }
        public event Action<int> OnMovesChanged;
        public event Action OnWin;
        public event Action OnLose;

        private LevelConfig _config;
        private GameFlowController _flow;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public void Initialize(LevelConfig config, GameFlowController flow)
        {
            Config = config;
            _config = config;
            _flow = flow;

            // 🔥【补正】确保每次切换新关卡时，这些核心数值被成功刷新
            MovesRemaining = config.MaxMoves;
            TargetScore = config.TargetScore;

            // 触发一下事件，让 UI 刷新显示新关卡的初始步数
            OnMovesChanged?.Invoke(MovesRemaining);
        }

        public void ConsumeMove()
        {
            MovesRemaining = Mathf.Max(0, MovesRemaining - 1);
            OnMovesChanged?.Invoke(MovesRemaining);
        }

        public void CheckConditions()
        {
            bool won = ScoreSystem.Instance.HasReachedTarget(_config.TargetScore);

            if (won)
            {
                AudioManager.Instance?.PlayWin();
                _flow.SetState(GameState.GameOver);
                OnWin?.Invoke();
                return;
            }

            if (MovesRemaining <= 0)
            {
                AudioManager.Instance?.PlayLose();
                _flow.SetState(GameState.GameOver);
                OnLose?.Invoke();
            }
        }
    }
}
