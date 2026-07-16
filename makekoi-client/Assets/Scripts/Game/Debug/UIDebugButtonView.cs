using R3;
using UnityEngine;
using UnityEngine.UI;

public class UIDebugButtonView : MonoBehaviour
{
    [SerializeField] private Button _debugFinishMiniGameButton;
    private void Awake()
    {
        _debugFinishMiniGameButton.OnClickAsObservable()
            .Subscribe(_ =>
            {
                DebugModel.Instance.RequestFinishCurrentMiniGame();
            }).AddTo(this);
    }
}
