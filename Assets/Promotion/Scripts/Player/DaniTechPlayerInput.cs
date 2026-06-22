using UnityEngine;

public class DaniTechPlayerInput : MonoBehaviour, IDaniTechPlayerInputSource
{
    [SerializeField] private float _mouseSensitivity = 1.7f;

    private Vector2 _moveInput;
    private Vector2 _lookInput;
    private bool _isRunPressed;
    private bool _jumpPressed;
    private float _zoomInput;

    public Vector2 MoveInput => _moveInput;
    public Vector2 LookInput => _lookInput;
    public bool IsRunPressed => _isRunPressed;
    public bool JumpPressed => _jumpPressed;
    public float ZoomInput => _zoomInput;

    // Update는 매 프레임 현장의 신호를 받아 적는 슬레이트입니다.
    // 이 컴포넌트는 입력을 읽기만 하고, 배우를 직접 움직이지 않습니다.
    private void Update()
    {
        ReadMoveInput();
        ReadLookInput();
        ReadActionInput();
    }

    // WASD 입력을 한 장의 큐시트처럼 Vector2로 모읍니다.
    // 실제 이동 방향으로 바꾸는 일은 Movement 배우가 담당합니다.
    private void ReadMoveInput()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        _moveInput = Vector2.ClampMagnitude(new Vector2(horizontal, vertical), 1f);
    }

    // 마우스 입력은 카메라 배우에게 넘길 시점 회전 큐입니다.
    // 감도는 Inspector에서 바꿀 수 있게 변수로 열어 둡니다.
    private void ReadLookInput()
    {
        float mouseX = Input.GetAxis("Mouse X") * _mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * _mouseSensitivity;
        _lookInput = new Vector2(mouseX, mouseY);
    }

    // 점프, 달리기, 줌처럼 순간 선택이 필요한 신호를 모읍니다.
    // JumpPressed는 GetKeyDown이므로 한 번 누른 장면 컷만 점프 컴포넌트에 전달됩니다.
    private void ReadActionInput()
    {
        _isRunPressed = Input.GetKey(KeyCode.LeftShift);
        _jumpPressed = Input.GetKeyDown(KeyCode.Space);
        _zoomInput = Input.GetAxis("Mouse ScrollWheel");
    }
}
