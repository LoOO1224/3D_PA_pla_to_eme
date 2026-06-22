using UnityEngine;

public class DaniTechPromotionSceneController : MonoBehaviour
{
    // Controller는 배우를 직접 붙잡지 않고, 촬영 시작 신호처럼 씬 전체 큐만 관리합니다.
    // 실제 이동, 점프, 카메라 추적은 각 배우가 가진 전용 컴포넌트가 처리합니다.
    private void Start()
    {
        LockCursor();
    }

    // Escape 입력은 플레이 테스트 편의를 위한 씬 단위 큐입니다.
    // 캐릭터 이동 로직에는 관여하지 않으므로 Controller의 책임 범위를 넘지 않습니다.
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleCursorLock();
        }
    }

    // 마우스 시점 회전이 자연스럽게 동작하도록 커서를 화면 중앙에 고정합니다.
    private void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // 플레이 중 점검이 필요하면 커서를 풀고, 다시 누르면 촬영 모드로 돌아갑니다.
    private void ToggleCursorLock()
    {
        bool shouldLock = Cursor.lockState != CursorLockMode.Locked;
        Cursor.lockState = shouldLock ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = shouldLock == false;
    }
}
