using UnityEngine;

namespace Match3.Data
{
    [CreateAssetMenu(fileName = "LevelConfig", menuName = "Match3/Level Config")]
    public class LevelConfig : ScriptableObject
    {
        [Header("Grid Settings")]
        public int Rows = 8;
        public int Cols = 8;

        [Header("Tile Types")]
        public int TileTypeCount = 4; // how many colors to use

        [Header("Win Conditions")]
        public int TargetScore = 1000;
        public int MaxMoves = 30;

        [Header("Score Settings")]
        public int BaseScorePerTile = 10;
        public float ComboMultiplierStep = 0.5f; // per cascade level
    }
}
