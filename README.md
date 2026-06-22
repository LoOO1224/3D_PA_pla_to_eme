# 3D Promotion

Unity 6000.3.13f1 기반 플래티넘 승급 과제 프로젝트입니다.

## 실행

- Scene: `Assets/Promotion/Scenes/PromotionMain.unity`
- Build: `Builds/Windows/3D_Promotion.exe`
- 이동: `W/A/S/D`
- 시점 회전: Mouse
- 점프: `Space`
- 달리기: `Left Shift`
- 카메라 Zoom: Mouse Wheel
- 커서 잠금 전환: `Esc`

## 구현 범위

- Rigidbody 기반 3D 플레이어 이동
- Inspector 변수 기반 이동속도, 점프 세기 관리
- Ground Check 기반 지상/공중 판정
- 단일 점프와 이중 점프
- Idle/Walk/Run/Jump/Fall Animator 전환 및 Idle-Walk-Run 블렌딩
- 3인칭 카메라 추적, 마우스 시점 회전, Zoom
- 스태미너 기반 달리기/점프 제한
- 이동 상태 기반 발자국 사운드
- 작은 캐릭터/적/배경 에셋 선별 적용
