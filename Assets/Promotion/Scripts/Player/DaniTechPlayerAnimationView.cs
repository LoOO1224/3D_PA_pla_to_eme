using UnityEngine;

public class DaniTechPlayerAnimationView : MonoBehaviour
{
    private static readonly int MoveSpeedHash = Animator.StringToHash("MoveSpeed");
    private static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");
    private static readonly int VerticalSpeedHash = Animator.StringToHash("VerticalSpeed");
    private static readonly int JumpTriggerHash = Animator.StringToHash("JumpTrigger");

    [SerializeField] private Animator Animator_Player;
    [SerializeField] private Rigidbody Rigidbody_Player;

    private DaniTechPlayerMovement _playerMovement;
    private DaniTechPlayerJump _playerJump;
    private DaniTechPlayerGroundChecker _groundChecker;

    // AnimationView는 배우의 표정을 담당하는 촬영감독입니다.
    // 이동이나 점프를 직접 실행하지 않고, 다른 컴포넌트의 상태를 Animator 파라미터로 번역합니다.
    private void Awake()
    {
        if (Animator_Player == null)
        {
            Animator_Player = GetComponentInChildren<Animator>();
        }

        if (Rigidbody_Player == null)
        {
            Rigidbody_Player = GetComponent<Rigidbody>();
        }

        _playerMovement = GetComponent<DaniTechPlayerMovement>();
        _playerJump = GetComponent<DaniTechPlayerJump>();
        _groundChecker = GetComponent<DaniTechPlayerGroundChecker>();
    }

    // 점프 이벤트를 받으면 Animator에 JumpTrigger를 전달합니다.
    private void OnEnable()
    {
        if (_playerJump != null)
        {
            _playerJump.OnJumped += PlayJumpAnimation;
        }
    }

    // 이벤트 연결은 컴포넌트 생명주기에 맞춰 정리합니다.
    private void OnDisable()
    {
        if (_playerJump != null)
        {
            _playerJump.OnJumped -= PlayJumpAnimation;
        }
    }

    // 매 프레임 이동 속도, 지면 여부, 수직 속도를 Animator에 부드럽게 전달합니다.
    private void Update()
    {
        UpdateAnimatorParameters();
    }

    // 이동속도는 0~1 값으로 정규화해 Idle, Walk, Run 블렌딩에 사용합니다.
    private void UpdateAnimatorParameters()
    {
        if (Animator_Player == null || Animator_Player.runtimeAnimatorController == null)
        {
            return;
        }

        float runSpeed = _playerMovement != null ? _playerMovement.RunSpeed : 1f;
        float moveSpeed01 = _playerMovement != null ? Mathf.InverseLerp(0f, runSpeed, _playerMovement.CurrentMoveSpeed) : 0f;
        bool isGrounded = _groundChecker != null && _groundChecker.IsGrounded;
        float verticalSpeed = Rigidbody_Player != null ? Rigidbody_Player.linearVelocity.y : 0f;

        Animator_Player.SetFloat(MoveSpeedHash, moveSpeed01, 0.08f, Time.deltaTime);
        Animator_Player.SetBool(IsGroundedHash, isGrounded);
        Animator_Player.SetFloat(VerticalSpeedHash, verticalSpeed);
    }

    // 점프 컴포넌트가 실제 힘을 준 순간, 애니메이션도 같은 컷으로 전환합니다.
    public void PlayJumpAnimation()
    {
        if (Animator_Player == null || Animator_Player.runtimeAnimatorController == null)
        {
            return;
        }

        Animator_Player.SetTrigger(JumpTriggerHash);
    }
}
