using UnityEngine;

public class DaniTechPlayerStamina : MonoBehaviour
{
    [SerializeField] private float _maxStamina = 100f;
    [SerializeField] private float _runSpendPerSecond = 18f;
    [SerializeField] private float _jumpSpendAmount = 12f;
    [SerializeField] private float _recoverPerSecond = 22f;
    [SerializeField] private float _recoverDelay = 0.7f;

    private float _currentStamina;
    private float _lastSpendTime;

    public float MaxStamina => _maxStamina;
    public float CurrentStamina => _currentStamina;
    public float JumpSpendAmount => _jumpSpendAmount;

    // Awake는 배우가 자기 소품을 챙기는 장면입니다.
    // 스태미너는 런타임에 변하는 Instance Data이므로 GameManager의 PlayerModel에도 초기값을 보고합니다.
    private void Awake()
    {
        _currentStamina = _maxStamina;

        if (DaniTechGameManager.Inst != null)
        {
            DaniTechGameManager.Inst.InitPlayerModel(_maxStamina);
        }
    }

    // 스태미너 회복은 이 배우가 직접 관리합니다.
    // Controller가 매 프레임 값을 만지지 않아도 각 컴포넌트가 자기 장면을 진행합니다.
    private void Update()
    {
        RecoverStamina();
        ReportStaminaToModel();
    }

    // 달리기는 매 FixedUpdate마다 조금씩 비용을 쓰므로 초당 비용을 델타타임으로 환산합니다.
    public bool SpendRunStamina(float deltaTime)
    {
        float spendAmount = _runSpendPerSecond * deltaTime;
        return TryUseStamina(spendAmount);
    }

    // 점프처럼 한 번에 비용을 쓰는 행동은 같은 입구를 통하게 해 제한 규칙을 한곳에 둡니다.
    public bool TryUseStamina(float spendAmount)
    {
        if (CanUseStamina(spendAmount) == false)
        {
            return false;
        }

        _currentStamina -= spendAmount;
        _lastSpendTime = Time.time;
        return true;
    }

    // 다른 컴포넌트가 "이 행동을 해도 되는가"만 물어볼 수 있게 공개합니다.
    public bool CanUseStamina(float spendAmount)
    {
        return _currentStamina >= spendAmount;
    }

    // 감독이 직접 회복시키지 않고, 스태미너 배우가 쉬는 장면을 스스로 계산합니다.
    private void RecoverStamina()
    {
        if (Time.time < _lastSpendTime + _recoverDelay)
        {
            return;
        }

        _currentStamina = Mathf.Min(_maxStamina, _currentStamina + (_recoverPerSecond * Time.deltaTime));
    }

    // 저장 후보 데이터는 GameManager의 Model에 기록합니다.
    private void ReportStaminaToModel()
    {
        if (DaniTechGameManager.Inst == null)
        {
            return;
        }

        DaniTechGameManager.Inst.SetCurrentStamina(_currentStamina);
    }
}
