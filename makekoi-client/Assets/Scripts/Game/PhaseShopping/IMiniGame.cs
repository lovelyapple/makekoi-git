using UnityEngine;

public interface IMiniGame
{
    public MiniGameType ThisMiniGameType { get; }
    public UIShoppingCurrentPartStateView CurrentPartStateView { get; set; }
    public void Init(UIShoppingCurrentPartStateView currentPartStateView);
    public void OnGameLoaded();
    public void OnGameUnloaded();
    public void OnGameStartRequested();
    public void OnGameFinishRequested();
}
public abstract class MiniGameBase : MonoBehaviour, IMiniGame
{
    public abstract MiniGameType ThisMiniGameType { get; }
    public UIShoppingCurrentPartStateView CurrentPartStateView { get; set; }
    public virtual void Init(UIShoppingCurrentPartStateView currentPartStateView) { }
    public virtual void OnGameLoaded() { }
    public virtual void OnGameUnloaded() { }
    public virtual void OnGameStartRequested() { }
    public virtual void OnGameFinishRequested() { }
}
