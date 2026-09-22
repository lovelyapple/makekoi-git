using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 桁ごとの <see cref="UINumberRoller"/> をまとめて回して数値を表示する。
/// 増える時は右の桁から、減る時は左の桁から順に回り出す。
/// 数字がなくなる上位の桁は、0 へ送らずに回した勢いのまま数字だけ消す (桁枠は残る)。
/// </summary>
public class UINumberRollerGroup : MonoBehaviour
{
    [Header("左(上位)から右(下位)の順に並べる")]
    [SerializeField] private List<UINumberRoller> _rollers = new List<UINumberRoller>();

    [Header("Roll")]
    [SerializeField] private float _digitDelay = 0.08f;   // 隣の桁が回り出すまでの間
    [SerializeField] private bool _hideLeadingZeros = true;

    private CancellationTokenSource _cts;

    /// <summary>今表示している値。</summary>
    public int CurrentValue { get; private set; }

    /// <summary>桁数で表せる最大値。</summary>
    public int MaxValue
    {
        get
        {
            var max = 1;
            for (var i = 0; i < _rollers.Count; i++) max *= 10;
            return max - 1;
        }
    }

    private void Awake()
    {
        SetValueImmediate(CurrentValue);
    }

    private void OnDestroy()
    {
        CancelRoll();
#if UNITY_EDITOR
        StopEditorRoll();
#endif
    }

    /// <summary>アニメーションなしで値を差し替える。</summary>
    public void SetValueImmediate(int value)
    {
        CancelRoll();

        var clamped = Mathf.Clamp(value, 0, MaxValue);
        var digits = SplitDigits(clamped);
        var firstVisible = FirstVisibleIndex(clamped);

        for (var i = 0; i < _rollers.Count; i++)
        {
            if (_rollers[i] == null) continue;
            _rollers[i].SetNumberImmediate(digits[i]);
            SetDigitNumberVisible(i, i >= firstVisible);
        }

        CurrentValue = clamped;
    }

    /// <summary>ドラムを回して value を表示する。回している途中に呼ぶと、前の回転は打ち切る。</summary>
    public UniTask RollToAsync(int value, CancellationToken ct = default) => RollToAsync(value, false, ct);

    /// <summary>
    /// 周回も桁ごとの時間差もなしで value まで回す。
    /// プレイ中に値を追いかけて出すときは、回り続けると読めないのでこちらを使う。
    /// </summary>
    public UniTask FollowToAsync(int value, CancellationToken ct = default) => RollToAsync(value, true, ct);

    private async UniTask RollToAsync(int value, bool follow, CancellationToken ct = default)
    {
        CancelRoll();
        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct, this.GetCancellationTokenOnDestroy());
        var token = _cts.Token;

        var clamped = Mathf.Clamp(value, 0, MaxValue);
        // 増えるなら右の桁から、減るなら左の桁から回り出す
        var increasing = clamped >= CurrentValue;
        var digits = SplitDigits(clamped);
        var firstVisible = FirstVisibleIndex(clamped);

        // 桁が増える分は回す前に出しておく
        for (var i = firstVisible; i < _rollers.Count; i++) SetDigitNumberVisible(i, true);

        CurrentValue = clamped;

        var rolls = new List<UniTask>(_rollers.Count);
        for (var i = 0; i < _rollers.Count; i++)
        {
            var roller = _rollers[i];
            if (roller == null) continue;

            var willHide = i < firstVisible;

            if (willHide && !roller.IsNumberVisible)
            {
                // 前から数字が出ていない桁は回す意味がないので黙って合わせる
                roller.SetNumberImmediate(digits[i]);
                continue;
            }

            var order = increasing ? _rollers.Count - 1 - i : i;
            var delay = follow ? 0f : order * _digitDelay;

            // 消えると分かっている桁は 0 へ送らず、回した勢いのまま消す
            rolls.Add(willHide
                ? SpinOutDigitAsync(roller, delay, follow, token)
                : RollDigitAsync(roller, digits[i], delay, follow, token));
        }

