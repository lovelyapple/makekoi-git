using UnityEngine;

public class UIFrameRoller : MonoBehaviour
{
    public enum RollState
    {
        Idle,
        Rolling,
        MaxSpeed,
    }
    [SerializeField] private bool _rollToRight = true;
    [SerializeField] private float _rollBaseSpeed = 1f;
    [SerializeField] private float _rollSpeedMaxSpeed = 5.0f;
    [SerializeField] private float _floatingRange = 0.1f;
    [SerializeField] private float _floatingMoveSpeed = 1f;
    [SerializeField] private RollState _rollState = RollState.Idle;

#if UNITY_EDITOR
    [SerializeField] private RollState DebugRollState = RollState.Idle;
    [ContextMenu("Debug Set Roll State")]
    public void DebugSetRollState()
    {
        SetState(DebugRollState);
    }
#endif
    private Vector3 _initialLocalPosition;
    private Vector2 _floatingVelocity;

    private void Awake()
    {
        _initialLocalPosition = transform.localPosition;
        InitFloatingVelocity();
    }

    private void InitFloatingVelocity()
    {
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        _floatingVelocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * _floatingMoveSpeed;
    }

    public void SetState(RollState state)
    {
        if (_rollState == state) return;
        var prev = _rollState;
        _rollState = state;
        if (state == RollState.Idle) return;
        // IdleからRolling/MaxSpeedに戻ったとき速度を再初期化
        if (prev == RollState.Idle)
        {
            InitFloatingVelocity();
        }
    }

    private void Update()
    {
        switch (_rollState)
        {
            case RollState.Idle:
                transform.localPosition = Vector3.Lerp(transform.localPosition, _initialLocalPosition, Time.deltaTime * 5f);
                transform.localRotation = Quaternion.Lerp(transform.localRotation, Quaternion.identity, Time.deltaTime * 5f);
                break;

            case RollState.Rolling:
                Rotate(_rollBaseSpeed);
                Float();
                break;

            case RollState.MaxSpeed:
                Rotate(_rollSpeedMaxSpeed);
                Float();
                break;
        }
    }

    private void Rotate(float speed)
    {
        float direction = _rollToRight ? -1f : 1f;
        transform.Rotate(0f, 0f, direction * speed * Time.deltaTime);
    }

    private void Float()
    {
        Vector3 pos = transform.localPosition;
        pos.x += _floatingVelocity.x * Time.deltaTime;
        pos.y += _floatingVelocity.y * Time.deltaTime;

        float minX = _initialLocalPosition.x - _floatingRange;
        float maxX = _initialLocalPosition.x + _floatingRange;
        float minY = _initialLocalPosition.y - _floatingRange;
        float maxY = _initialLocalPosition.y + _floatingRange;

        if (pos.x < minX || pos.x > maxX)
        {
            pos.x = Mathf.Clamp(pos.x, minX, maxX);
            ReflectX();
        }

        if (pos.y < minY || pos.y > maxY)
        {
            pos.y = Mathf.Clamp(pos.y, minY, maxY);
            ReflectY();
        }

        transform.localPosition = new Vector3(pos.x, pos.y, _initialLocalPosition.z);
    }

    // X軸で反射（± 15度のランダムを加える）
    private void ReflectX()
    {
        float speed = _floatingVelocity.magnitude;
        float angle = Mathf.Atan2(_floatingVelocity.y, _floatingVelocity.x);
        float reflected = Mathf.PI - angle + Random.Range(-15f, 15f) * Mathf.Deg2Rad;
        _floatingVelocity = new Vector2(Mathf.Cos(reflected), Mathf.Sin(reflected)) * speed;
    }

    // Y軸で反射（± 15度のランダムを加える）
    private void ReflectY()
    {
        float speed = _floatingVelocity.magnitude;
        float angle = Mathf.Atan2(_floatingVelocity.y, _floatingVelocity.x);
        float reflected = -angle + Random.Range(-15f, 15f) * Mathf.Deg2Rad;
        _floatingVelocity = new Vector2(Mathf.Cos(reflected), Mathf.Sin(reflected)) * speed;
    }
}
