using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 数字1桁分のドラム。上・中央・下の3枚を縦に流し続け、指定した数字で止める。
/// 桁をまたぐ繰り上がりは扱わないので、複数桁は並べた数だけ呼び出し側で回す。
/// </summary>
public class UINumberRoller : MonoBehaviour
{
    private const int DigitCount = 10;

    [Header("0〜9 の順に並べる")]
    [SerializeField] private Sprite[] _numberSprites = new Sprite[DigitCount];

    [Header("上・中央・下の3枚。中央が今出ている数字")]
    [SerializeField] private Image _upDigit;
    [SerializeField] private Image _centerDigit;
    [SerializeField] private Image _downDigit;

    [Header("Roll")]
    [SerializeField] private float _step = 58f;         // 数字1つ分の縦移動量
    [SerializeField] private int _extraSpins = 2;        // 止まるまでに余分に回す周回数
    [SerializeField] private float _duration = 0.6f;
    [SerializeField] private float _overshootDigits = 0.35f; // 行き過ぎる量を数字何個分で指定。0 で行き過ぎなし
    [SerializeField, Range(0.05f, 0.5f)] private float _settleRatio = 0.25f; // 行き過ぎから戻すのに使う時間の割合
    [SerializeField] private bool _rollDownward = true;  // 数字が上から下へ流れる

    // 連続位置。整数部が中央に出ている数字、小数部が次の数字への進み具合
    private float _position;
    private CancellationTokenSource _cts;

    /// <summary>中央に出ている数字。</summary>
    public int CurrentNumber => Mod(Mathf.FloorToInt(_position), DigitCount);

    public bool IsRolling { get; private set; }

    private void Awake()
    {
        Apply(_position);
    }

    private void OnDestroy()
    {
        CancelRoll();
#if UNITY_EDITOR
        StopEditorRoll();
#endif
    }

    /// <summary>
    /// 数字の表示/非表示。桁枠は残したまま中央の数字だけ消すので、
    /// 上位の桁に数字がなくても枠の並びは崩れない。
    /// </summary>
    public void SetNumberVisible(bool visible)
    {
        if (_centerDigit == null) return;
        if (_centerDigit.gameObject.activeSelf != visible) _centerDigit.gameObject.SetActive(visible);
    }

    /// <summary>数字が表示されているか。</summary>
    public bool IsNumberVisible => _centerDigit == null || _centerDigit.gameObject.activeSelf;

    /// <summary>アニメーションなしで数字を差し替える。</summary>
    public void SetNumberImmediate(int number)
    {
        CancelRoll();
        _position = Mod(number, DigitCount);
        Apply(_position);
    }

    /// <summary>ドラムを回して number で止める。回している途中に呼ぶと、前の回転は打ち切る。</summary>
    public UniTask RollToAsync(int number, CancellationToken ct = default) => RollToAsync(number, _extraSpins, ct);

    /// <summary>
    /// 余分な周回をせず最短で number まで回す。
    /// プレイ中に値を追いかけて出すときは、周回すると読めないのでこちらを使う。
    /// </summary>
    public UniTask FollowToAsync(int number, CancellationToken ct = default) => RollToAsync(number, 0, ct);

    /// <summary>
    /// 数字を変えずに回すだけ回す。
    /// 桁が消えると分かっているときに、0 まで送らずに演出だけ見せるために使う。
    /// </summary>
    public UniTask SpinAsync(CancellationToken ct = default)
        => RollToAsync(CurrentNumber, Mathf.Max(1, _extraSpins), ct);

    /// <summary>ドラムを回して number で止める。extraSpins で余分な周回数を指定する。</summary>
    public async UniTask RollToAsync(int number, int extraSpins, CancellationToken ct = default)
    {
        CancelRoll();
        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct, this.GetCancellationTokenOnDestroy());
        var token = _cts.Token;

        var start = _position;
        var startNumber = Mathf.FloorToInt(start);
        // 目標の数字に着地するまでの距離。余分な周回を足してドラムらしく回す
        var distance = Mathf.Max(0, extraSpins) * DigitCount
                       + Mod(number - startNumber, DigitCount)
                       - (start - startNumber);

