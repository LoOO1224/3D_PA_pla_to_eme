using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class DaniTechPromotionProjectBuilder
{
    private const string ScenePath = "Assets/Promotion/Scenes/PromotionMain.unity";
    private const string PlayerPrefabPath = "Assets/Promotion/Prefabs/Player_Mage.prefab";
    private const string HudPrefabPath = "Assets/Promotion/Prefabs/UI/HUD_GameStatus.prefab";
    private const string AnimatorControllerPath = "Assets/Promotion/Animation/PlayerMage.controller";
    private const string BackgroundTexturePath = "Assets/Promotion/Textures/Kitbash_Backdrop.png";

    // 배치모드에서 호출하는 총괄 제작 단계입니다.
    // Controller가 런타임 배우를 붙잡지 않도록, 씬 제작 단계에서만 필요한 참조를 조립합니다.
    public static void BuildProject()
    {
        CreateFolderStructure();
        int groundLayer = EnsureLayer("Ground", 8);
        int playerLayer = EnsureLayer("Player", 9);
        int enemyLayer = EnsureLayer("Enemy", 10);

        AnimatorController animatorController = CreatePlayerAnimatorController();
        DaniTechGameHUDUI hudPrefab = CreateGameHUDPrefab();
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        CreateManagers();
        GameObject environmentRoot = new GameObject("Environment_Root");
        GameObject actorsRoot = new GameObject("Actors_Root");

        CreateEnvironment(environmentRoot.transform, groundLayer);
        GameObject player = CreatePlayer(actorsRoot.transform, playerLayer, groundLayer, animatorController);
        CreateEnemy(actorsRoot.transform, enemyLayer);
        Transform cameraFocusAnchor = player.transform.Find("CameraFocusAnchor");
        Camera camera = CreateCamera(cameraFocusAnchor != null ? cameraFocusAnchor : player.transform, player.transform);
        ConnectPlayerCamera(player, camera);
        CreateUI(player, camera, hudPrefab);
        CreateLighting();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("DaniTechPromotionProjectBuilder: 프로젝트 씬과 빌드 세팅 생성 완료");
    }

    // 프로젝트 산출물이 들어갈 폴더를 먼저 준비합니다.
    private static void CreateFolderStructure()
    {
        string[] folders =
        {
            "Assets/Promotion/Animation",
            "Assets/Promotion/Audio",
            "Assets/Promotion/Materials",
            "Assets/Promotion/Prefabs",
            "Assets/Promotion/Prefabs/UI",
            "Assets/Promotion/Scenes",
            "Assets/Promotion/Textures",
            "Assets/Promotion/Scripts/UI"
        };

        foreach (string folder in folders)
        {
            Directory.CreateDirectory(folder);
        }

        AssetDatabase.Refresh();
    }

    // Unity Layer는 지면 판정과 충돌 대상을 구분하는 촬영장 표식입니다.
    private static int EnsureLayer(string layerName, int preferredIndex)
    {
        int existLayer = LayerMask.NameToLayer(layerName);
        if (existLayer >= 0)
        {
            return existLayer;
        }

        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty layers = tagManager.FindProperty("layers");

        if (string.IsNullOrEmpty(layers.GetArrayElementAtIndex(preferredIndex).stringValue))
        {
            layers.GetArrayElementAtIndex(preferredIndex).stringValue = layerName;
            tagManager.ApplyModifiedProperties();
            return preferredIndex;
        }

        for (int i = 8; i < layers.arraySize; i++)
        {
            if (string.IsNullOrEmpty(layers.GetArrayElementAtIndex(i).stringValue))
            {
                layers.GetArrayElementAtIndex(i).stringValue = layerName;
                tagManager.ApplyModifiedProperties();
                return i;
            }
        }

        Debug.LogWarning($"{layerName} Layer를 추가할 빈 슬롯이 없습니다. Default Layer를 사용합니다.");
        return 0;
    }

    // Idle, Walk, Run, Jump, Fall 5개 애니메이션 클립과 블렌드 트리를 생성합니다.
    // 수업 체크리스트의 "애니메이션 종류와 전환"을 씬 데이터로 명확히 남깁니다.
    private static AnimatorController CreatePlayerAnimatorController()
    {
        AssetDatabase.DeleteAsset(AnimatorControllerPath);

        AnimationClip idleClip = CreateVerticalAnimationClip("Assets/Promotion/Animation/Player_Idle.anim", true,
            new Keyframe(0f, 0f),
            new Keyframe(0.5f, 0.035f),
            new Keyframe(1f, 0f));

        AnimationClip walkClip = CreateVerticalAnimationClip("Assets/Promotion/Animation/Player_Walk.anim", true,
            new Keyframe(0f, 0f),
            new Keyframe(0.18f, 0.055f),
            new Keyframe(0.36f, 0f),
            new Keyframe(0.54f, 0.055f),
            new Keyframe(0.72f, 0f));

        AnimationClip runClip = CreateVerticalAnimationClip("Assets/Promotion/Animation/Player_Run.anim", true,
            new Keyframe(0f, 0f),
            new Keyframe(0.12f, 0.085f),
            new Keyframe(0.24f, 0f),
            new Keyframe(0.36f, 0.085f),
            new Keyframe(0.48f, 0f));

        AnimationClip jumpClip = CreateVerticalAnimationClip("Assets/Promotion/Animation/Player_Jump.anim", false,
            new Keyframe(0f, 0f),
            new Keyframe(0.18f, 0.16f),
            new Keyframe(0.32f, 0.08f));

        AnimationClip fallClip = CreateVerticalAnimationClip("Assets/Promotion/Animation/Player_Fall.anim", true,
            new Keyframe(0f, -0.02f),
            new Keyframe(0.3f, -0.065f),
            new Keyframe(0.6f, -0.02f));

        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(AnimatorControllerPath);
        controller.AddParameter("MoveSpeed", AnimatorControllerParameterType.Float);
        controller.AddParameter("IsGrounded", AnimatorControllerParameterType.Bool);
        controller.AddParameter("VerticalSpeed", AnimatorControllerParameterType.Float);
        controller.AddParameter("JumpTrigger", AnimatorControllerParameterType.Trigger);

        BlendTree blendTree;
        AnimatorState groundState = controller.CreateBlendTreeInController("IdleWalkRunBlend", out blendTree);
        blendTree.blendType = BlendTreeType.Simple1D;
        blendTree.blendParameter = "MoveSpeed";
        blendTree.AddChild(idleClip, 0f);
        blendTree.AddChild(walkClip, 0.5f);
        blendTree.AddChild(runClip, 1f);

        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
        AnimatorState jumpState = stateMachine.AddState("Jump");
        AnimatorState fallState = stateMachine.AddState("Fall");
        jumpState.motion = jumpClip;
        fallState.motion = fallClip;
        stateMachine.defaultState = groundState;

        AddTransition(groundState, jumpState, "JumpTrigger", AnimatorConditionMode.If, 0f);
        AddTransition(groundState, fallState, "IsGrounded", AnimatorConditionMode.IfNot, 0f);
        AddTransition(jumpState, fallState, "VerticalSpeed", AnimatorConditionMode.Less, 0f);
        AddTransition(fallState, groundState, "IsGrounded", AnimatorConditionMode.If, 0f);
        AddTransition(fallState, jumpState, "JumpTrigger", AnimatorConditionMode.If, 0f);

        AssetDatabase.SaveAssets();
        return controller;
    }

    // VisualRoot의 위아래 움직임만 애니메이션으로 주어, 어떤 캐릭터 프리팹에도 안전하게 적용합니다.
    private static AnimationClip CreateVerticalAnimationClip(string assetPath, bool isLoop, params Keyframe[] yKeys)
    {
        AssetDatabase.DeleteAsset(assetPath);

        AnimationClip clip = new AnimationClip();
        clip.frameRate = 30f;
        clip.SetCurve("VisualRoot", typeof(Transform), "localPosition.y", new AnimationCurve(yKeys));

        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = isLoop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        AssetDatabase.CreateAsset(clip, assetPath);
        return clip;
    }

    // Animator 전환은 짧은 컷 편집처럼 duration을 낮게 잡아 끊김이 적게 느껴지도록 합니다.
    private static void AddTransition(AnimatorState fromState, AnimatorState toState, string parameterName, AnimatorConditionMode mode, float threshold)
    {
        AnimatorStateTransition transition = fromState.AddTransition(toState);
        transition.hasExitTime = false;
        transition.duration = 0.08f;
        transition.canTransitionToSelf = false;
        transition.AddCondition(mode, threshold, parameterName);
    }

    // GameManager와 SceneController는 씬 흐름만 담당합니다.
    private static void CreateManagers()
    {
        GameObject gameManagerObj = new GameObject("DaniTechGameManager");
        gameManagerObj.AddComponent<DaniTechGameManager>();

        GameObject sceneControllerObj = new GameObject("DaniTechPromotionSceneController");
        sceneControllerObj.AddComponent<DaniTechPromotionSceneController>();
    }

    // 지면, 점프 발판, 배경 보드를 생성합니다.
    private static void CreateEnvironment(Transform root, int groundLayer)
    {
        Material groundMaterial = CreateMaterial("Assets/Promotion/Materials/MAT_Ground.mat", new Color(0.32f, 0.44f, 0.32f));
        Material platformMaterial = CreateMaterial("Assets/Promotion/Materials/MAT_Platform.mat", new Color(0.48f, 0.45f, 0.39f));

        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground_Main";
        ground.transform.SetParent(root);
        ground.transform.localScale = new Vector3(4f, 1f, 4f);
        ground.layer = groundLayer;
        ground.GetComponent<Renderer>().sharedMaterial = groundMaterial;

        CreatePlatform(root, groundLayer, platformMaterial, "Platform_JumpCheck", new Vector3(4f, 0.35f, 4f), new Vector3(2.6f, 0.35f, 2.6f));
        CreatePlatform(root, groundLayer, platformMaterial, "Platform_RunLine", new Vector3(-4f, 0.15f, 7f), new Vector3(5f, 0.3f, 1.5f));
        CreateBackdrop(root);
    }

    // 점프와 착지를 영상으로 확인하기 쉽게 낮은 발판을 배치합니다.
    private static void CreatePlatform(Transform root, int groundLayer, Material material, string objectName, Vector3 position, Vector3 scale)
    {
        GameObject platform = GameObject.CreatePrimitive(PrimitiveType.Cube);
        platform.name = objectName;
        platform.transform.SetParent(root);
        platform.transform.position = position;
        platform.transform.localScale = scale;
        platform.layer = groundLayer;
        platform.GetComponent<Renderer>().sharedMaterial = material;
    }

    // 가장 작은 배경 이미지를 실제 3D 씬 뒤쪽 보드 텍스처로 사용합니다.
    private static void CreateBackdrop(Transform root)
    {
        Texture2D backdropTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(BackgroundTexturePath);
        Material backdropMaterial = CreateTexturedMaterial("Assets/Promotion/Materials/MAT_Backdrop.mat", backdropTexture);

        GameObject backdrop = GameObject.CreatePrimitive(PrimitiveType.Quad);
        backdrop.name = "Backdrop_KitbashImage";
        backdrop.transform.SetParent(root);
        backdrop.transform.position = new Vector3(0f, 5.2f, 18f);
        backdrop.transform.localScale = new Vector3(18f, 10f, 1f);
        backdrop.GetComponent<Renderer>().sharedMaterial = backdropMaterial;

        Collider collider = backdrop.GetComponent<Collider>();
        if (collider != null)
        {
            Object.DestroyImmediate(collider);
        }
    }

    // 플레이어는 여러 전용 컴포넌트를 가진 배우입니다.
    private static GameObject CreatePlayer(Transform root, int playerLayer, int groundLayer, RuntimeAnimatorController controller)
    {
        GameObject player = new GameObject("Player_Mage");
        player.transform.SetParent(root);
        player.transform.position = new Vector3(0f, 0.05f, 0f);
        player.layer = playerLayer;

        Rigidbody rigidbody = player.AddComponent<Rigidbody>();
        rigidbody.mass = 1f;
        rigidbody.freezeRotation = true;
        rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
        rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        CapsuleCollider collider = player.AddComponent<CapsuleCollider>();
        collider.height = 1.8f;
        collider.radius = 0.36f;
        collider.center = new Vector3(0f, 0.9f, 0f);

        Transform visualRoot = new GameObject("VisualRoot").transform;
        visualRoot.SetParent(player.transform);
        visualRoot.localPosition = Vector3.zero;
        visualRoot.localRotation = Quaternion.identity;
        visualRoot.localScale = Vector3.one;
        CreateMageVisual(visualRoot);

        Transform groundCheck = new GameObject("GroundCheck").transform;
        groundCheck.SetParent(player.transform);
        groundCheck.localPosition = new Vector3(0f, 0.08f, 0f);

        Transform cameraFocusAnchor = new GameObject("CameraFocusAnchor").transform;
        cameraFocusAnchor.SetParent(player.transform);
        cameraFocusAnchor.localPosition = new Vector3(0f, 1.35f, 0.08f);

        Animator animator = player.AddComponent<Animator>();
        animator.runtimeAnimatorController = controller;

        AudioSource audioSource = player.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0.35f;

        DaniTechPlayerInput input = player.AddComponent<DaniTechPlayerInput>();
        DaniTechPlayerStamina stamina = player.AddComponent<DaniTechPlayerStamina>();
        DaniTechPlayerGroundChecker groundChecker = player.AddComponent<DaniTechPlayerGroundChecker>();
        DaniTechPlayerMovement movement = player.AddComponent<DaniTechPlayerMovement>();
        DaniTechPlayerJump jump = player.AddComponent<DaniTechPlayerJump>();
        DaniTechPlayerAnimationView animationView = player.AddComponent<DaniTechPlayerAnimationView>();
        DaniTechFootstepSound footstepSound = player.AddComponent<DaniTechFootstepSound>();

        LayerMask groundMask = 1 << groundLayer;
        SetPrivateField(groundChecker, "Transform_GroundCheck", groundCheck);
        SetPrivateField(groundChecker, "_groundMask", groundMask);
        SetPrivateField(movement, "Rigidbody_Player", rigidbody);
        SetPrivateField(jump, "Rigidbody_Player", rigidbody);
        SetPrivateField(animationView, "Animator_Player", animator);
        SetPrivateField(animationView, "Rigidbody_Player", rigidbody);
        SetPrivateField(footstepSound, "AudioSource_Footstep", audioSource);

        PrefabUtility.SaveAsPrefabAssetAndConnect(player, PlayerPrefabPath, InteractionMode.AutomatedAction);

        EditorUtility.SetDirty(input);
        EditorUtility.SetDirty(stamina);
        return player;
    }

    // 작은 마법사 캐릭터 프리팹을 플레이어 시각 모델로 사용합니다.
    private static void CreateMageVisual(Transform visualRoot)
    {
        GameObject magePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Characters/Fantasy/Mages/MageBlue.prefab");
        GameObject visual;

        if (magePrefab != null)
        {
            visual = PrefabUtility.InstantiatePrefab(magePrefab) as GameObject;
        }
        else
        {
            visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.name = "Fallback_CapsuleVisual";
        }

        visual.transform.SetParent(visualRoot);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = Vector3.one * 1.15f;
        SetLayerRecursive(visual, visualRoot.root.gameObject.layer);

        Collider[] colliders = visual.GetComponentsInChildren<Collider>();
        foreach (Collider collider in colliders)
        {
            Object.DestroyImmediate(collider);
        }
    }

    // 적 에셋은 가장 작은 실제 적 패키지인 Carnivorous Plant를 장면에 배치합니다.
    private static void CreateEnemy(Transform root, int enemyLayer)
    {
        GameObject enemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Forest Creatures Pack/Carnivorous Plant/Prefabs/Carnivorous Plant-Green.prefab");
        GameObject enemy = enemyPrefab != null
            ? PrefabUtility.InstantiatePrefab(enemyPrefab) as GameObject
            : GameObject.CreatePrimitive(PrimitiveType.Cylinder);

        enemy.name = "Enemy_CarnivorousPlant";
        enemy.transform.SetParent(root);
        enemy.transform.position = new Vector3(6f, 0f, 4f);
        enemy.transform.rotation = Quaternion.Euler(0f, -35f, 0f);
        enemy.transform.localScale = Vector3.one * 1.15f;
        SetLayerRecursive(enemy, enemyLayer);
    }

    // 카메라에는 추적 전용 컴포넌트만 붙입니다.
    private static Camera CreateCamera(Transform cameraTarget, Transform inputRoot)
    {
        GameObject cameraObj = new GameObject("Main Camera");
        cameraObj.tag = "MainCamera";
        cameraObj.transform.position = new Vector3(0f, 3.1f, -6.2f);
        cameraObj.transform.rotation = Quaternion.Euler(16f, 0f, 0f);

        Camera camera = cameraObj.AddComponent<Camera>();
        camera.fieldOfView = 60f;
        camera.nearClipPlane = 0.05f;
        camera.farClipPlane = 120f;
        cameraObj.AddComponent<AudioListener>();

        DaniTechThirdPersonCameraFollow cameraFollow = cameraObj.AddComponent<DaniTechThirdPersonCameraFollow>();
        SetPrivateField(cameraFollow, "Transform_Target", cameraTarget);
        SetPrivateField(cameraFollow, "Transform_InputRoot", inputRoot);
        SetPrivateField(cameraFollow, "_followHeight", 0f);
        SetPrivateField(cameraFollow, "_followDistance", 6.1f);
        SetPrivateField(cameraFollow, "_minDistance", 3.1f);
        SetPrivateField(cameraFollow, "_maxDistance", 8.8f);
        return camera;
    }

    // 플레이어 이동 컴포넌트에 카메라 기준 Transform만 전달합니다.
    private static void ConnectPlayerCamera(GameObject player, Camera camera)
    {
        DaniTechPlayerMovement movement = player.GetComponent<DaniTechPlayerMovement>();
        SetPrivateField(movement, "Transform_CameraRoot", camera.transform);
    }

    // HUD 프리팹은 에디터 제작 단계에서 만들어 둡니다.
    // 런타임에는 UIManager가 이 프리팹을 Instantiate하므로 UI 생성 규칙을 지킬 수 있습니다.
    private static DaniTechGameHUDUI CreateGameHUDPrefab()
    {
        AssetDatabase.DeleteAsset(HudPrefabPath);

        GameObject hudObj = new GameObject("HUD_GameStatus", typeof(RectTransform), typeof(CanvasGroup), typeof(Image), typeof(DaniTechGameHUDUI));
        RectTransform hudRect = hudObj.GetComponent<RectTransform>();
        hudRect.anchorMin = new Vector2(0f, 1f);
        hudRect.anchorMax = new Vector2(0f, 1f);
        hudRect.pivot = new Vector2(0f, 1f);
        hudRect.anchoredPosition = new Vector2(18f, -18f);
        hudRect.sizeDelta = new Vector2(430f, 255f);

        Image backgroundImage = hudObj.GetComponent<Image>();
        backgroundImage.color = new Color(0.055f, 0.075f, 0.095f, 0.88f);

        CanvasGroup canvasGroup = hudObj.GetComponent<CanvasGroup>();
        canvasGroup.alpha = 0.96f;

        Outline outline = hudObj.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.42f);
        outline.effectDistance = new Vector2(1.2f, -1.2f);

        Text titleText = CreateHudText(hudRect, "Text_Title", "Platinum Controller Monitor", 21, FontStyle.Bold, new Color(0.92f, 0.96f, 1f), new Vector2(22f, -18f), new Vector2(386f, 28f));
        Text moveText = CreateHudText(hudRect, "Text_MoveStatus", "Move  | Ready", 14, FontStyle.Normal, new Color(0.78f, 0.9f, 1f), new Vector2(22f, -58f), new Vector2(386f, 22f));
        Text jumpText = CreateHudText(hudRect, "Text_JumpStatus", "Jump  | Ready", 14, FontStyle.Normal, new Color(0.88f, 0.84f, 1f), new Vector2(22f, -84f), new Vector2(386f, 22f));
        Text animationText = CreateHudText(hudRect, "Text_AnimationStatus", "Anim  | Ready", 14, FontStyle.Normal, new Color(0.92f, 0.92f, 0.82f), new Vector2(22f, -110f), new Vector2(386f, 22f));
        Text cameraText = CreateHudText(hudRect, "Text_CameraStatus", "Camera| Ready", 14, FontStyle.Normal, new Color(0.76f, 0.95f, 0.86f), new Vector2(22f, -136f), new Vector2(386f, 22f));
        Text staminaText = CreateHudText(hudRect, "Text_StaminaStatus", "Stamina | Ready", 14, FontStyle.Normal, new Color(0.95f, 0.9f, 0.78f), new Vector2(22f, -162f), new Vector2(386f, 22f));
        Text footstepText = CreateHudText(hudRect, "Text_FootstepStatus", "Sound | Ready", 14, FontStyle.Normal, new Color(0.9f, 0.86f, 0.76f), new Vector2(22f, -214f), new Vector2(386f, 22f));

        Image staminaBackImage = CreateHudImage(hudRect, "Image_StaminaBack", new Color(0.15f, 0.18f, 0.2f, 0.95f), new Vector2(22f, -192f), new Vector2(386f, 13f));
        Image staminaFillImage = CreateHudImage(staminaBackImage.rectTransform, "Image_StaminaFill", new Color(0.25f, 0.8f, 0.45f, 1f), Vector2.zero, new Vector2(386f, 13f));
        RectTransform staminaFillRect = staminaFillImage.rectTransform;
        staminaFillRect.anchorMin = new Vector2(0f, 0f);
        staminaFillRect.anchorMax = new Vector2(1f, 1f);
        staminaFillRect.pivot = new Vector2(0f, 0.5f);
        staminaFillRect.offsetMin = Vector2.zero;
        staminaFillRect.offsetMax = Vector2.zero;
        staminaFillImage.type = Image.Type.Filled;
        staminaFillImage.fillMethod = Image.FillMethod.Horizontal;
        staminaFillImage.fillOrigin = 0;
        staminaFillImage.fillAmount = 1f;

        DaniTechGameHUDUI hudUI = hudObj.GetComponent<DaniTechGameHUDUI>();
        SetPrivateField(hudUI, "Text_Title", titleText);
        SetPrivateField(hudUI, "Text_MoveStatus", moveText);
        SetPrivateField(hudUI, "Text_JumpStatus", jumpText);
        SetPrivateField(hudUI, "Text_AnimationStatus", animationText);
        SetPrivateField(hudUI, "Text_CameraStatus", cameraText);
        SetPrivateField(hudUI, "Text_StaminaStatus", staminaText);
        SetPrivateField(hudUI, "Text_FootstepStatus", footstepText);
        SetPrivateField(hudUI, "Image_StaminaFill", staminaFillImage);

        GameObject prefabObj = PrefabUtility.SaveAsPrefabAsset(hudObj, HudPrefabPath);
        Object.DestroyImmediate(hudObj);
        return prefabObj.GetComponent<DaniTechGameHUDUI>();
    }

    // HUD 텍스트 한 줄을 생성합니다.
    private static Text CreateHudText(RectTransform parent, string objectName, string defaultText, int fontSize, FontStyle fontStyle, Color color, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        GameObject textObj = new GameObject(objectName, typeof(RectTransform), typeof(Text));
        RectTransform rect = textObj.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;

        Text text = textObj.GetComponent<Text>();
        text.font = GetBuiltinFont();
        text.text = defaultText;
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.color = color;
        text.alignment = TextAnchor.MiddleLeft;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Truncate;

        Shadow shadow = textObj.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.45f);
        shadow.effectDistance = new Vector2(1f, -1f);
        return text;
    }

    // HUD 이미지 요소를 생성합니다.
    private static Image CreateHudImage(RectTransform parent, string objectName, Color color, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        GameObject imageObj = new GameObject(objectName, typeof(RectTransform), typeof(Image));
        RectTransform rect = imageObj.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;

        Image image = imageObj.GetComponent<Image>();
        image.color = color;
        return image;
    }

    // Unity 내장 폰트를 사용해 별도 폰트 에셋 없이도 HUD가 렌더링되게 합니다.
    private static Font GetBuiltinFont()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null)
        {
            font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        return font;
    }

    // 씬에 Canvas와 UIManager를 배치합니다.
    private static void CreateUI(GameObject player, Camera camera, DaniTechGameHUDUI hudPrefab)
    {
        GameObject canvasObj = new GameObject("Canvas_GameUI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObj.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = camera;
        canvas.planeDistance = 1f;
        canvas.sortingOrder = 20;

        CanvasScaler canvasScaler = canvasObj.GetComponent<CanvasScaler>();
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.referenceResolution = new Vector2(1280f, 720f);
        canvasScaler.matchWidthOrHeight = 0.5f;

        GameObject uiManagerObj = new GameObject("DaniTechUIManager");
        DaniTechUIManager uiManager = uiManagerObj.AddComponent<DaniTechUIManager>();
        SetPrivateField(uiManager, "Prefab_GameHUDUI", hudPrefab);
        SetPrivateField(uiManager, "Transform_UIRoot", canvasObj.transform);
        SetPrivateField(uiManager, "GameObject_Player", player);
        SetPrivateField(uiManager, "CameraFollow_Main", camera.GetComponent<DaniTechThirdPersonCameraFollow>());
    }

    // 기본 조명과 하늘 톤을 설정합니다.
    private static void CreateLighting()
    {
        GameObject lightObj = new GameObject("Directional Light");
        Light light = lightObj.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.15f;
        lightObj.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        RenderSettings.ambientLight = new Color(0.42f, 0.48f, 0.55f);
        RenderSettings.skybox = null;
    }

    // 단색 머티리얼을 생성합니다.
    private static Material CreateMaterial(string assetPath, Color color)
    {
        AssetDatabase.DeleteAsset(assetPath);
        Material material = new Material(Shader.Find("Standard"));
        material.color = color;
        AssetDatabase.CreateAsset(material, assetPath);
        return material;
    }

    // 텍스처 머티리얼을 생성합니다.
    private static Material CreateTexturedMaterial(string assetPath, Texture2D texture)
    {
        AssetDatabase.DeleteAsset(assetPath);
        Material material = new Material(Shader.Find("Unlit/Texture") ?? Shader.Find("Standard"));

        if (texture != null)
        {
            if (material.HasProperty("_MainTex"))
            {
                material.SetTexture("_MainTex", texture);
            }

            if (material.HasProperty("_BaseMap"))
            {
                material.SetTexture("_BaseMap", texture);
            }
        }

        AssetDatabase.CreateAsset(material, assetPath);
        return material;
    }

    // 프리팹 내부 자식까지 레이어를 통일합니다.
    private static void SetLayerRecursive(GameObject target, int layer)
    {
        target.layer = layer;
        foreach (Transform child in target.transform)
        {
            SetLayerRecursive(child.gameObject, layer);
        }
    }

    // Inspector 전용 private 필드를 Editor 생성기에서만 채웁니다.
    // 런타임 코드에서는 public 필드 난사를 피하고, 씬 제작 단계에서 참조를 연결합니다.
    private static void SetPrivateField(Object target, string fieldName, object value)
    {
        FieldInfo fieldInfo = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        if (fieldInfo == null)
        {
            Debug.LogWarning($"{target.GetType().Name}에서 {fieldName} 필드를 찾을 수 없습니다.");
            return;
        }

        fieldInfo.SetValue(target, value);
        EditorUtility.SetDirty(target);
    }
}
