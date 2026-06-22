using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class DaniTechPlayerJump : MonoBehaviour
{
    [SerializeField] private Rigidbody Rigidbody_Player;
    [SerializeField] private float _jumpPower = 7.2f;
    [SerializeField] private bool _canUseDoubleJump = true;
    [SerializeField] private int _airJumpCountMax = 1;

    private IDaniTechPlayerInputSource _inputSource;
    private DaniTechPlayerGroundChecker _groundChecker;
    private DaniTechPlayerStamina _playerStamina;
    private int _remainAirJumpCount;

    public event Action OnJumped;
    public float JumpPower => _jumpPower;
    public bool CanUseDoubleJump => _canUseDoubleJump;
    public int AirJumpCountMax => _airJumpCountMax;
    public int RemainAirJumpCount => _remainAirJumpCount;

    // Jump 배우는 입력, 지면 판정, 스태미너 배우와 협업합니다.
    // 각 배우에게 필요한 질문만 하고, 실제 점프 힘 적용은 자기 책임으로 수행합니다.
    private void Awake()
    {
        if (Rigidbody_Player == null)
        {
            Rigidbody_Player = GetComponent<Rigidbody>();
        }

        _inputSource = GetInputSource();
        _groundChecker = GetComponent<DaniTechPlayerGroundChecker>();
        _playerStamina = GetComponent<DaniTechPlayerStamina>();
        ResetAirJumpCount();
    }

    // 지면에 다시 닿는 순간 공중 점프 횟수를 복구합니다.
    private void OnEnable()
    {
        if (_groundChecker != null)
        {
            _groundChecker.OnGroundStateChanged += HandleGroundStateChanged;
        }
    }

    // 이벤트 구독은 배우가 무대에서 내려갈 때 정리합니다.
    private void OnDisable()
    {
        if (_groundChecker != null)
        {
            _groundChecker.OnGroundStateChanged -= HandleGroundStateChanged;
        }
    }

    // 점프 입력은 한 프레임 이벤트이므로 Update에서 확인합니다.
    private void Update()
    {
        if (_inputSource == null || _inputSource.JumpPressed == false)
        {
            return;
        }

        TryJump();
    }

    // 입력 컴포넌트는 구체 클래스 대신 인터페이스로 찾습니다.
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

        Debug.LogWarning("점프 입력 컴포넌트를 찾을 수 없습니다.");
        return null;
    }

    // 지면 상태가 true로 바뀌면 착지 컷입니다.
    // 이때 다음 점프를 위해 공중 점프 가능 횟수를 다시 채웁니다.
    private void HandleGroundStateChanged(bool isGrounded)
    {
        if (isGrounded)
        {
            ResetAirJumpCount();
        }
    }

    // 점프 요청의 입구입니다.
    // 지면 여부, 이중 점프 가능 여부, 스태미너 비용을 모두 통과해야 실제 힘을 줍니다.
    public bool TryJump()
    {
        if (CheckJumpAvailable() == false)
        {
            return false;
        }

        if (_playerStamina != null && _playerStamina.TryUseStamina(_playerStamina.JumpSpendAmount) == false)
        {
            return false;
        }

        UseAirJumpCountIfNeeded();
        Jump();
        return true;
    }

    // 현재 장면에서 점프가 가능한지 판단합니다.
    private bool CheckJumpAvailable()
    {
        bool isGrounded = _groundChecker != null && _groundChecker.IsGrounded;
        if (isGrounded)
        {
            return true;
        }

        return _canUseDoubleJump && _remainAirJumpCount > 0;
    }

    // 공중에서 점프했다면 남은 공중 점프 횟수를 하나 사용합니다.
    private void UseAirJumpCountIfNeeded()
    {
        bool isGrounded = _groundChecker != null && _groundChecker.IsGrounded;
        if (isGrounded)
        {
            return;
        }

        _remainAirJumpCount--;
    }

    // Rigidbody에 위쪽 속도 변화를 주어 물리 기반 점프를 만듭니다.
    // 기존 낙하 속도를 0으로 정리한 뒤 점프 힘을 주면 입력 순간 반응이 선명합니다.
    private void Jump()
    {
        Vector3 currentVelocity = Rigidbody_Player.linearVelocity;
        Rigidbody_Player.linearVelocity = new Vector3(currentVelocity.x, 0f, currentVelocity.z);
        Rigidbody_Player.AddForce(Vector3.up * _jumpPower, ForceMode.VelocityChange);
        OnJumped?.Invoke();
    }

    // 착지 컷에서 다시 사용할 공중 점프 횟수를 초기화합니다.
    private void ResetAirJumpCount()
    {
        _remainAirJumpCount = Mathf.Max(0, _airJumpCountMax);
    }
}
