public abstract class SingletonBase<T> where T : SingletonBase<T>, new()
{
    private static T _instance;
    public static T Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new T();
                _instance.OnReset();
            }
            return _instance;
        }
    }

    public virtual void OnReset() { }
}