        IsRolling = true;
        try
        {
            var elapsed = 0f;
            while (elapsed < _duration)
            {
                // キャンセルされたら数字を確定させずに抜ける
                if (await UniTask.Yield(PlayerLoopTiming.Update, token).SuppressCancellationThrow()) return;

                elapsed += Time.deltaTime;
                _position = start + RollOffset(Mathf.Clamp01(elapsed / _duration), distance);
                Apply(_position);
            }

            _position = Mod(number, DigitCount);
            Apply(_position);
        }
        finally
        {
            IsRolling = false;
        }
    }

    private void CancelRoll()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
    }

    /// <summary>
    /// 経過割合 t (0〜1) に対して、開始位置からどれだけ進んだかを返す。
    /// 行き過ぎ量は距離ではなく数字何個分で決めるので、何周回しても戻り幅は変わらない。
    /// </summary>
    private float RollOffset(float t, float distance)
    {
        if (_overshootDigits <= 0f) return distance * EaseOutCubic(t);

        var peak = distance + _overshootDigits;
        var rollRatio = 1f - _settleRatio;
        if (t < rollRatio) return peak * EaseOutCubic(t / rollRatio);

        // 行き過ぎた分を目標へ戻す
        return Mathf.Lerp(peak, distance, EaseOutCubic((t - rollRatio) / _settleRatio));
    }

    private static float EaseOutCubic(float t)
    {
        var inverted = t - 1f;
        return 1f + inverted * inverted * inverted;
    }

    private void Apply(float position)
    {
        var number = Mathf.FloorToInt(position);
        var fraction = position - number;
        // 流れる向き。下向きなら中央の数字は下へ抜けていく
        var direction = _rollDownward ? -1f : 1f;
        var centerY = direction * fraction * _step;

        SetSlot(_centerDigit, number, centerY);
        SetSlot(_upDigit, _rollDownward ? number + 1 : number - 1, centerY + _step);
        SetSlot(_downDigit, _rollDownward ? number - 1 : number + 1, centerY - _step);
    }

    private void SetSlot(Image image, int number, float y)
    {
        if (image == null) return;

        var sprite = GetSprite(number);
        if (image.sprite != sprite) image.sprite = sprite;

        var rectTransform = image.rectTransform;
        var anchoredPosition = rectTransform.anchoredPosition;
        if (!Mathf.Approximately(anchoredPosition.y, y))
        {
            rectTransform.anchoredPosition = new Vector2(anchoredPosition.x, y);
        }
    }

    private Sprite GetSprite(int number)
    {
        if (_numberSprites == null || _numberSprites.Length == 0) return null;
        return _numberSprites[Mod(number, _numberSprites.Length)];
    }

    private static int Mod(int value, int length) => ((value % length) + length) % length;

#if UNITY_EDITOR
    [Header("Debug")]
    [SerializeField, Range(0, DigitCount - 1)] private int _debugNumber = 7;

    private UnityEditor.EditorApplication.CallbackFunction _editorRollStep;

    /// <summary>Play していないときに回す。上位のグループからデバッグ用に呼ぶ。</summary>
    public void RollToInEditor(int number) => DebugRollInEditor(number);

    /// <summary>Play していないときに回っているか。</summary>
    public bool IsRollingInEditor => _editorRollStep != null;

    [ContextMenu("Debug Roll To Number")]
    private void DebugRollToNumber()
    {
        if (Application.isPlaying)
        {
            RollToAsync(_debugNumber).Forget();
            return;
        }
        // Play していないと PlayerLoop が回らないので、エディタの update で時間を進める
        DebugRollInEditor(_debugNumber);
    }

    [ContextMenu("Debug Set Number Immediate")]
    private void DebugSetNumberImmediate()
    {
        StopEditorRoll();
        SetNumberImmediate(_debugNumber);
        RepaintInEditor();
    }

    [ContextMenu("Debug Stop Roll")]
    private void DebugStopRoll()
    {
        CancelRoll();
        StopEditorRoll();
    }

    private void DebugRollInEditor(int number)
    {
        StopEditorRoll();

        var start = _position;
        var startNumber = Mathf.FloorToInt(start);
        var distance = _extraSpins * DigitCount
                       + Mod(number - startNumber, DigitCount)
                       - (start - startNumber);
        var startTime = UnityEditor.EditorApplication.timeSinceStartup;

        _editorRollStep = () =>
        {
            // Inspector から外されたり Play に入ったら止める
            if (this == null || Application.isPlaying)
            {
                StopEditorRoll();
                return;
            }

            var elapsed = (float)(UnityEditor.EditorApplication.timeSinceStartup - startTime);
            if (elapsed < _duration)
            {
                _position = start + RollOffset(elapsed / _duration, distance);
            }
            else
            {
                _position = Mod(number, DigitCount);
                StopEditorRoll();
            }

            Apply(_position);
            RepaintInEditor();
        };
        UnityEditor.EditorApplication.update += _editorRollStep;
    }

    private void StopEditorRoll()
    {
        if (_editorRollStep == null) return;
        UnityEditor.EditorApplication.update -= _editorRollStep;
        _editorRollStep = null;
    }

    private void RepaintInEditor()
    {
        UnityEditor.EditorUtility.SetDirty(this);
        Canvas.ForceUpdateCanvases();
        UnityEditor.SceneView.RepaintAll();
    }
#endif
}
