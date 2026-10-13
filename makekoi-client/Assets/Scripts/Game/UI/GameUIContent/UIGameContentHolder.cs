using R3;
using UnityEngine;

public class UIGameContentHolder : MonoBehaviour
{
    [SerializeField] private GamePhaseContentType _buttonType;
    public GamePhaseContentType ButtonType => _buttonType;
    public bool IsContentLoaded => _contentInstance != null;
    public Observable<GamePhaseContentType> OnRequestGoToNextPhase() => _contentInstance.OnRequestGoToNextPhase();
    public GamePhaseWindowBase Content => _contentInstance;
    private GamePhaseWindowBase _contentInstance;
    private RectTransform _contentRect;
    private Vector2 _contentDesignSize;

    private void Start()
    {
        var contentPrefab = ResourceContainer.Instance.GetGamePhaseContentPrefab(_buttonType);
        if (contentPrefab != null)
        {
            var content = Instantiate(contentPrefab, this.transform);
            _contentInstance = content.GetComponent<GamePhaseWindowBase>();
            var rt = content.GetComponent<RectTransform>();
            rt.SetParent(transform, false);     // UIではこれが重要（worldPositionStays=false）

            // Holder中央に、Prefabの設計サイズのまま置く。収まらない分はFitContentで縮める。
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero; // localPositionではなくanchoredPosition

            _contentRect = rt;
            _contentDesignSize = rt.rect.size;
            FitContent();
        }
        else
        {
            Debug.LogWarning($"[UIGameContentHolder] Content prefab not found for type: {_buttonType}", this);
        }
    }

    /// <summary>
    /// Holderのサイズが変わったときにUnityから呼ばれる。
    /// Holder自体はページャが画面サイズに合わせて設定するため、ここで追従させる。
    /// </summary>
    private void OnRectTransformDimensionsChange() => FitContent();

    /// <summary>
    /// Prefabの縦横比を保ったままHolderに収める。縦が入らない端末では横も一緒に縮む。
    /// 設計サイズより拡大はしない。
    /// </summary>
    private void FitContent()
    {
        if (_contentRect == null) return;
        if (_contentDesignSize.x <= 0f || _contentDesignSize.y <= 0f) return;

        var holder = (RectTransform)transform;
        var scale = Mathf.Min(
            holder.rect.width / _contentDesignSize.x,
            holder.rect.height / _contentDesignSize.y,
            1f);

        if (scale <= 0f || float.IsNaN(scale)) return;

        _contentRect.localScale = new Vector3(scale, scale, 1f);
    }
}
