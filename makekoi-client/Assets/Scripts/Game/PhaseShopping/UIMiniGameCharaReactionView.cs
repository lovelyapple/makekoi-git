using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIMiniGameCharaReactionView : MonoBehaviour
{
    [SerializeField] private Image _characterImage;
    [SerializeField] private TextMeshProUGUI _reactionMessageText;
    public void UpdateCharacterReaction(StaffReactionCode reactionCode)
    {
        var record = StaffReactionTableContainer.GetRandomRecord(reactionCode);
        var spriteResource = ResourceContainer.Instance.GetSpriteByPath(record.TexturePath);
        _characterImage.sprite = spriteResource;
        _reactionMessageText.text = record.Message;
    }
}
