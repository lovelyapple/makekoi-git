using Makekoi.PartnerCreate;
using R3;
public class GameModel : SingletonBase<GameModel>
{
    public GameSessionData SessionData { get; private set; }
    private Subject<GameSessionData> _sessionDataSubject = new Subject<GameSessionData>();
    public Observable<GameSessionData> OnSessionDataChangedObservable() => _sessionDataSubject.AsObservable();
    public override void OnReset()
    {
        SessionData = new GameSessionData();
        _sessionDataSubject.OnNext(SessionData);
    }
    public void UpdateGenderType(GenderType genderType)
    {
        SessionData.SelectedGender = genderType;
        _sessionDataSubject.OnNext(SessionData);
    }

}
