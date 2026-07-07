using R3;
using UnityEngine;

public class AnimationTriggerFinishedEvent : MonoBehaviour
{
    private Subject<Unit> OnAnimationTriggerFinishedSubject = new Subject<Unit>();
    public Observable<Unit> OnAnimationTriggerFinishedObservable() => OnAnimationTriggerFinishedSubject.AsObservable();
    public void OnAnimationTriggerFinished()
    {
        OnAnimationTriggerFinishedSubject.OnNext(Unit.Default);
    }
}
