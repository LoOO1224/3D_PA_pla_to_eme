using UnityEngine;
using UnityEngine.UI;

public class DaniTechGameHUDUI : MonoBehaviour
{
    [SerializeField] private Text Text_Title;
    [SerializeField] private Text Text_MoveStatus;
    [SerializeField] private Text Text_JumpStatus;
    [SerializeField] private Text Text_AnimationStatus;
    [SerializeField] private Text Text_CameraStatus;
    [SerializeField] private Text Text_StaminaStatus;
    [SerializeField] private Text Text_FootstepStatus;
    [SerializeField] private Image Image_StaminaFill;

    private GameObject _playerObj;
    private DaniTechPlayerMovement _playerMovement;
    private DaniTechPlayerJump _playerJump;
    private DaniTechPlayerGroundChecker _groundChecker;
    private DaniTechPlayerStamina _playerStamina;
    private DaniTechFootstepSound _footstepSound;
    private DaniTechThirdPersonCameraFollow _cameraFollow;

    // HUD는 촬영장 모니터입니다. 배우를 조종하지 않고, 각 배우가 가진 현재 상태만 읽어서 보여줍니다.
    public void InitHud(GameObject playerObj, DaniTechThirdPersonCameraFollow cameraFollow)
    {
        _playerObj = playerObj;
        _cameraFollow = cameraFollow;
        CachePlayerComponents();
        RefreshHud();
    }

    // 매 프레임 체크리스트 상태를 갱신합니다.
    private void Update()
    {
        RefreshHud();
    }

    // 플레이어 배우가 가진 역할표들을 가져옵니다.
    private void CachePlayerComponents()
    {
        if (_playerObj == null)
        {
            return;
        }

        _playerMovement = _playerObj.GetComponent<DaniTechPlayerMovement>();
        _playerJump = _playerObj.GetComponent<DaniTechPlayerJump>();
        _groundChecker = _playerObj.GetComponent<DaniTechPlayerGroundChecker>();
        _playerStamina = _playerObj.GetComponent<DaniTechPlayerStamina>();
        _footstepSound = _playerObj.GetComponent<DaniTechFootstepSound>();
    }

    // HUD 한 장 안에서 필수/추가 구현 상태를 바로 확인할 수 있게 정리합니다.
    private void RefreshHud()
    {
        if (_playerObj == null)
        {
            return;
        }

        if (_playerMovement == null)
        {
            CachePlayerComponents();
        }

        SetText(Text_Title, "Platinum Controller Monitor");
        RefreshMoveStatus();
        RefreshJumpStatus();
        RefreshAnimationStatus();
        RefreshCameraStatus();
        RefreshStaminaStatus();
        RefreshFootstepStatus();
    }

    // 이동속도 변수화와 현재 이동 상태를 표시합니다.
    private void RefreshMoveStatus()
    {
        if (_playerMovement == null)
        {
            SetText(Text_MoveStatus, "Move  | Waiting for PlayerMovement");
            return;
        }

        string moveMode = _playerMovement.IsMoving ? (_playerMovement.IsRunning ? "Run" : "Walk") : "Idle";
        SetText(Text_MoveStatus, $"Move  | {moveMode}  Speed {_playerMovement.CurrentMoveSpeed:0.00} / Run {_playerMovement.RunSpeed:0.00}");
    }

    // ground check, 단일 점프, 이중 점프 상태를 표시합니다.
    private void RefreshJumpStatus()
    {
        if (_playerJump == null || _groundChecker == null)
        {
            SetText(Text_JumpStatus, "Jump  | Waiting for Jump/GroundChecker");
            return;
        }

        string groundState = _groundChecker.IsGrounded ? "Grounded" : "Air";
        string doubleJumpState = _playerJump.CanUseDoubleJump ? $"Double ON ({_playerJump.RemainAirJumpCount}/{_playerJump.AirJumpCountMax})" : "Double OFF";
        SetText(Text_JumpStatus, $"Jump  | {groundState}  Power {_playerJump.JumpPower:0.0}  {doubleJumpState}");
    }

    // Animator BlendTree에 들어가는 이동 비율을 표시합니다.
    private void RefreshAnimationStatus()
    {
        if (_playerMovement == null)
        {
            SetText(Text_AnimationStatus, "Anim  | Waiting for Movement");
            return;
        }

        float moveSpeed01 = Mathf.InverseLerp(0f, _playerMovement.RunSpeed, _playerMovement.CurrentMoveSpeed);
        SetText(Text_AnimationStatus, $"Anim  | Idle-Walk-Run Blend  MoveSpeed {moveSpeed01:0.00}");
    }

    // 카메라 Zoom 거리와 초점 앵커 상태를 표시합니다.
    private void RefreshCameraStatus()
    {
        if (_cameraFollow == null)
        {
            SetText(Text_CameraStatus, "Camera| Waiting for CameraFollow");
            return;
        }

        string targetName = _cameraFollow.Target != null ? _cameraFollow.Target.name : "None";
        SetText(Text_CameraStatus, $"Camera| Focus {targetName}  Zoom {_cameraFollow.CurrentDistance:0.00}m  Pitch {_cameraFollow.CurrentPitch:0}");
    }

    // 스태미너 수치와 바를 표시합니다.
    private void RefreshStaminaStatus()
    {
        if (_playerStamina == null)
        {
            SetText(Text_StaminaStatus, "Stamina | Waiting for Stamina");
            SetStaminaFill(0f);
            return;
        }

        float stamina01 = _playerStamina.MaxStamina <= 0f ? 0f : _playerStamina.CurrentStamina / _playerStamina.MaxStamina;
        SetText(Text_StaminaStatus, $"Stamina | {_playerStamina.CurrentStamina:0}/{_playerStamina.MaxStamina:0}  Run/Jump Limited");
        SetStaminaFill(stamina01);
    }

    // 발자국 사운드가 재생 가능한 상태인지 표시합니다.
    private void RefreshFootstepStatus()
    {
        if (_footstepSound == null)
        {
            SetText(Text_FootstepStatus, "Sound | Waiting for FootstepSound");
            return;
        }

        string soundState = _footstepSound.IsFootstepRecentlyPlayed ? "Playing" : (_footstepSound.IsFootstepActive ? "Ready" : "Idle");
        SetText(Text_FootstepStatus, $"Sound | Footstep {soundState}");
    }

    // Text 누락으로 HUD 전체가 멈추지 않도록 작은 방어 코드를 둡니다.
    private void SetText(Text targetText, string message)
    {
        if (targetText == null)
        {
            return;
        }

        targetText.text = message;
    }

    // 스태미너 바는 색상도 함께 바꿔서 멀리서 봐도 상태를 알 수 있게 합니다.
    private void SetStaminaFill(float stamina01)
    {
        if (Image_StaminaFill == null)
        {
            return;
        }

        float clampedValue = Mathf.Clamp01(stamina01);
        Image_StaminaFill.fillAmount = clampedValue;
        Image_StaminaFill.color = Color.Lerp(new Color(0.9f, 0.25f, 0.2f), new Color(0.25f, 0.8f, 0.45f), clampedValue);
    }
}
