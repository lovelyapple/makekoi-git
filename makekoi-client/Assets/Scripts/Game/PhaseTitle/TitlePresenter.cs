using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;

public class TitlePresenter : GamePhaseWindowBase
{
    public override GamePhaseContentType ThisPhaseContentType => GamePhaseContentType.Title;
    [SerializeField] private TitleView _view;
    private void Awake()
    {
        _view.OnStartButtonClicked
        .Subscribe(async _ => await RunStartAsync())
        .AddTo(this);
    }
    public override void OnEnter()
    {
        _view.OnReset();
    }
    private async UniTask<Unit> RunStartAsync()
    {
        await _view.FadeInButtonAsync(this.GetCancellationTokenOnDestroy());
        RequestGoToNextPhase();
        return Unit.Default;
    }
}
