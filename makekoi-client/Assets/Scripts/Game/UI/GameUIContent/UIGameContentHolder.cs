using UnityEngine;

public class UIGameContentHolder : MonoBehaviour
{
    [SerializeField] private GamePhaseContentType _buttonType;
    public GamePhaseContentType ButtonType => _buttonType;
    private void Start()
    {
        var contentPrefab = ResourceContainer.Instance.GetGamePhaseContentPrefab(_buttonType);
        if (contentPrefab != null)
        {
            var content = Instantiate(contentPrefab, this.transform);
            content.transform.localPosition = Vector3.zero;
            content.transform.localScale = Vector3.one;
        }
        else
        {
            Debug.LogWarning($"[UIGameContentHolder] Content prefab not found for type: {_buttonType}", this);
        }
    }
}
