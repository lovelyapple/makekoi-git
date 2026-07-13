using UnityEngine;
using TMPro;
using R3;

public class UIGlobalHeaderController : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _currentMoneyText;
    private int _currentPlayerMoney = 0;
    private void Awake()
    {
        UpdatePlayerMoneyUI(_currentPlayerMoney);
        GameModel.Instance.OnSessionDataChangedObservable()
            .Where(sessionData => sessionData.PlayerMoney != _currentPlayerMoney)
            .Subscribe(sessionData =>
            {
                if (_currentPlayerMoney != sessionData.PlayerMoney)
                {
                    _currentPlayerMoney = sessionData.PlayerMoney;
                    UpdatePlayerMoneyUI(_currentPlayerMoney);
                }
            }).AddTo(this);
    }
    private void UpdatePlayerMoneyUI(int playerMoney)
    {
        _currentMoneyText.text = $"${playerMoney.ToString("N0")}";
    }
}
