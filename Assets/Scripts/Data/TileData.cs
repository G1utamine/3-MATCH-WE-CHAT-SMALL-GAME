using UnityEngine;

namespace Match3.Data
{
    public enum TileType
    {
        Red = 0,
        Blue = 1,
        Green = 2,
        Yellow = 3,
        Purple = 4,
        Orange = 5
    }

    public enum TileState
    {
        Normal,
        Selected,
        PendingClear,
        Empty
    }

    [System.Serializable]
    public class TileData
    {
        public TileType Type;
        public int Row;
        public int Col;
        public TileState State;

        public TileData(TileType type, int row, int col)
        {
            Type = type;
            Row = row;
            Col = col;
            State = TileState.Normal;
        }

        public Vector2Int Position => new Vector2Int(Col, Row);

        public override string ToString() => $"Tile[{Row},{Col}] Type={Type} State={State}";
    }
}