        await UniTask.WhenAll(rolls);
    }

    private async UniTask RollDigitAsync(UINumberRoller roller, int number, float delay, bool follow, CancellationToken ct)
    {
        if (delay > 0f)
        {
            if (await UniTask.Delay(TimeSpan.FromSeconds(delay), cancellationToken: ct).SuppressCancellationThrow()) return;
        }

        if (follow) await roller.FollowToAsync(number, ct);
        else await roller.RollToAsync(number, ct);
    }

    // 消える桁。回すだけ回して、止まると同時に消すので 0 は表に出ない
    private async UniTask SpinOutDigitAsync(UINumberRoller roller, float delay, bool follow, CancellationToken ct)
    {
        if (!follow)
        {
            if (delay > 0f)
            {
                if (await UniTask.Delay(TimeSpan.FromSeconds(delay), cancellationToken: ct).SuppressCancellationThrow()) return;
            }

            await roller.SpinAsync(ct);
            if (ct.IsCancellationRequested) return;
        }

        roller.SetNumberVisible(false);
    }

    private void CancelRoll()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
    }

    // 左(上位)から右(下位)の順で桁ごとの数字を返す
    private int[] SplitDigits(int value)
    {
        var digits = new int[_rollers.Count];
        for (var i = _rollers.Count - 1; i >= 0; i--)
        {
            digits[i] = value % 10;
            value /= 10;
        }
        return digits;
    }

    // 数字を出し始める桁の位置。これより左は数字を消す
    private int FirstVisibleIndex(int value)
    {
        if (!_hideLeadingZeros) return 0;

        var used = 1;
        while (value >= 10 && used < _rollers.Count)
        {
            value /= 10;
            used++;
        }
        return _rollers.Count - used;
    }

    // 桁枠は常に出したままで、中央の数字だけを出し入れする
    private void SetDigitNumberVisible(int index, bool visible)
    {
        var roller = _rollers[index];
        if (roller == null) return;
        roller.SetNumberVisible(visible);
    }

#if UNITY_EDITOR
    [Header("Debug")]
    [SerializeField] private int _debugValue = 12345;

    private UnityEditor.EditorApplication.CallbackFunction _editorRollStep;

    [ContextMenu("Debug Roll To Value")]
    private void DebugRollToValue()
    {
        if (Application.isPlaying)
        {
            RollToAsync(_debugValue).Forget();
            return;
        }
        // Play していないと PlayerLoop が回らないので、エディタの update で時間を進める
        DebugRollInEditor(_debugValue);
    }

    [ContextMenu("Debug Set Value Immediate")]
    private void DebugSetValueImmediate()
    {
        StopEditorRoll();
        SetValueImmediate(_debugValue);
        RepaintInEditor();
    }

    [ContextMenu("Debug Stop Roll")]
    private void DebugStopRoll()
    {
        CancelRoll();
        StopEditorRoll();
    }

    private void DebugRollInEditor(int value)
    {
        StopEditorRoll();

        var clamped = Mathf.Clamp(value, 0, MaxValue);
        var increasing = clamped >= CurrentValue;
        var digits = SplitDigits(clamped);
        var firstVisible = FirstVisibleIndex(clamped);

        for (var i = firstVisible; i < _rollers.Count; i++) SetDigitNumberVisible(i, true);

        CurrentValue = clamped;

        // 遅延を消化したら順に回し始める
        var waiting = new List<(UINumberRoller Roller, int Number, float Delay, bool WillHide)>();
        // 回し終わり次第に消す桁
        var spinningOut = new List<UINumberRoller>();
        for (var i = 0; i < _rollers.Count; i++)
        {
            var roller = _rollers[i];
            if (roller == null) continue;

            var willHide = i < firstVisible;
            if (willHide && !roller.IsNumberVisible)
            {
                roller.SetNumberImmediate(digits[i]);
                continue;
            }

            var order = increasing ? _rollers.Count - 1 - i : i;
            waiting.Add((roller, digits[i], order * _digitDelay, willHide));
        }

        var startTime = UnityEditor.EditorApplication.timeSinceStartup;
        _editorRollStep = () =>
        {
            if (this == null || Application.isPlaying)
            {
                StopEditorRoll();
                return;
            }

            var elapsed = (float)(UnityEditor.EditorApplication.timeSinceStartup - startTime);
            for (var i = waiting.Count - 1; i >= 0; i--)
            {
                if (elapsed < waiting[i].Delay) continue;

                var next = waiting[i];
                if (next.WillHide)
                {
                    // 消える桁は 0 へ送らず、数字はそのままで回すだけ回す
                    next.Roller.RollToInEditor(next.Roller.CurrentNumber);
                    spinningOut.Add(next.Roller);
                }
                else
                {
                    next.Roller.RollToInEditor(next.Number);
                }
                waiting.RemoveAt(i);
            }

            // 回り終わった桁はその場で消す。0 を見せない
            for (var i = spinningOut.Count - 1; i >= 0; i--)
            {
                if (spinningOut[i].IsRollingInEditor) continue;
                spinningOut[i].SetNumberVisible(false);
                spinningOut.RemoveAt(i);
                RepaintInEditor();
            }

            if (waiting.Count > 0 || spinningOut.Count > 0) return;

            // 回り出すのを待っている桁がなくなっても、まだ回っている桁があれば待つ
            for (var i = 0; i < _rollers.Count; i++)
            {
                if (_rollers[i] != null && _rollers[i].IsRollingInEditor) return;
            }

            StopEditorRoll();
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
