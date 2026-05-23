using UnityEngine;
using Match3.Systems;

namespace Match3.Core
{
    public static class CoordinateConverter
    {
        public static bool WorldToGrid(Vector3 worldPos, BoardView boardView, out int row, out int col)
        {
            return boardView.WorldToGrid(worldPos, out row, out col);
        }

        public static Vector3 GridToWorld(int row, int col, BoardView boardView)
        {
            return boardView.GridToWorld(row, col);
        }

        public static Vector2 WorldToUI(Vector3 worldPos, Canvas canvas, Camera uiCamera, RectTransform targetRect)
        {
            Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(uiCamera, worldPos);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(targetRect, screenPos, uiCamera, out Vector2 localPoint);
            return localPoint;
        }
    }
}