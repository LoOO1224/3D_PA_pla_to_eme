using UnityEngine;

public class DaniTechGameManager : MonoBehaviour
{
    public static DaniTechGameManager Inst { get; private set; }

    private DaniTechPlayerModel _playerModel = new DaniTechPlayerModel();

    public DaniTechPlayerModel PlayerModel => _playerModel;

    // 영화 제작으로 보면 Awake는 촬영장 문을 열고 총괄 제작자를 한 명만 세우는 순간입니다.
    // 다른 배우들이 "총감독이 누구인지" 물어볼 때 Inst로 같은 사람을 찾아가게 해 둡니다.
    private void Awake()
    {
        if (Inst != null && Inst != this)
        {
            Destroy(gameObject);
            return;
        }

        Inst = this;
    }

    // 플레이어의 저장 가능한 런타임 정보를 처음 세팅합니다.
    // Static Data가 기획서라면, Model은 실제 촬영 중 계속 바뀌는 촬영 기록지입니다.
    public void InitPlayerModel(float maxStamina)
    {
        _playerModel.MaxStamina = maxStamina;
        _playerModel.CurrentStamina = maxStamina;
    }

    // 스태미너 컴포넌트가 자기 역할을 수행한 뒤, 저장 후보 데이터만 매니저에게 보고합니다.
    public void SetCurrentStamina(float currentStamina)
    {
        _playerModel.CurrentStamina = currentStamina;
    }

    // 지면 판정 컴포넌트가 "배우가 무대 위에 있는지"만 알려주면, 매니저는 결과 기록만 보관합니다.
    public void SetGroundedState(bool isGrounded)
    {
        _playerModel.IsGrounded = isGrounded;
    }

    // 위치는 실제 저장 기능을 붙일 때 마지막 세이브 위치로 사용할 수 있도록 모델에만 기록합니다.
    public void SetLastPlayerPosition(Vector3 playerPosition)
    {
        _playerModel.LastPlayerPosition = playerPosition;
    }
}
