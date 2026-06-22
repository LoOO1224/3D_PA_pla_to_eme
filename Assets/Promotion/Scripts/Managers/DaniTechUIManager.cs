using UnityEngine;

public class DaniTechUIManager : MonoBehaviour
{
    public static DaniTechUIManager Inst { get; private set; }

    [SerializeField] private DaniTechGameHUDUI Prefab_GameHUDUI;
    [SerializeField] private Transform Transform_UIRoot;
    [SerializeField] private GameObject GameObject_Player;
    [SerializeField] private DaniTechThirdPersonCameraFollow CameraFollow_Main;

    private DaniTechGameHUDUI _gameHUDUI;

    // UIManager는 극장의 안내 데스크입니다. UI 프리팹을 어디에 열지 관리하고, 실제 게임 배우의 역할은 건드리지 않습니다.
    private void Awake()
    {
        if (Inst != null && Inst != this)
        {
            Destroy(gameObject);
            return;
        }

        Inst = this;
    }

    // 런타임 UI는 new GameObject가 아니라 미리 만든 HUD 프리팹을 Instantiate해서 엽니다.
    private void Start()
    {
        OpenGameHUD();
    }

    // 이미 열려 있으면 중복 생성하지 않고 기존 HUD를 반환합니다.
    public DaniTechGameHUDUI OpenGameHUD()
    {
        if (_gameHUDUI != null)
        {
            return _gameHUDUI;
        }

        return CreateGameHUD();
    }

    // HUD 프리팹을 UI Root 아래에 생성하고, 체크할 플레이어와 카메라를 연결합니다.
    private DaniTechGameHUDUI CreateGameHUD()
    {
        if (Prefab_GameHUDUI == null || Transform_UIRoot == null)
        {
            Debug.LogWarning("HUD UI Prefab 또는 UI Root가 연결되지 않았습니다.");
            return null;
        }

        _gameHUDUI = Instantiate(Prefab_GameHUDUI, Transform_UIRoot);
        _gameHUDUI.InitHud(GameObject_Player, CameraFollow_Main);
        return _gameHUDUI;
    }
}
