using System;
using UnityEngine;

namespace Match3.Core
{
    public enum GameState
    {
        Idle,
        Selecting,
        Swapping,
        Checking,
        Clearing,
        Dropping,
        Spawning,
        GameOver
    }
    public class GameFlowController : MonoBehaviour
    {
        public static GameFlowController Instance { get; private set; }

        public GameState CurrentState { get; private set; } = GameState.Idle;

        public event Action<GameState> OnStateChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void SetState(GameState newState)
        {
            if (CurrentState == newState) return;
            CurrentState = newState;
            OnStateChanged?.Invoke(newState);
        }
        public bool CanAcceptInput => CurrentState == GameState.Idle;
        public bool IsGameOver => CurrentState == GameState.GameOver;
    }
}
