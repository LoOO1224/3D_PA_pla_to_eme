using UnityEngine;

public static class DaniTechPlayerLocomotionUtil
{
    // 카메라 기준 이동 계산은 여러 배우가 재사용할 수 있는 순수 계산입니다.
    // 영화 비유로는 카메라가 바라보는 방향을 기준으로 배우의 동선을 다시 그리는 조감도입니다.
    public static Vector3 CalculateCameraRelativeMoveDirection(Vector2 moveInput, Transform cameraTransform)
    {
        if (cameraTransform == null)
        {
            return CalculateWorldMoveDirection(moveInput);
        }

        return CalculateCameraRelativeMoveDirection(moveInput, cameraTransform.rotation);
    }

    // 테스트나 시뮬레이션에서는 실제 카메라 Transform이 없어도 회전값만으로 같은 계산을 검증합니다.
    public static Vector3 CalculateCameraRelativeMoveDirection(Vector2 moveInput, Quaternion cameraRotation)
    {
        Vector2 clampedInput = Vector2.ClampMagnitude(moveInput, 1f);
        Vector3 forward = cameraRotation * Vector3.forward;
        Vector3 right = cameraRotation * Vector3.right;

        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();

        Vector3 moveDirection = (forward * clampedInput.y) + (right * clampedInput.x);
        return moveDirection.sqrMagnitude > 1f ? moveDirection.normalized : moveDirection;
    }

    // 카메라가 아직 준비되지 않은 첫 프레임에도 월드 기준 이동은 정상 동작하게 해 둡니다.
    public static Vector3 CalculateWorldMoveDirection(Vector2 moveInput)
    {
        Vector2 clampedInput = Vector2.ClampMagnitude(moveInput, 1f);
        return new Vector3(clampedInput.x, 0f, clampedInput.y);
    }

    // 작은 입력 노이즈 때문에 배우가 미세하게 떨리지 않도록 이동 판정을 한곳에서 통일합니다.
    public static bool CheckMoveInput(Vector2 moveInput)
    {
        return moveInput.sqrMagnitude > 0.01f;
    }
}
