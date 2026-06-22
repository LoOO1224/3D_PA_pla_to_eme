using UnityEngine;

public class DaniTechThirdPersonCameraFollow : MonoBehaviour
{
    [SerializeField] private Transform Transform_Target;
    [SerializeField] private Transform Transform_InputRoot;
    [SerializeField] private float _followHeight = 1.55f;
    [SerializeField] private float _followDistance = 5.6f;
    [SerializeField] private float _minDistance = 2.4f;
    [SerializeField] private float _maxDistance = 8.5f;
    [SerializeField] private float _zoomSpeed = 5.5f;
    [SerializeField] private float _pitchMin = -25f;
    [SerializeField] private float _pitchMax = 65f;
    [SerializeField] private float _positionSmoothTime = 0.05f;
    [SerializeField] private float _rotationSmoothSpeed = 18f;

    private IDaniTechPlayerInputSource _inputSource;
    private Vector3 _followVelocity;
    private float _currentDistance;
    private float _yaw;
    private float _pitch = 20f;

    public Transform Target => Transform_Target;
    public float CurrentDistance => _currentDistance;
    public float CurrentPitch => _pitch;

    // 카메라 배우는 플레이어를 따라가는 촬영감독입니다.
    // 플레이어를 움직이지 않고, 입력 중 시점 회전과 줌만 받아 자기 위치를 계산합니다.
    private void Awake()
    {
        _currentDistance = _followDistance;
        FindInputSourceFromTarget();

        if (Transform_Target != null)
        {
            _yaw = Transform_Target.eulerAngles.y;
        }
    }

    // LateUpdate는 모든 배우가 움직인 뒤 카메라가 마지막으로 프레임을 잡는 타이밍입니다.
    private void LateUpdate()
    {
        if (Transform_Target == null)
        {
            return;
        }

        FindInputSourceFromTarget();
        RotateCamera();
        ZoomCamera();
        FollowTarget();
    }

    // 입력은 타겟 플레이어가 가진 컴포넌트에서 인터페이스로 찾습니다.
    private void FindInputSourceFromTarget()
    {
        if (Transform_Target == null || _inputSource != null)
        {
            return;
        }

        Transform inputRoot = Transform_InputRoot != null ? Transform_InputRoot : Transform_Target;
        MonoBehaviour[] behaviours = inputRoot.GetComponentsInParent<MonoBehaviour>();
        foreach (MonoBehaviour behaviour in behaviours)
        {
            if (behaviour is IDaniTechPlayerInputSource inputSource)
            {
                _inputSource = inputSource;
                return;
            }
        }

        behaviours = inputRoot.GetComponentsInChildren<MonoBehaviour>();
        foreach (MonoBehaviour behaviour in behaviours)
        {
            if (behaviour is IDaniTechPlayerInputSource inputSource)
            {
                _inputSource = inputSource;
                return;
            }
        }
    }

    // 마우스 이동량으로 카메라의 yaw, pitch를 갱신합니다.
    private void RotateCamera()
    {
        if (_inputSource == null)
        {
            return;
        }

        Vector2 lookInput = _inputSource.LookInput;
        _yaw += lookInput.x;
        _pitch = Mathf.Clamp(_pitch - lookInput.y, _pitchMin, _pitchMax);
    }

    // 마우스 휠 입력으로 3인칭 거리 Zoom을 조절합니다.
    public void ZoomCamera()
    {
        if (_inputSource == null)
        {
            return;
        }

        _currentDistance = Mathf.Clamp(
            _currentDistance - (_inputSource.ZoomInput * _zoomSpeed),
            _minDistance,
            _maxDistance);
    }

    // 타겟 뒤쪽의 원하는 위치를 계산하고 부드럽게 따라갑니다.
    private void FollowTarget()
    {
        Quaternion targetRotation = Quaternion.Euler(_pitch, _yaw, 0f);
        Vector3 targetCenter = Transform_Target.position + Vector3.up * _followHeight;
        Vector3 desiredPosition = targetCenter + (targetRotation * new Vector3(0f, 0f, -_currentDistance));

        transform.position = Vector3.SmoothDamp(
            transform.position,
            desiredPosition,
            ref _followVelocity,
            _positionSmoothTime);

        Quaternion lookRotation = Quaternion.LookRotation(targetCenter - transform.position, Vector3.up);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            lookRotation,
            _rotationSmoothSpeed * Time.deltaTime);
    }
}
