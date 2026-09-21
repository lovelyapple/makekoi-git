using UnityEngine;
using TMPro;
using Cysharp.Threading.Tasks;
using R3;

public class MiniGamePerformController : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _miniGameFixLabel;
    [SerializeField] private TextMeshProUGUI _miniGameNameGUI;
    [SerializeField] private float _nameRevealDelay = 0.5f; // 固定ラベルだけを見せる溜めの時間
    [SerializeField] private float _nameReadDuration = 1f;  // ゲーム名を読ませる時間

    public async UniTask<Unit> PerformMiniGame(MiniGameType gameType)
    {
        _miniGameFixLabel.gameObject.SetActive(true);
        _miniGameNameGUI.gameObject.SetActive(false);
        _miniGameNameGUI.text = GetMiniGameName(gameType);

        this.gameObject.SetActive(true);

        // 固定ラベルで溜めてからゲーム名を出し、読ませてから閉じる
        await UniTask.Delay(System.TimeSpan.FromSeconds(_nameRevealDelay),
            cancellationToken: this.GetCancellationTokenOnDestroy());
        _miniGameNameGUI.gameObject.SetActive(true);
        await UniTask.Delay(System.TimeSpan.FromSeconds(_nameReadDuration),
            cancellationToken: this.GetCancellationTokenOnDestroy());

        this.gameObject.SetActive(false);
        return Unit.Default;
    }

    // 次のミニゲームで何をすればいいのかを一言で伝える
    private static string GetMiniGameName(MiniGameType gameType)
    {
        var key = gameType switch
        {
            MiniGameType.Dogeza => "MINI_GAME_NAME_DOGEZA",
            MiniGameType.TimeTap => "MINI_GAME_NAME_TIME_TAP",
            MiniGameType.Jyanken => "MINI_GAME_NAME_JYANKEN",
            _ => null,
        };

        var text = key != null ? TempTextTableContainer.GetText(key) : null;
        if (text == null)
        {
            Debug.LogWarning($"[MiniGamePerformController] {gameType} の表示名がありません (key={key})");
            return gameType.ToString();
        }
        return text;
    }
}
