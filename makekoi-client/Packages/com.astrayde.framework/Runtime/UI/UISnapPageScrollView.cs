using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace AstraydeFramework.UI
{
    /// <summary>
    /// 中央スナップ付きページングScrollRect（横・縦対応）。
    /// - ドラッグ終了時に最近傍の要素へスナップ
    /// - Prev / Next / GoToIndex で任意ページへ移動
    /// - currentIndex ± bufferCount の範囲のみCellを描画し、Cellを使い回す
    /// </summary>
    public class UISnapPageScrollView : MonoBehaviour, IBeginDragHandler, IEndDragHandler
    {
        [SerializeField] private ScrollRect _scrollRect;
        [SerializeField] private RectTransform _content;

        [Tooltip("中央インデックスの前後何個のCellを描画するか")]
        [SerializeField] private int _bufferCount = 2;

        [SerializeField] private float _snapDuration = 0.25f;
        [SerializeField] private float _spacing = 0f;

        [Tooltip("ONにすると縦スクロール、OFFで横スクロール")]
        [SerializeField] private bool _isVertical = false;

        [Tooltip("ONにするとCellサイズをViewportサイズに合わせる（1ページ＝1画面）")]
        [SerializeField] private bool _fitCellSizeToViewport = true;

        [Header("Buttons (optional)")]
        [SerializeField] private Button _prevButton;
        [SerializeField] private Button _nextButton;

        [Header("Mode")]
        [Tooltip("ドラッグによる手動スクロールを許可するか")]
        [SerializeField] private bool _dragEnabled = true;
        [Tooltip("ONにするとStart時に _inspectorCells をCellとして使う")]
        [SerializeField] private bool _useManualCells = false;
        [Tooltip("手動配置モード時に使うCellをInspectorで差す")]
        [SerializeField] private List<GameObject> _inspectorCells = new();

        private List<object> _dataList = new();
        private GameObject _cellPrefab;
        private int _cellCount;
        private float _cellSize; // 横モード=width、縦モード=height

        private readonly Dictionary<int, GameObject> _activeCells = new();
        private readonly Queue<GameObject> _cellPool = new();

        private RectTransform _viewport;
        private int _currentIndex;
        private bool _isSnapping;
        private bool _isDragging;

        public int CurrentIndex => _currentIndex;

        /// <summary>ページが変わったときに通知。引数は実データindex。</summary>
        public event Action<int> OnPageChanged;

        /// <summary>ページが変わったときに通知。型付きデータと中心セルのGameObjectを受け取る場合はこちらを使う。</summary>
        public void OnPageChangedData<T>(Action<int, T, GameObject> callback)
        {
            _onPageChangedData = (index, data, go) => callback(index, (T)data, go);
        }

        // ------------------------------------------------------------------ //
        // 初期化 / リセット
        // ------------------------------------------------------------------ //

        private void Start()
        {
            if (_useManualCells) InitializeManual();
        }

        /// <summary>Prefabモード: CellのSizeはPrefabのRectTransformから自動取得する。</summary>
        public void Initialize<T>(GameObject cellPrefab, List<T> dataList)
        {
            _cellPrefab = cellPrefab;
            _dataList = new List<object>(dataList.Count);
            foreach (var item in dataList) _dataList.Add(item);
            _cellCount = _dataList.Count;
            _viewport = _scrollRect.viewport;
            // ViewportはScrollRectにdrivenされるため、サイズを読む前にレイアウトを確定させる
            Canvas.ForceUpdateCanvases();
            var rt = cellPrefab.GetComponent<RectTransform>();
            _cellSize = ResolveCellSize(rt);

            Setup();
        }

        /// <summary>手動モード + データバインド: Start後にデータを差し込みたい場合に呼ぶ。</summary>
        public void BindData<T>(List<T> dataList, Action<T, GameObject> onDraw)
        {
            for (var i = 0; i < Mathf.Min(_cellCount, dataList.Count); i++)
                onDraw?.Invoke(dataList[i], _inspectorCells[i]);
        }

        private void InitializeManual()
        {
            _cellCount = _inspectorCells.Count;
            _viewport = _scrollRect.viewport;
            // ViewportはScrollRectにdrivenされるため、サイズを読む前にレイアウトを確定させる
            Canvas.ForceUpdateCanvases();
            if (_cellCount > 0)
            {
                var rt = _inspectorCells[0].GetComponent<RectTransform>();
                _cellSize = ResolveCellSize(rt);
            }
            else
            {
                _cellSize = 0f;
            }

            _activeCells.Clear();
            for (var i = 0; i < _cellCount; i++)
            {
                PositionCell(_inspectorCells[i], i);
                _activeCells[i] = _inspectorCells[i];
            }

            Setup();
        }

        /// <summary>Cell1枚分のサイズ。_fitCellSizeToViewport がONならViewportサイズを採用する。</summary>
        private float ResolveCellSize(RectTransform cellRect)
        {
            if (_fitCellSizeToViewport)
                return _isVertical ? _viewport.rect.height : _viewport.rect.width;

            return _isVertical ? cellRect.rect.height : cellRect.rect.width;
        }

        private void Setup()
        {
            Canvas.ForceUpdateCanvases();

            _scrollRect.horizontal = !_isVertical;
            _scrollRect.vertical = _isVertical;
            _scrollRect.inertia = false;
            _scrollRect.movementType = ScrollRect.MovementType.Unrestricted;

            // ドラッグ無効時はScrollRect自体を無効化する（ApplyContentPositionは直接anchoredPositionを操作するため引き続き動作する）
            if (!_dragEnabled)
                _scrollRect.enabled = false;

            var totalSize = _cellCount * _cellSize + Mathf.Max(0, _cellCount - 1) * _spacing;
            _content.sizeDelta = _isVertical
                ? new Vector2(_content.sizeDelta.x, totalSize)
                : new Vector2(totalSize, _content.sizeDelta.y);

            _currentIndex = 0;
            ApplyContentPosition(GetTargetPos(0));
            UpdateVisibleCells();
            RefreshButtons();

            // 初期表示時にも index0 のデータを通知する
            if (_cellCount > 0)
            {
                var centerGo = _activeCells.TryGetValue(0, out var go) ? go : null;
                OnPageChanged?.Invoke(0);
                if (_dataList.Count > 0)
                    _onPageChangedData?.Invoke(0, _dataList[0], centerGo);
            }

            if (_prevButton != null)
            {
                _prevButton.onClick.RemoveAllListeners();
                _prevButton.onClick.AddListener(MovePrev);
            }
            if (_nextButton != null)
            {
                _nextButton.onClick.RemoveAllListeners();
                _nextButton.onClick.AddListener(MoveNext);
            }
        }

        /// <summary>Cell描画コールバックを設定する。Initialize の前後どちらでも可。</summary>
        public void OnDrawCell<T>(Action<T, GameObject> callback)
        {
            _onDrawCell = (index, data, go) => callback((T)data, go);
        }

        /// <summary>全Cellを非表示にしてリセットする。</summary>
        public void Clear()
        {
            foreach (var kv in _activeCells)
            {
                if (_useManualCells) kv.Value.SetActive(false);
                else ReturnToPool(kv.Value);
            }
            _activeCells.Clear();
            if (!_useManualCells)
                while (_cellPool.Count > 0) Destroy(_cellPool.Dequeue());
            _cellCount = 0;
            _dataList.Clear();
        }

        // ------------------------------------------------------------------ //
        // 外部API
        // ------------------------------------------------------------------ //

        public void MovePrev() => GoToIndex(_currentIndex - 1);
        public void MoveNext() => GoToIndex(_currentIndex + 1);

        public void GoToIndex(int index)
        {
            if (_cellCount == 0) return;
            index = Mathf.Clamp(index, 0, _cellCount - 1);
            SnapToIndexAsync(index).Forget();
        }

        private void RefreshButtons()
        {
            if (_prevButton != null) _prevButton.interactable = _cellCount > 1;
            if (_nextButton != null) _nextButton.interactable = _cellCount > 1;
        }

        // ------------------------------------------------------------------ //
        // ドラッグイベント
        // ------------------------------------------------------------------ //

        void IBeginDragHandler.OnBeginDrag(PointerEventData eventData)
        {
            if (!_dragEnabled || _cellCount <= 1) return;
            _isDragging = true;
            _isSnapping = false;
        }

        void IEndDragHandler.OnEndDrag(PointerEventData eventData)
        {
            if (!_dragEnabled || _cellCount <= 1) return;
            _isDragging = false;
            SnapToIndexAsync(GetNearestIndex()).Forget();
        }

        // ------------------------------------------------------------------ //
        // スナップアニメーション
        // ------------------------------------------------------------------ //

        private async UniTaskVoid SnapToIndexAsync(int targetIndex)
        {
            _isSnapping = true;
            _currentIndex = targetIndex;
            UpdateVisibleCells();

            var startPos = _isVertical ? _content.anchoredPosition.y : _content.anchoredPosition.x;
            var endPos = GetTargetPos(targetIndex);
            var elapsed = 0f;
            var ct = this.GetCancellationTokenOnDestroy();

            while (elapsed < _snapDuration && _isSnapping && !_isDragging)
            {
                elapsed += Time.deltaTime;
                var t = EaseOutQuad(Mathf.Clamp01(elapsed / _snapDuration));
                ApplyContentPosition(Mathf.Lerp(startPos, endPos, t));
                await UniTask.Yield(PlayerLoopTiming.Update, ct);
            }

            if (!_isDragging && _isSnapping)
            {
                ApplyContentPosition(endPos);
                var centerGo = _activeCells.TryGetValue(_currentIndex, out var go) ? go : null;
                OnPageChanged?.Invoke(_currentIndex);
                _onPageChangedData?.Invoke(_currentIndex, _currentIndex < _dataList.Count ? _dataList[_currentIndex] : null, centerGo);
            }

            _isSnapping = false;
        }

        // ------------------------------------------------------------------ //
        // 仮想セル管理
        // ------------------------------------------------------------------ //

        private void UpdateVisibleCells()
        {
            if (_cellCount == 0) return;

            var first = Mathf.Max(0, _currentIndex - _bufferCount);
            var last = Mathf.Min(_cellCount - 1, _currentIndex + _bufferCount);

            var toHide = new List<int>();
            foreach (var kv in _activeCells)
                if (kv.Key < first || kv.Key > last) toHide.Add(kv.Key);

            foreach (var idx in toHide)
            {
                if (_useManualCells) _activeCells[idx].SetActive(false);
                else ReturnToPool(_activeCells[idx]);
                _activeCells.Remove(idx);
            }

            for (var i = first; i <= last; i++)
            {
                if (_activeCells.ContainsKey(i)) continue;

                GameObject cell;
                if (_useManualCells)
                {
                    cell = _inspectorCells[i];
                }
                else
                {
                    if (_cellPrefab == null) continue;
                    cell = _cellPool.Count > 0 ? _cellPool.Dequeue() : Instantiate(_cellPrefab, _content);
                    PositionCell(cell, i);
                    _onDrawCell?.Invoke(i, _dataList[i], cell);
                }

                cell.SetActive(true);
                _activeCells[i] = cell;
            }
        }

        private void PositionCell(GameObject cell, int index)
        {
            var rt = cell.GetComponent<RectTransform>();
            if (_isVertical)
            {
                // 縦: 上から下へ並べる
                rt.anchorMin = new Vector2(0.5f, 1f);
                rt.anchorMax = new Vector2(0.5f, 1f);
                rt.pivot = new Vector2(0.5f, 1f);
                rt.anchoredPosition = new Vector2(0f, -(index * (_cellSize + _spacing)));
                rt.sizeDelta = new Vector2(rt.sizeDelta.x, _cellSize);
            }
            else
            {
                // 横: 左から右へ並べる
                rt.anchorMin = new Vector2(0f, 0.5f);
                rt.anchorMax = new Vector2(0f, 0.5f);
                rt.pivot = new Vector2(0f, 0.5f);
                rt.anchoredPosition = new Vector2(index * (_cellSize + _spacing), 0f);
                rt.sizeDelta = new Vector2(_cellSize, rt.sizeDelta.y);
            }
        }

        private void ReturnToPool(GameObject cell)
        {
            cell.SetActive(false);
            _cellPool.Enqueue(cell);
        }

        // ------------------------------------------------------------------ //
        // ユーティリティ
        // ------------------------------------------------------------------ //

        private int GetNearestIndex()
        {
            var stride = _cellSize + _spacing;
            float raw;
            if (_isVertical)
            {
                // GetTargetPos の逆関数
                // index = (contentY - cellSize/2 + viewportH/2) / stride
                raw = (_content.anchoredPosition.y - _cellSize / 2f + _viewport.rect.height / 2f) / stride;
            }
            else
            {
                // contentX + CellCenter = viewport.width/2 を解いた式
                // CellCenter = index*(cellSize+spacing) + cellSize/2
                // contentX   = viewport.width/2 - CellCenter
                raw = (_viewport.rect.width / 2f - _content.anchoredPosition.x - _cellSize / 2f) / stride;
            }
            return Mathf.Clamp(Mathf.RoundToInt(raw), 0, _cellCount - 1);
        }

        private float GetTargetPos(int index)
        {
            if (_isVertical)
            {
                // Contentはpivot(0,1)・anchor上端で、anchoredPosition.yは「正で上へ」。
                // Cell中心をViewport中心に合わせる条件を解くと以下になる。
                // contentY = cellSize/2 - viewportH/2 + index*stride
                return _cellSize / 2f - _viewport.rect.height / 2f + index * (_cellSize + _spacing);
            }
            else
            {
                // contentX + CellCenter = viewport.width/2 を解いた式
                // CellCenter = index*(cellSize+spacing) + cellSize/2
                // contentX   = viewport.width/2 - CellCenter
                return _viewport.rect.width / 2f - index * (_cellSize + _spacing) - _cellSize / 2f;
            }
        }

        private void ApplyContentPosition(float pos)
        {
            _content.anchoredPosition = _isVertical
                ? new Vector2(_content.anchoredPosition.x, pos)
                : new Vector2(pos, _content.anchoredPosition.y);
        }

        private static float EaseOutQuad(float t) => 1f - (1f - t) * (1f - t);

        private Action<int, object, GameObject> _onDrawCell;
        private Action<int, object, GameObject> _onPageChangedData;
    }
}