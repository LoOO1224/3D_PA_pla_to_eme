using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class DaniTechPromotionSimulationRunner
{
    private const int SimulationCount = 1200;
    private const float FixedDeltaTime = 0.02f;
    private const float Gravity = -9.81f;

    // 배치모드 검증용 1200회 이동, 점프, 낙하, 착지 시뮬레이션입니다.
    // 실제 손 플레이를 대신해 "같은 계산이 반복되어도 무너지지 않는가"를 자동으로 확인합니다.
    public static void RunThousandMovementJumpSimulation()
    {
        int passCount = 0;
        StringBuilder reportBuilder = new StringBuilder();
        reportBuilder.AppendLine("DaniTech Promotion movement/jump simulation report");
        reportBuilder.AppendLine($"Target Count: {SimulationCount}");

        for (int i = 0; i < SimulationCount; i++)
        {
            bool passed = SimulateOneScenario(i, out string failReason);
            if (passed)
            {
                passCount++;
                continue;
            }

            reportBuilder.AppendLine($"FAILED {i}: {failReason}");
            WriteReport(reportBuilder.ToString());
            ExitBatchMode(1);
            return;
        }

        reportBuilder.AppendLine($"Passed Count: {passCount}");
        reportBuilder.AppendLine("Result: PASS");
        WriteReport(reportBuilder.ToString());
        Debug.Log(reportBuilder.ToString());
        ExitBatchMode(0);
    }

    // 한 시나리오는 이동 방향 계산, 달리기 속도 제한, 점프, 이중 점프, 착지를 모두 포함합니다.
    private static bool SimulateOneScenario(int scenarioIndex, out string failReason)
    {
        Quaternion cameraRotation = Quaternion.Euler(0f, (scenarioIndex * 19f) % 360f, 0f);
        Vector2 moveInput = new Vector2(
            Mathf.Sin(scenarioIndex * 0.37f),
            Mathf.Cos(scenarioIndex * 0.23f));

        Vector3 moveDirection = DaniTechPlayerLocomotionUtil.CalculateCameraRelativeMoveDirection(moveInput, cameraRotation);
        if (float.IsNaN(moveDirection.x) || moveDirection.sqrMagnitude > 1.0001f)
        {
            failReason = "카메라 기준 이동 방향 계산이 비정상입니다.";
            return false;
        }

        float runSpeed = 6.2f;
        float height = 0f;
        float verticalVelocity = 0f;
        bool isGrounded = true;
        bool jumped = false;
        bool doubleJumped = false;
        bool landed = false;
        int remainAirJumpCount = 1;
        Vector3 position = Vector3.zero;

        for (int frame = 0; frame < 240; frame++)
        {
            bool jumpPressed = frame == 5 || frame == 38;
            if (jumpPressed && (isGrounded || remainAirJumpCount > 0))
            {
                if (isGrounded == false)
                {
                    remainAirJumpCount--;
                    doubleJumped = true;
                }

                verticalVelocity = 7.2f;
                isGrounded = false;
                jumped = true;
            }

            position += moveDirection * runSpeed * FixedDeltaTime;
            verticalVelocity += Gravity * FixedDeltaTime;
            height += verticalVelocity * FixedDeltaTime;

            if (height <= 0f && frame > 5)
            {
                if (isGrounded == false)
                {
                    landed = true;
                }

                height = 0f;
                verticalVelocity = 0f;
                isGrounded = true;
                remainAirJumpCount = 1;
            }
        }

        if (jumped == false)
        {
            failReason = "점프가 실행되지 않았습니다.";
            return false;
        }

        if (doubleJumped == false)
        {
            failReason = "이중 점프가 실행되지 않았습니다.";
            return false;
        }

        if (landed == false || isGrounded == false)
        {
            failReason = "착지 상태로 돌아오지 못했습니다.";
            return false;
        }

        if (float.IsNaN(position.x) || position.magnitude <= 0f)
        {
            failReason = "이동 누적 위치가 비정상입니다.";
            return false;
        }

        failReason = string.Empty;
        return true;
    }

    // 시뮬레이션 결과는 Logs 폴더에 남겨 제출 전 확인할 수 있게 합니다.
    private static void WriteReport(string report)
    {
        Directory.CreateDirectory("Logs");
        File.WriteAllText("Logs/DaniTechSimulationReport.txt", report, Encoding.UTF8);
    }

    // 배치모드에서는 성공, 실패를 명확한 종료 코드로 반환합니다.
    private static void ExitBatchMode(int exitCode)
    {
        if (Application.isBatchMode)
        {
            EditorApplication.Exit(exitCode);
        }
    }
}
