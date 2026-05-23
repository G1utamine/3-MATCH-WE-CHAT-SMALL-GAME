using System;
using UnityEngine;
using Match3.Data;

namespace Match3.Systems
{
    public class ScoreSystem : MonoBehaviour
    {
        public static ScoreSystem Instance { get; private set; }

        public int CurrentScore { get; private set; }
        public event Action<int> OnScoreChanged;

        private LevelConfig _config;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public void Initialize(LevelConfig config)
        {
            _config = config;
            CurrentScore = 0;
        }

        /// <summary>
        /// cascadeLevel 0 = first match (no bonus), 1+ = combo.
        /// </summary>
        public void AddScore(int tileCount, int cascadeLevel)
        {
            float multiplier = 1f + cascadeLevel * _config.ComboMultiplierStep;
            int gain = Mathf.RoundToInt(tileCount * _config.BaseScorePerTile * multiplier);
            CurrentScore += gain;


            OnScoreChanged?.Invoke(CurrentScore);
        }

        public bool HasReachedTarget(int target) => CurrentScore >= target;
    }
}
