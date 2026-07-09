using R3;
using UnityEngine;
using UnityEngine.UI;
using Makekoi.PartnerCreate;

public class UIGenderSelectPanel : UICharacterSelectPanelBase
{
    [SerializeField] private UIGenderSelectButton _maleButton;
    [SerializeField] private UIGenderSelectButton _femaleButton;
    [SerializeField] private Button _confirmButton;
    [SerializeField] private UIGenderSelectConfirmPerformanceView _confirmPerformanceView;
    private bool _isConfirmButtonClicked = false;
    public Observable<GenderType> OnConfirmObserable() =>
        _confirmButton.OnClickAsObservable()
            .Where(_ => _selectedGenderType.HasValue && !_isConfirmButtonClicked)
            .SelectAwait(async (_, ct) =>
            {
                _isConfirmButtonClicked = true;
                var sprite = _selectedGenderType.Value == GenderType.Male ?
                _maleButton.CharacterSprite : _femaleButton.CharacterSprite;

                var messageSuffix = _selectedGenderType.Value == GenderType.Male ? "MALE" : "FEMALE";
                var message = TempTextTableContainer.GetText($"CHARA_CHAT_MESSAGE_GENDER_SELECT_CONFIRM_{messageSuffix}");

                _confirmPerformanceView.Setup(sprite, message);
                await _confirmPerformanceView.ShowAsync();
                return _selectedGenderType.Value;
            });
    private GenderType? _selectedGenderType = null;
    private void Awake()
    {
        _maleButton.OnClickObserable()
        .Merge(_femaleButton.OnClickObserable())
        .Subscribe(genderType =>
        {
            _selectedGenderType = genderType;
            _maleButton.OnAnySelected(genderType);
            _femaleButton.OnAnySelected(genderType);
            _confirmButton.interactable = true;
        }).AddTo(this);
    }
    public override void OnEnter()
    {
        base.OnEnter();
        _maleButton.OnReset();
        _femaleButton.OnReset();
        _confirmButton.interactable = false;
        _selectedGenderType = null;
        _isConfirmButtonClicked = false;
    }


}
