using Makekoi.PartnerCreate;
public class GameModel : SingletonBase<GameModel>
{
    public GameSessionData SessionData { get; private set; }
    public override void OnReset()
    {
        SessionData = new GameSessionData();
    }
    public void UpdateGenderType(GenderType genderType)
    {
        SessionData.SelectedGender = genderType;
    }

}
