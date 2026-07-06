public class GameModel : SingletonBase<GameModel>
{
    public GameSessionData SessionData { get; private set; }
    public override void OnInitialize()
    {
        SessionData = new GameSessionData();
    }

}
