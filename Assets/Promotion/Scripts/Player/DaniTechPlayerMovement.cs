using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class DaniTechPlayerMovement : MonoBehaviour
{
    [SerializeField] private Rigidbody Rigidbody_Player;
    [SerializeField] private Transform Transform_CameraRoot;
    [SerializeField] private float _walkSpeed = 3.2f;
    [SerializeField] private float _runSpeed = 6.2f;
    [SerializeField] private float _rotationSmoothSpeed = 14f;

    private IDaniTechPlayerInputSource _inputSource;
    private DaniTechPlayerStamina _playerStamina;
    private Vector3 _moveDirection;
    private float _currentMoveSpeed;
    private bool _isRunning;

    public Vector3 MoveDirection => _moveDirection;
    public float CurrentMoveSpeed => _currentMoveSpeed;
    public float RunSpeed => _runSpeed;
    public bool IsRunning => _isRunning;
    public bool IsMoving => _moveDirection.sqrMagnitude > 0.01f;

    // Awake는 이동 배우가 필요한 동료 컴포넌트를 자기 몸에서 찾아 캐스팅하는 장면입니다.
    // Controller가 참조를 몰아 갖지 않고, 배우가 자기 역할표를 직접 확인합니다.
    private void Awake()
    {
        if (Rigidbody_Player == null)
        {
            Rigidbody_Player = GetComponent<Rigidbody>();
        }

        _inputSource = GetInputSource();
        _playerStamina = GetComponent<DaniTechPlayerStamina>();

        if (Transform_CameraRoot == null && Camera.main != null)
        {
            Transform_CameraRoot = Camera.main.transform;
        }

        Rigidbody_Player.freezeRotation = true;
        Rigidbody_Player.interpolation = RigidbodyInterpolation.Interpolate;
        Rigidbody_Player.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }

    // FixedUpdate는 물리 기반 이동 촬영 타이밍입니다.
    // Rigidbody의 y 속도는 중력과 점프가 맡고, 이 컴포넌트는 수평 이동만 연출합니다.
    private void FixedUpdate()
    {
        UpdateMoveDirection();
        UpdateMoveSpeed();
        MovePlayer();
        RotatePlayer();
        ReportPositionToModel();
    }

    // 입력 컴포넌트는 인터페이스로 찾습니다.
    // 나중에 테스트용 입력 배우로 바꿔도 Movement 코드는 그대로 재사용됩니다.
    private IDaniTechPlayerInputSource GetInputSource()
    {
        MonoBehaviour[] behaviours = GetComponents<MonoBehaviour>();
        foreach (MonoBehaviour behaviour in behaviours)
        {
            if (behaviour is IDaniTechPlayerInputSource inputSource)
            {
                return inputSource;
            }
        }

        Debug.LogWarning("플레이어 입력 컴포넌트를 찾을 수 없습니다.");
        return null;
    }

    // 카메라가 보는 방향을 기준으로 WASD를 월드 이동 방향으로 바꿉니다.
    private void UpdateMoveDirection()
    {
        if (_inputSource == null)
        {
            _moveDirection = Vector3.zero;
            return;
        }

        _moveDirection = DaniTechPlayerLocomotionUtil.CalculateCameraRelativeMoveDirection(
            _inputSource.MoveInput,
            Transform_CameraRoot);
    }

    // 이동속도는 하드코딩하지 않고 Inspector 변수로 관리합니다.
    // 달리기는 스태미너 배우에게 허락을 받은 뒤에만 Run 속도를 씁니다.
    private void UpdateMoveSpeed()
    {
        _isRunning = false;

        if (_inputSource == null || IsMoving == false)
        {
            _currentMoveSpeed = 0f;
            return;
        }

        bool wantsRun = _inputSource.IsRunPressed;
        bool canRun = _playerStamina == null || _playerStamina.SpendRunStamina(Time.fixedDeltaTime);
        _isRunning = wantsRun && canRun;
        _currentMoveSpeed = _isRunning ? _runSpeed : _walkSpeed;
    }

    // Rigidbody의 선형 속도를 갱신해 물리 기반 이동을 수행합니다.
    // y축은 건드리지 않아 Unity 중력과 점프 힘이 자연스럽게 상승, 낙하를 담당합니다.
    private void MovePlayer()
    {
        Vector3 currentVelocity = Rigidbody_Player.linearVelocity;
        Vector3 horizontalVelocity = _moveDirection * _currentMoveSpeed;
        Rigidbody_Player.linearVelocity = new Vector3(horizontalVelocity.x, currentVelocity.y, horizontalVelocity.z);
    }

    // 배우가 이동 방향을 바라보도록 회전합니다.
    // 카메라 컷이 바뀌어도 몸 회전은 이동 방향을 따라 부드럽게 이어집니다.
    private void RotatePlayer()
    {
        if (IsMoving == false)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(_moveDirection, Vector3.up);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            _rotationSmoothSpeed * Time.fixedDeltaTime);
    }

    // 저장 후보 위치만 GameManager의 Model에 남깁니다.
    private void ReportPositionToModel()
    {
        if (DaniTechGameManager.Inst == null)
        {
            return;
        }

        DaniTechGameManager.Inst.SetLastPlayerPosition(transform.position);
    }
}
