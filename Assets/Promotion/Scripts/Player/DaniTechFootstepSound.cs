using UnityEngine;

public class DaniTechFootstepSound : MonoBehaviour
{
    [SerializeField] private AudioSource AudioSource_Footstep;
    [SerializeField] private AudioClip AudioClip_Footstep;
    [SerializeField] private float _walkStepInterval = 0.48f;
    [SerializeField] private float _runStepInterval = 0.32f;

    private DaniTechPlayerMovement _playerMovement;
    private DaniTechPlayerGroundChecker _groundChecker;
    private float _stepTimer;
    private float _lastFootstepTime;
    private bool _isFootstepActive;

    public bool IsFootstepActive => _isFootstepActive;
    public bool IsFootstepRecentlyPlayed => Time.time <= _lastFootstepTime + 0.2f;

    // 발자국 사운드는 움직임 배우와 지면 판정 배우의 결과만 보고 자기 소리를 냅니다.
    // 사운드가 없으면 아주 짧은 절차적 효과음을 만들어 초보 프로젝트에서도 바로 들리게 합니다.
    private void Awake()
    {
        if (AudioSource_Footstep == null)
        {
            AudioSource_Footstep = GetComponent<AudioSource>();
        }

        _playerMovement = GetComponent<DaniTechPlayerMovement>();
        _groundChecker = GetComponent<DaniTechPlayerGroundChecker>();

        if (AudioClip_Footstep == null)
        {
            AudioClip_Footstep = CreateFootstepClip();
        }
    }

    // 이동 중이고 땅에 닿아 있을 때만 일정 간격으로 소리를 재생합니다.
    private void Update()
    {
        UpdateFootstep();
    }

    // 발자국은 Walk와 Run의 리듬이 달라야 움직임이 더 자연스럽게 느껴집니다.
    private void UpdateFootstep()
    {
        bool canPlayFootstep = _playerMovement != null
                               && _groundChecker != null
                               && _playerMovement.IsMoving
                               && _groundChecker.IsGrounded;
        _isFootstepActive = canPlayFootstep;

        if (canPlayFootstep == false)
        {
            _stepTimer = 0f;
            return;
        }

        _stepTimer -= Time.deltaTime;
        if (_stepTimer > 0f)
        {
            return;
        }

        PlayFootstep();
        _stepTimer = _playerMovement.IsRunning ? _runStepInterval : _walkStepInterval;
    }

    // AudioSource는 사운드 배우의 스피커입니다.
    private void PlayFootstep()
    {
        if (AudioSource_Footstep == null || AudioClip_Footstep == null)
        {
            return;
        }

        AudioSource_Footstep.PlayOneShot(AudioClip_Footstep, 0.45f);
        _lastFootstepTime = Time.time;
    }

    // 외부 사운드 에셋 없이도 발자국 구현 체크가 가능하도록 짧은 저음 클릭을 만듭니다.
    private AudioClip CreateFootstepClip()
    {
        const int frequency = 44100;
        const float duration = 0.08f;
        int sampleCount = Mathf.CeilToInt(frequency * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float time01 = i / (float)sampleCount;
            float envelope = Mathf.Exp(-time01 * 18f);
            samples[i] = Mathf.Sin(2f * Mathf.PI * 95f * i / frequency) * envelope * 0.35f;
        }

        AudioClip clip = AudioClip.Create("DaniTech_ProceduralFootstep", sampleCount, 1, frequency, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
