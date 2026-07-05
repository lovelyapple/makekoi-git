using TMPro;
using UnityEngine;

[RequireComponent(typeof(TextMeshProUGUI))]
public class UITextLinker : MonoBehaviour
{
    [SerializeField] private string _titleKey;

    private TextMeshProUGUI _textMeshPro;

    private void Awake()
    {
        _textMeshPro = GetComponent<TextMeshProUGUI>();
    }

    private void Start()
    {
        if (!string.IsNullOrEmpty(_titleKey))
            SetTextByKey(_titleKey);
    }

    public void SetTextByKey(string titleKey)
    {
        var text = TempTextTableContainer.GetText(titleKey);
        if (text != null)
            _textMeshPro.text = text;
        else
            Debug.LogWarning($"[UITextLinker] Key not found: {titleKey}", this);
    }

    public void SetText(string text)
    {
        _textMeshPro.text = text;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        var tmp = GetComponent<TextMeshProUGUI>();
        if (tmp == null) return;
        if (string.IsNullOrEmpty(_titleKey)) return;

        var text = TempTextTableContainer.GetText(_titleKey);
        tmp.text = text ?? "NOT_FOUND";
    }
#endif
}
