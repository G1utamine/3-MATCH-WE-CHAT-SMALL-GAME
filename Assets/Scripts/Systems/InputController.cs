using Match3.Core;
using UnityEngine;

namespace Match3.Systems
{
    /// <summary>
    /// 统一处理 PC鼠标 和 手机触摸 两套输入，支持：
    ///   - 点击选中 → 再点击相邻格子 → 交换
    ///   - 按住拖拽 → 松手时根据方向交换
    /// 两套逻辑共用同一套状态，互不干扰。
    /// 只有在 GameState.Idle 时才接受输入。
    /// </summary>
    public class InputController : MonoBehaviour
    {
        // ── 可调参数 ────────────────────────────────────────────────────
        /// <summary>拖拽生效的最小像素距离（小于此视为点击）</summary>
        [SerializeField] private float _dragThresholdPixels = 20f;

        // ── 依赖 ────────────────────────────────────────────────────────
        private GameFlowController _flow;
        private BoardView _boardView;
        private SwapSystem _swapSystem;
        private Camera _cam;

        // ── 状态 ────────────────────────────────────────────────────────
        // 选中（点击模式）
        private int _selectedRow = -1;
        private int _selectedCol = -1;
        private bool _hasSelection;

        // 按下起始信息（拖拽判断用）
        private Vector2 _pressScreenPos;   // 按下时的屏幕坐标
        private int _pressRow = -1;
        private int _pressCol = -1;
        private bool _isPressing;          // 当前是否处于按下状态
        private bool _dragFired;           // 本次按下是否已触发过拖拽交换

        // ── 初始化 ───────────────────────────────────────────────────────
        public void Initialize(GameFlowController flow, BoardView boardView, SwapSystem swapSystem)
        {
            _flow = flow;
            _boardView = boardView;
            _swapSystem = swapSystem;
            _cam = Camera.main;
        }

        // ── Update 主循环 ────────────────────────────────────────────────
        private void Update()
        {
            // 手机触摸优先；有触摸时忽略鼠标，避免双重触发
            if (Input.touchCount > 0)
            {
                HandleTouch();
            }
            else
            {
                HandleMouse();
            }
        }

        // ════════════════════════════════════════════════════════════════
        // 鼠标输入
        // ════════════════════════════════════════════════════════════════

        private void HandleMouse()
        {
            if (Input.GetMouseButtonDown(0))
                OnPointerDown(Input.mousePosition);

            if (Input.GetMouseButton(0) && _isPressing)
                OnPointerMove(Input.mousePosition);

            if (Input.GetMouseButtonUp(0) && _isPressing)
                OnPointerUp(Input.mousePosition);
        }

        // ════════════════════════════════════════════════════════════════
        // 触摸输入（单指）
        // ════════════════════════════════════════════════════════════════

        private void HandleTouch()
        {
            // 只处理第一根手指
            Touch touch = Input.GetTouch(0);

            switch (touch.phase)
            {
                case TouchPhase.Began:
                    OnPointerDown(touch.position);
                    break;
                case TouchPhase.Moved:
                case TouchPhase.Stationary:
                    if (_isPressing)
                        OnPointerMove(touch.position);
                    break;
                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    if (_isPressing)
                        OnPointerUp(touch.position);
                    break;
            }
        }

        // ════════════════════════════════════════════════════════════════
        // 统一指针逻辑
        // ════════════════════════════════════════════════════════════════

        /// <summary>按下/触摸开始</summary>
        private void OnPointerDown(Vector2 screenPos)
        {
            if (!_flow.CanAcceptInput) return;

            Vector3 world = ScreenToWorld(screenPos);
            if (!_boardView.WorldToGrid(world, out int row, out int col))
            {
                // 点到空白区域：取消已有选中
                ClearSelection();
                return;
            }

            _isPressing = true;
            _dragFired = false;
            _pressScreenPos = screenPos;
            _pressRow = row;
            _pressCol = col;
        }

        /// <summary>移动中：实时判断是否达到拖拽阈值</summary>
        private void OnPointerMove(Vector2 screenPos)
        {
            if (_dragFired) return;
            if (!_flow.CanAcceptInput) return;

            float dist = Vector2.Distance(screenPos, _pressScreenPos);
            if (dist < _dragThresholdPixels) return;

            Vector2 delta = screenPos - _pressScreenPos;
            int targetRow = _pressRow;
            int targetCol = _pressCol;

            if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
                targetCol += delta.x > 0 ? 1 : -1;
            else
                targetRow += delta.y > 0 ? 1 : -1;

            // 拖出棋盘边界直接忽略，不触发交换
            if (!GridSystem.Instance.InBounds(targetRow, targetCol))
            {
                _dragFired = true; // 标记为已处理，避免反复触发
                return;
            }

            _dragFired = true;
            ClearSelection();
            TrySwap(_pressRow, _pressCol, targetRow, targetCol);
        }

        /// <summary>抬起/触摸结束：若没拖拽则视为点击</summary>
        private void OnPointerUp(Vector2 screenPos)
        {
            _isPressing = false;

            if (_dragFired)
            {
                // 本次已是拖拽，不再处理点击逻辑
                _dragFired = false;
                return;
            }

            // 未达到拖拽阈值 → 走点击逻辑
            if (!_flow.CanAcceptInput) return;

            Vector3 world = ScreenToWorld(screenPos);
            if (!_boardView.WorldToGrid(world, out int row, out int col))
            {
                ClearSelection();
                return;
            }

            HandleClickOnTile(row, col);
        }

        // ════════════════════════════════════════════════════════════════
        // 点击逻辑（两步选中）
        // ════════════════════════════════════════════════════════════════

        private void HandleClickOnTile(int row, int col)
        {
            if (!_hasSelection)
            {
                // 第一步：选中
                SelectTile(row, col);
            }
            else
            {
                if (row == _selectedRow && col == _selectedCol)
                {
                    // 再次点击同一格：取消选中
                    ClearSelection();
                    return;
                }

                // 第二步：尝试交换
                int r1 = _selectedRow, c1 = _selectedCol;
                ClearSelection();
                TrySwap(r1, c1, row, col);
            }
        }

        // ════════════════════════════════════════════════════════════════
        // 辅助方法
        // ════════════════════════════════════════════════════════════════

        private void TrySwap(int r1, int c1, int r2, int c2)
        {
            _flow.SetState(GameState.Selecting);
            _swapSystem.TrySwap(r1, c1, r2, c2);
        }

        private void SelectTile(int row, int col)
        {
            _hasSelection = true;
            _selectedRow = row;
            _selectedCol = col;

            _boardView.GetView(row, col)?.SetSelected(true);
        }

        private void ClearSelection()
        {
            if (_hasSelection)
                _boardView.GetView(_selectedRow, _selectedCol)?.SetSelected(false);

            _hasSelection = false;
            _selectedRow = _selectedCol = -1;
        }

        private Vector3 ScreenToWorld(Vector2 screenPos)
        {
            Vector3 world = _cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 0f));
            world.z = 0f;
            return world;
        }
    }
}
