using R3;
using UnityEngine;

public class UIGameContentHolder : MonoBehaviour
{
    [SerializeField] private GamePhaseContentType _buttonType;
    public GamePhaseContentType ButtonType => _buttonType;
    public bool IsContentLoaded => _contentInstance != null;
    public Observable<GamePhaseContentType> OnRequestGoToNextPhase() => _contentInstance.OnRequestGoToNextPhase();
    private GamePhaseWindowBase _contentInstance;
    private void Start()
    {
        var contentPrefab = ResourceContainer.Instance.GetGamePhaseContentPrefab(_buttonType);
        if (contentPrefab != null)
        {
            var content = Instantiate(contentPrefab, this.transform);
            _contentInstance = content.GetComponent<GamePhaseWindowBase>();
            var rt = content.GetComponent<RectTransform>();
            rt.SetParent(transform, false);     // UIではこれが重要（worldPositionStays=false）
            rt.anchoredPosition = Vector2.zero; // localPositionではなくanchoredPosition
            rt.localScale = Vector3.one;

            if (_contentInstance is TitlePresenter titlePresenter)
            {
                titlePresenter.OnEnter();
            }
        }
        else
        {
            Debug.LogWarning($"[UIGameContentHolder] Content prefab not found for type: {_buttonType}", this);
        }
    }
}
