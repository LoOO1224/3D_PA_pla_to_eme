using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class DaniTechPromotionDemoFrameRecorder
{
    private const string ScenePath = "Assets/Promotion/Scenes/PromotionMain.unity";
    private const string FrameFolderPath = "Recordings/DemoFrames";
    private const int Width = 320;
    private const int Height = 180;
    private const int FrameRate = 12;
    private const int FrameCount = 240;

    // 실제 제출 영상의 원본 프레임을 만드는 에디터 전용 녹화기입니다.
    // 게임 로직 검증은 시뮬레이션이 담당했고, 이 녹화기는 이동, 점프, 낙하, 착지, 재이동 흐름을 눈으로 보여줍니다.
    public static void RecordDemoFrames()
    {
        EditorSceneManager.OpenScene(ScenePath);
        Directory.CreateDirectory(FrameFolderPath);
        ClearOldFrames();

        GameObject player = GameObject.Find("Player_Mage");
        Camera camera = Camera.main;

        if (player == null || camera == null)
        {
            Debug.LogError("시연 프레임 녹화에 필요한 Player 또는 Main Camera를 찾을 수 없습니다.");
            ExitBatchMode(1);
            return;
        }

        Animator animator = player.GetComponent<Animator>();
        RenderTexture renderTexture = new RenderTexture(Width, Height, 24);
        Texture2D frameTexture = new Texture2D(Width, Height, TextureFormat.RGB24, false);
        camera.targetTexture = renderTexture;

        for (int frame = 0; frame < FrameCount; frame++)
        {
            float time = frame / (float)FrameRate;
            SetDemoPlayerPose(player, animator, time);
            SetDemoCamera(camera, player.transform.position);
            RenderFrame(camera, renderTexture, frameTexture, frame);
        }

        camera.targetTexture = null;
        Object.DestroyImmediate(renderTexture);
        Object.DestroyImmediate(frameTexture);

        File.WriteAllText(Path.Combine(FrameFolderPath, "frame_info.txt"), $"fps={FrameRate}\nframes={FrameCount}\n");
        Debug.Log($"시연 프레임 생성 완료: {FrameFolderPath}, {FrameCount} frames");
        ExitBatchMode(0);
    }

    // 이전 녹화 프레임이 섞이지 않도록 폴더를 정리합니다.
    private static void ClearOldFrames()
    {
        string[] files = Directory.GetFiles(FrameFolderPath, "*.png");
        foreach (string file in files)
        {
            File.Delete(file);
        }
    }

    // 플레이어를 정해진 동선에 배치합니다.
    // 영화로 치면 배우에게 "이동 -> 점프 -> 낙하 -> 착지 -> 재이동" 리허설 동선을 직접 지정하는 장면입니다.
    private static void SetDemoPlayerPose(GameObject player, Animator animator, float time)
    {
        Vector3 position = CalculateDemoPosition(time);
        Vector3 nextPosition = CalculateDemoPosition(time + (1f / FrameRate));
        Vector3 moveDirection = nextPosition - position;
        moveDirection.y = 0f;

        player.transform.position = position;
        if (moveDirection.sqrMagnitude > 0.0001f)
        {
            player.transform.rotation = Quaternion.LookRotation(moveDirection.normalized, Vector3.up);
        }

        if (animator == null || animator.runtimeAnimatorController == null)
        {
            return;
        }

        bool isGrounded = position.y <= 0.06f;
        bool isRunning = time > 12f;
        float moveSpeed = moveDirection.magnitude * FrameRate;
        float moveSpeed01 = Mathf.InverseLerp(0f, isRunning ? 6.2f : 3.2f, moveSpeed);
        float verticalSpeed = CalculateDemoPosition(time + 0.05f).y - position.y;

        animator.SetFloat("MoveSpeed", moveSpeed01);
        animator.SetBool("IsGrounded", isGrounded);
        animator.SetFloat("VerticalSpeed", verticalSpeed);
        animator.Update(1f / FrameRate);
    }

    // 시간에 따른 시연 위치를 계산합니다.
    private static Vector3 CalculateDemoPosition(float time)
    {
        float z = 0f;
        float x = Mathf.Sin(time * 0.45f) * 1.3f;
        float y = 0.05f;

        if (time < 5f)
        {
            z = time * 0.9f;
        }
        else if (time < 9f)
        {
            float jumpTime = time - 5f;
            z = 4.5f + jumpTime * 0.85f;
            y = Mathf.Max(0.05f, 3.2f * jumpTime - 4.9f * jumpTime * jumpTime);
        }
        else if (time < 12f)
        {
            z = 7.9f;
        }
        else
        {
            z = 7.9f + (time - 12f) * 1.25f;
        }

        return new Vector3(x, y, z);
    }

    // 카메라는 플레이어 뒤쪽에서 살짝 위를 바라봅니다.
    private static void SetDemoCamera(Camera camera, Vector3 playerPosition)
    {
        Vector3 cameraPosition = playerPosition + new Vector3(0f, 3.1f, -6.2f);
        Vector3 lookTarget = playerPosition + Vector3.up * 1.3f;
        camera.transform.position = cameraPosition;
        camera.transform.rotation = Quaternion.LookRotation(lookTarget - cameraPosition, Vector3.up);
    }

    // 현재 카메라 프레임을 PNG로 저장합니다.
    private static void RenderFrame(Camera camera, RenderTexture renderTexture, Texture2D frameTexture, int frame)
    {
        RenderTexture.active = renderTexture;
        camera.Render();
        frameTexture.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
        frameTexture.Apply();

        byte[] pngBytes = frameTexture.EncodeToPNG();
        string framePath = Path.Combine(FrameFolderPath, $"frame_{frame:0000}.png");
        File.WriteAllBytes(framePath, pngBytes);
        RenderTexture.active = null;
    }

    // 배치모드 실행 결과를 종료 코드로 반환합니다.
    private static void ExitBatchMode(int exitCode)
    {
        if (Application.isBatchMode)
        {
            EditorApplication.Exit(exitCode);
        }
    }
}
