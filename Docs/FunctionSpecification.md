# 기능명세서

## 프로젝트 개요

이 프로젝트는 Unity에서 기본적인 3D 플레이어 조작 흐름을 확인하기 위한 플래티넘 승급 과제입니다. 플레이어는 작은 마법사 캐릭터 에셋을 사용했고, 장면에는 적 에셋인 Carnivorous Plant와 작은 Kitbash 배경 이미지를 배치했습니다.

## 구현 기능

### 입력 처리

`DaniTechPlayerInput`이 키보드와 마우스 입력을 읽습니다. 이 컴포넌트는 입력만 담당하고, 실제 이동이나 점프는 하지 않습니다. `W/A/S/D`는 이동 입력, 마우스는 시점 회전, `Space`는 점프, `Left Shift`는 달리기, 마우스 휠은 카메라 Zoom으로 사용됩니다.

### 이동

`DaniTechPlayerMovement`가 Rigidbody의 수평 속도를 갱신해 플레이어를 이동시킵니다. 이동 방향은 카메라가 바라보는 방향을 기준으로 계산했습니다. 이동속도는 `_walkSpeed`, `_runSpeed` 변수로 관리되며, 하드코딩된 숫자로 직접 이동하지 않습니다.

### 방향 전환

플레이어가 이동 중일 때 `Quaternion.Slerp`를 사용해 이동 방향으로 부드럽게 회전합니다. 그래서 `W/A/S/D` 입력 방향이 바뀌어도 캐릭터가 순간적으로 꺾이지 않고 자연스럽게 몸을 돌립니다.

### 중력과 점프

중력은 Unity Rigidbody 물리 계산을 사용합니다. `DaniTechPlayerJump`는 점프 입력을 받으면 Rigidbody에 위쪽 힘을 `ForceMode.VelocityChange`로 적용합니다. 점프 세기인 `_jumpPower`는 Inspector에 노출되어 있어 값을 바꾸면 점프 높이가 달라집니다.

### Ground Check

`DaniTechPlayerGroundChecker`가 발밑 `GroundCheck` 위치에 작은 구를 두고 `Physics.CheckSphere`로 지면 여부를 판정합니다. 점프 가능 여부와 착지 판정은 이 결과를 기준으로 처리합니다.

### 애니메이션

`DaniTechPlayerAnimationView`가 이동, 지면, 수직 속도 상태를 Animator 파라미터로 전달합니다. Animator에는 `Idle`, `Walk`, `Run`, `Jump`, `Fall` 클립이 있고, `MoveSpeed` 값으로 `Idle -> Walk -> Run` 블렌딩이 이루어집니다.

### 3인칭 카메라

`DaniTechThirdPersonCameraFollow`가 플레이어를 따라가며, 마우스 입력으로 카메라 회전 값을 갱신합니다. 카메라는 플레이어 뒤쪽과 위쪽 Offset을 유지하고, 마우스 휠 입력으로 거리 Zoom을 조절합니다.

## 추가 구현 요소

### 이중 점프

`DaniTechPlayerJump`에서 지상 점프 후 공중 점프 가능 횟수를 `_airJumpCountMax`로 관리합니다. 착지하면 공중 점프 횟수를 다시 회복합니다.

### 애니메이션 블렌딩

`MoveSpeed` 파라미터를 0에서 1 사이 값으로 정규화해 Blend Tree에 전달합니다. 이 값에 따라 Idle, Walk, Run 애니메이션이 자연스럽게 섞입니다.

### 카메라 Zoom

마우스 휠 입력을 받아 카메라와 플레이어 사이 거리를 `_minDistance`, `_maxDistance` 범위 안에서 조절합니다.

### 스태미너 시스템

`DaniTechPlayerStamina`가 현재 스태미너를 관리합니다. 달리기는 초당 스태미너를 사용하고, 점프는 한 번에 일정량을 사용합니다. 스태미너가 부족하면 달리기나 점프가 제한됩니다.

### 발자국 사운드

`DaniTechFootstepSound`가 플레이어가 이동 중이고 지면에 닿아 있을 때만 발자국 소리를 재생합니다. Walk와 Run 상태에 따라 재생 간격이 다릅니다.

## 코드 구조

Controller는 `DaniTechPromotionSceneController` 하나로 제한했고, 이 컴포넌트는 커서 잠금 같은 씬 시작 큐만 담당합니다. 플레이어의 실제 기능은 `Input`, `Movement`, `Jump`, `GroundChecker`, `AnimationView`, `Stamina`, `FootstepSound` 컴포넌트로 분리했습니다.

Unity 참조 객체는 `[SerializeField] private`으로 두고, 외부에서 필요한 값은 프로퍼티로 열었습니다. 런타임에 UI를 `new GameObject()`로 만들지 않았고, 플레이어는 생성된 Prefab과 씬 배치 오브젝트를 사용합니다.

## 데이터 관리 구분

이 과제에서 이동속도, 점프 세기, 카메라 거리처럼 기획자가 고정값으로 조정하는 값은 Static Data 성격입니다. 현재는 작은 과제이므로 Inspector 변수로 관리했습니다.

현재 스태미너, 마지막 플레이어 위치, 지상 여부처럼 플레이 중 계속 변하는 값은 Instance Data 성격입니다. 이 값은 `DaniTechPlayerModel`에 담고 `DaniTechGameManager`가 소유하도록 구성했습니다.
