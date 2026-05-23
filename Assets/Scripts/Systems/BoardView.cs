using System.Collections.Generic;
using DG.Tweening;
using Match3.Core;
using Match3.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Match3.Systems
{
    public class BoardView : MonoBehaviour
    {
        public static BoardView Instance { get; private set; }

        [Header("Tile 设置")]
        [SerializeField] private TileView _tilePrefab;
        [SerializeField] private Sprite[] _tileSprites;

        [Header("棋盘格子（按从左上到右下顺序拖入，共64个）")]
        [SerializeField] private RectTransform[] _cells; // 64个，含角落占位

        private TileView[,] _views;
        private int _rows, _cols;
        private Rect[] _cellWorldRects;
        private Vector3[] _cellWorldPositions;
        private (int row, int col)[] _indexToRowCol; // 索引到行列的映射

        public int Rows => _rows;
        public int Cols => _cols;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public void Initialize(int rows, int cols)
        {
            _rows = rows;
            _cols = cols;
            _views = new TileView[rows, cols];
            StartCoroutine(BakeCellRectsNextFrame());
        }

        private System.Collections.IEnumerator BakeCellRectsNextFrame()
        {
            yield return null;
            BakeCellRects();
        }

        private int GridToIndex(int row, int col)
        {
            int visualRow = (_rows - 1 - row);
            return visualRow * _cols + col;
        }

        private RectTransform GetCell(int row, int col)
        {
            int index = GridToIndex(row, col);
            if (index < 0 || index >= _cells.Length) return null;
            return _cells[index];
        }

        public Vector3 GridToWorld(int row, int col)
        {
            int index = GridToIndex(row, col);
            if (index < 0 || index >= _cellWorldPositions.Length) return Vector3.zero;
            return _cellWorldPositions[index];
        }

        public TileView SpawnView(TileData data)
        {
            var cell = GetCell(data.Row, data.Col);
            if (cell == null) return null;

            var view = GameObjectPool<TileView>.Instance.Get(_tilePrefab, cell);
            var rt = view.transform as RectTransform;
            if (rt != null)
            {
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                rt.localScale = Vector3.one;
            }

            view.Initialize(data, _tileSprites[(int)data.Type]);
            _views[data.Row, data.Col] = view;
            return view;
        }

        public TileView GetView(int row, int col) => _views[row, col];
        public void SetView(int row, int col, TileView view) => _views[row, col] = view;
        public void ClearViewRef(int row, int col) => _views[row, col] = null;

        public Tween AnimateSwap(int r1, int c1, int r2, int c2, float duration = 0.25f)
        {
            var v1 = _views[r1, c1];
            var v2 = _views[r2, c2];

            _views[r1, c1] = v2;
            _views[r2, c2] = v1;

            var cell1 = GetCell(r1, c1);
            var cell2 = GetCell(r2, c2);

            var seq = DOTween.Sequence();

            if (v1 != null && cell2 != null)
                seq.Join(v1.MoveTo(cell2, duration).OnComplete(() => { if (v1 != null && cell2 != null) v1.transform.SetParent(cell2, true); }));
            if (v2 != null && cell1 != null)
                seq.Join(v2.MoveTo(cell1, duration).OnComplete(() => { if (v2 != null && cell1 != null) v2.transform.SetParent(cell1, true); }));

            return seq;
        }

        public Tween AnimateClear(List<Vector2Int> positions)
        {
            var seq = DOTween.Sequence();
            int count = positions.Count;
            for (int i = 0; i < count; i++)
            {
                var pos = positions[i];
                var view = _views[pos.x, pos.y];
                if (view == null) continue;

                seq.Join(view.PlayClearAnimation()
                    .OnComplete(() => GameObjectPool<TileView>.Instance.Release(_tilePrefab, view)));

                _views[pos.x, pos.y] = null;
            }
            return seq;
        }

        public Tween AnimateDrop(Dictionary<Vector2Int, Vector2Int> dropMap)
        {
            var seq = DOTween.Sequence();
            foreach (var kv in dropMap)
            {
                var dst = kv.Key;
                var src = kv.Value;
                var view = _views[src.x, src.y];
                if (view == null) continue;

                _views[dst.x, dst.y] = view;
                _views[src.x, src.y] = null;

                var dstCell = GetCell(dst.x, dst.y);
                if (dstCell != null)
                {
                    // 每个棋子开始移动时播放音效
                    var moveTween = view.MoveTo(dstCell, 0.3f)
                        .OnStart(() => AudioManager.Instance?.PlayDrop()) // 👈 添加这一行
                        .OnComplete(() => {
                            if (view != null && dstCell != null)
                                view.transform.SetParent(dstCell, true);
                        });
                    seq.Join(moveTween);
                }
            }
            return seq;
        }

        public Tween AnimateSpawn(List<TileData> newTiles)
        {
            var seq = DOTween.Sequence();
            int count = newTiles.Count;
            float offsetHeight = _cells[0] != null ? _cells[0].rect.height * 1.5f : 150f;

            for (int i = 0; i < count; i++)
            {
                var data = newTiles[i];
                var targetCell = GetCell(data.Row, data.Col);
                if (targetCell == null) continue;

                var view = GameObjectPool<TileView>.Instance.Get(_tilePrefab, targetCell);
                var rt = view.transform as RectTransform;
                if (rt != null)
                {
                    rt.anchorMin = Vector2.zero;
                    rt.anchorMax = Vector2.one;
                    rt.offsetMin = Vector2.zero;
                    rt.offsetMax = Vector2.zero;
                    rt.localScale = Vector3.zero;
                }

                Vector3 startWorld = GridToWorld(_rows - 1, data.Col);
                if (startWorld != Vector3.zero)
                    startWorld.y += offsetHeight;
                else
                    startWorld = targetCell.position + Vector3.up * 200f;

                view.transform.position = startWorld;
                view.Initialize(data, _tileSprites[(int)data.Type]);
                _views[data.Row, data.Col] = view;

                seq.Join(rt.DOScale(Vector3.one, 0.15f));
                seq.Join(view.transform
                    .DOMove(targetCell.position, 0.5f)
                    .SetEase(Ease.OutQuad)
                    .OnComplete(() =>
                    {
                        if (view != null && targetCell != null && view.gameObject.activeSelf)
                            view.transform.SetParent(targetCell, true);
                    }));
            }
            return seq;
        }

        public void ClearTileAtDirectly(int r, int c)
        {
            TileView view = _views[r, c];
            if (view != null)
            {
                view.transform.DOKill();
                view.PlayClearAnimation().OnComplete(() => {
                    GameObjectPool<TileView>.Instance.Release(_tilePrefab, view);
                });
                _views[r, c] = null;
            }
        }

        public List<TileView> GetAllViewsOfType(TileType type)
        {
            List<TileView> list = new List<TileView>();
            for (int r = 0; r < _rows; r++)
            {
                for (int c = 0; c < _cols; c++)
                {
                    if (_views[r, c] != null && _views[r, c].Data.Type == type)
                    {
                        list.Add(_views[r, c]);
                    }
                }
            }
            return list;
        }

        public void ChangeTileTypeAt(int r, int c, TileType newType)
        {
            TileView view = _views[r, c];
            if (view != null)
            {
                view.Data.Type = newType;
                view.Initialize(view.Data, _tileSprites[(int)newType]);
            }
        }

        public void ClearAllTiles()
        {
            if (_views == null) return;

            for (int r = 0; r < _rows; r++)
            {
                for (int c = 0; c < _cols; c++)
                {
                    TileView view = _views[r, c];
                    if (view != null)
                    {
                        view.transform.DOKill();
                        GameObjectPool<TileView>.Instance.Release(_tilePrefab, view);
                        _views[r, c] = null;
                    }
                }
            }
        }

        private void BakeCellRects()
        {
            int length = _cells.Length;
            _cellWorldRects = new Rect[length];
            _cellWorldPositions = new Vector3[length];
            _indexToRowCol = new (int, int)[length];

            var corners = new Vector3[4];
            for (int i = 0; i < length; i++)
            {
                if (_cells[i] == null) continue;

                _cellWorldPositions[i] = _cells[i].position;

                _cells[i].GetWorldCorners(corners);
                _cellWorldRects[i] = new Rect(
                    corners[0].x,
                    corners[0].y,
                    corners[2].x - corners[0].x,
                    corners[2].y - corners[0].y);

                // 反向计算行列映射
                for (int r = 0; r < _rows; r++)
                {
                    for (int c = 0; c < _cols; c++)
                    {
                        if (GridToIndex(r, c) == i)
                        {
                            _indexToRowCol[i] = (r, c);
                            break;
                        }
                    }
                }
            }
        }

        public bool WorldToGrid(Vector3 worldPos, out int row, out int col)
        {
            if (_cellWorldRects == null)
            {
                row = col = -1;
                return false;
            }

            Vector2 checkPos = new Vector2(worldPos.x, worldPos.y);
            int length = _cellWorldRects.Length;
            for (int i = 0; i < length; i++)
            {
                if (_cellWorldRects[i].Contains(checkPos))
                {
                    (row, col) = _indexToRowCol[i];
                    return true;
                }
            }

            row = col = -1;
            return false;
        }
    }
}