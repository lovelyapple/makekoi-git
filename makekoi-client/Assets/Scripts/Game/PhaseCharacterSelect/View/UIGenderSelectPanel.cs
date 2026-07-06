using R3;
using UnityEngine;
using UnityEngine.UI;
using Makekoi.PartnerCreate;

public class UIGenderSelectPanel : UICharacterSelectPanelBase
{
    [SerializeField] private UIGenderSelectButton _maleButton;
    [SerializeField] private UIGenderSelectButton _femaleButton;
    [SerializeField] private Button _confirmButton;
    public Observable<GenderType> OnConfirmObserable() =>
        _confirmButton.OnClickAsObservable()
            .Where(_ => _selectedGenderType.HasValue)
            .Select(_ => _selectedGenderType.Value);
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
    }

}
