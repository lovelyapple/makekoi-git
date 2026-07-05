using UnityEngine;

public class MonoSingletoneBase<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T _instance;
    public static T Instance
    {
        get => _instance;
    }

    public void Initialize(T instance)
    {
        _instance = instance;
    }
}
