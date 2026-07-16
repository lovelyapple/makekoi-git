using R3;
using UnityEngine;

public class DebugModel : SingletonBase<DebugModel>
{
    private Subject<Unit> _onFinishCurrentMiniGameRequested = new Subject<Unit>();
    public Observable<Unit> OnFinishCurrentMiniGameRequested() => _onFinishCurrentMiniGameRequested.AsObservable();
    public void RequestFinishCurrentMiniGame()
    {
        _onFinishCurrentMiniGameRequested.OnNext(Unit.Default);
    }
}
