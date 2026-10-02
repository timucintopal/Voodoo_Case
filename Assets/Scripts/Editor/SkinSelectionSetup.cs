using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

// One-click, re-runnable setup of everything the skin selection screen needs in the project and the Game scene.
public static class SkinSelectionSetup
{
    private const string    c_ScenePath = "Assets/Scenes/Game.unity";
    private const string    c_MainCameraPrefabPath = "Assets/Prefabs/MainCamera.prefab";
    private const string    c_MenuBrushesFolder = "Assets/Prefabs/Brushs/MainMenuBrushes";
    private const string    c_PreviewBrushesFolder = "Assets/Prefabs/Brushs/SkinSelectBrushes";
    private const string    c_MenuBrushSuffix = "-Menu";
    private const string    c_PreviewBrushSuffix = "-SkinSelect";
    private const string    c_AtlasName = "SkinPreviewAtlas";
    private const string    c_ViewPrefabPath = "Assets/Prefabs/UI/SkinSelectionView.prefab";
    private const string    c_CellPrefabPath = "Assets/Prefabs/UI/SkinCell.prefab";
    private const string    c_SpritesFolder = "Assets/Textures/UI/Skin Selection Screen UI/Sprites/";
    private const string    c_CellSpritePath = c_SpritesFolder + "case.png";
    private const string    c_BackButtonSpritePath = c_SpritesFolder + "backButton.png";
    private const string    c_BackButtonShineSpritePath = c_SpritesFolder + "backButton_shine.png";
    private const string    c_PodiumSpritePath = c_SpritesFolder + "baseSkin_colored.png";
    private const string    c_LightSpritePath = c_SpritesFolder + "light.png";
    private const string    c_MenuButtonSpritePath = c_SpritesFolder + "iconSkin.png";
    private const string    c_MainMenuPrefabPath = "Assets/Prefabs/UI/MainMenuView.prefab";
    private const string    c_MenuButtonName = "SkinSelectionButton";
    private const string    c_LegacyBrushSelectName = "BrushSelect";
    private static readonly Vector2 c_MenuButtonPosition = new Vector2(0f, 996f); // From the bottom center of the screen
    private const float     c_MenuButtonSize = 350f;
    private const int       c_UILayer = 5;
    private const int       c_ColumnCount = 3;
    private const float     c_ListTopInset = 1080f;
    private const float     c_BackButtonSize = 150f;
    private const float     c_BackButtonMargin = 45f;
    private const float     c_BackButtonShineMargin = 14f;
    private static readonly Color c_BackgroundColor = new Color(0.11f, 0.10f, 0.27f);
    private static readonly Color c_CellColor = new Color(0.16f, 0.13f, 0.37f);
    private static readonly Color c_BackButtonColor = new Color(0.45f, 0.25f, 0.95f);
    private static readonly Color c_SelectedColor = new Color(0.75f, 0.60f, 1f);
    private const float     c_SelectedFrameWidth = 12f;

    // Podium, in canvas units from the top center of the screen
    private static readonly Vector2 c_PodiumPosition = new Vector2(0f, -867f);
    private static readonly Vector2 c_PodiumSize = new Vector2(720f, 285f);
    private static readonly Vector2 c_LightPosition = new Vector2(0f, -417f);
    private static readonly Vector2 c_LightSize = new Vector2(760f, 880f);
    private static readonly Vector3 c_HeroPosition = new Vector3(0f, -637f, -200f); // Negative depth: in front of the canvas

    // Brushes of a same row of colors are listed in this order
    private static readonly string[] c_BrushDisplayOrder = { "Brush_2", "Brush_1" };
    private const int       c_FirstUserLayer = 8;
    private const float     c_PreviewRotationSpeed = 30f;
    private const float     c_AtlasCameraDistance = 10f;
    private static readonly Vector3 c_AtlasPosition = new Vector3(0f, -1000f, 0f);

    [MenuItem("Tools/Skin Selection/Setup")]
    public static void Setup()
    {
        if (EditorSceneManager.GetActiveScene().path != c_ScenePath)
        {
            Debug.LogError("Skin selection setup must be run with " + c_ScenePath + " opened.");
            return;
        }

        int layer = EnsureLayer(Constants.c_SkinPreviewLayer);
        if (layer < 0)
            return;

        SkinPreviewAtlas atlas = CreateAtlas(layer);
        ExcludeLayerFromOtherCameras(layer, atlas.m_Camera);
        CreatePreviewPrefabs(layer);
        SetupMainMenu();
        CreateView(atlas, CreateCellPrefab());

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("Skin selection setup done. Save the scene to keep the changes.");
    }

    // Throws away the generated view and cell, including any manual tweak made on them, and builds them again
    [MenuItem("Tools/Skin Selection/Rebuild View")]
    public static void RebuildView()
    {
        SkinSelectionView view = Object.FindObjectOfType<SkinSelectionView>(true);
        if (view != null)
            Undo.DestroyObjectImmediate(view.gameObject);

        AssetDatabase.DeleteAsset(c_ViewPrefabPath);
        AssetDatabase.DeleteAsset(c_CellPrefabPath);

        Setup();
    }

    [MenuItem("Tools/Skin Selection/Rebuild View", true)]
    public static bool CanRebuildView()
    {
        return Application.isPlaying == false;
    }

    #region Layer

    private static int EnsureLayer(string _Name)
    {
        int layer = LayerMask.NameToLayer(_Name);
        if (layer >= 0)
            return layer;

        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty layers = tagManager.FindProperty("layers");

        for (int i = c_FirstUserLayer; i < layers.arraySize; ++i)
        {
            SerializedProperty layerProperty = layers.GetArrayElementAtIndex(i);
            if (string.IsNullOrEmpty(layerProperty.stringValue) == false)
                continue;

            layerProperty.stringValue = _Name;
            tagManager.ApplyModifiedProperties();
            return i;
        }

        Debug.LogError("No free layer left for " + _Name);
        return -1;
    }

    private static void ExcludeLayerFromOtherCameras(int _Layer, Camera _AtlasCamera)
    {
        int layerMask = 1 << _Layer;

        GameObject cameraPrefab = PrefabUtility.LoadPrefabContents(c_MainCameraPrefabPath);
        foreach (Camera camera in cameraPrefab.GetComponentsInChildren<Camera>(true))
            camera.cullingMask &= ~layerMask;
        PrefabUtility.SaveAsPrefabAsset(cameraPrefab, c_MainCameraPrefabPath);
        PrefabUtility.UnloadPrefabContents(cameraPrefab);

        // Scene cameras which override their prefab mask, or which are not prefabs at all
        foreach (Camera camera in Object.FindObjectsOfType<Camera>(true))
        {
            if (camera == _AtlasCamera || (camera.cullingMask & layerMask) == 0)
                continue;

            Undo.RecordObject(camera, "Exclude skin preview layer");
            camera.cullingMask &= ~layerMask;
            PrefabUtility.RecordPrefabInstancePropertyModifications(camera);
        }
    }

    #endregion

    #region Preview prefabs

    private static void CreatePreviewPrefabs(int _Layer)
    {
        if (AssetDatabase.IsValidFolder(c_PreviewBrushesFolder) == false)
            AssetDatabase.CreateFolder("Assets/Prefabs/Brushs", "SkinSelectBrushes");

        foreach (BrushData brush in GetBrushesUsedBySkins())
        {
            string menuPath = c_MenuBrushesFolder + "/" + brush.name + c_MenuBrushSuffix + ".prefab";
            string previewPath = c_PreviewBrushesFolder + "/" + brush.name + c_PreviewBrushSuffix + ".prefab";

            if (AssetDatabase.LoadAssetAtPath<GameObject>(previewPath) == null && AssetDatabase.CopyAsset(menuPath, previewPath) == false)
            {
                Debug.LogError("Can't create " + previewPath + " from " + menuPath);
                continue;
            }

            GameObject preview = PrefabUtility.LoadPrefabContents(previewPath);

            foreach (Transform child in preview.GetComponentsInChildren<Transform>(true))
                child.gameObject.layer = _Layer;

            foreach (BrushRotation rotation in preview.GetComponentsInChildren<BrushRotation>(true))
                rotation.m_Speed = c_PreviewRotationSpeed;

            foreach (Renderer renderer in preview.GetComponentsInChildren<Renderer>(true))
            {
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }

            PrefabUtility.SaveAsPrefabAsset(preview, previewPath);
            PrefabUtility.UnloadPrefabContents(preview);

            brush.m_PreviewPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(previewPath);
            EditorUtility.SetDirty(brush);
        }
    }

    private static List<BrushData> GetBrushesUsedBySkins()
    {
        List<SkinData> skins = LoadAllSkins();
        List<BrushData> brushes = new List<BrushData>();

        for (int i = 0; i < skins.Count; ++i)
        {
            if (brushes.Contains(skins[i].Brush) == false)
                brushes.Add(skins[i].Brush);
        }

        return brushes;
    }

    private static List<SkinData> LoadAllSkins()
    {
        List<SkinData> skins = new List<SkinData>(Resources.LoadAll<SkinData>(Constants.c_SkinsPath));
        skins.AddRange(Resources.LoadAll<SkinData>(Constants.c_ExtendedSkinsPath));
        return skins;
    }

    #endregion

    #region Atlas

    private static SkinPreviewAtlas CreateAtlas(int _Layer)
    {
        SkinPreviewAtlas atlas = Object.FindObjectOfType<SkinPreviewAtlas>(true);

        if (atlas == null)
        {
            GameObject atlasGO = new GameObject(c_AtlasName);
            Undo.RegisterCreatedObjectUndo(atlasGO, "Create skin preview atlas");
            atlasGO.transform.position = c_AtlasPosition;
            atlas = atlasGO.AddComponent<SkinPreviewAtlas>();

            atlas.m_Camera = CreateChild(atlasGO.transform, "Camera").AddComponent<Camera>();
            atlas.m_Camera.transform.localPosition = Vector3.back * c_AtlasCameraDistance;
            atlas.m_PreviewsParent = CreateChild(atlasGO.transform, "Previews").transform;
            atlas.m_PreviewsParent.gameObject.SetActive(false);
        }

        Camera camera = atlas.m_Camera;
        camera.enabled = false;
        camera.orthographic = true;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.clear;
        camera.cullingMask = 1 << _Layer;
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = c_AtlasCameraDistance * 2f;
        camera.allowHDR = false;
        camera.useOcclusionCulling = false;

        foreach (Transform child in atlas.GetComponentsInChildren<Transform>(true))
            child.gameObject.layer = _Layer;

        EditorUtility.SetDirty(atlas);
        return atlas;
    }

    private static GameObject CreateChild(Transform _Parent, string _Name)
    {
        GameObject child = new GameObject(_Name);
        child.transform.SetParent(_Parent, false);
        return child;
    }

    #endregion

    #region Main menu

    // Adds the button opening the skin selection screen next to the legacy selection: the view enables one or the other
    private static void SetupMainMenu()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(c_MainMenuPrefabPath);
        MainMenuView mainMenu = root.GetComponent<MainMenuView>();
        Transform brushSelect = root.transform.Find(c_LegacyBrushSelectName);
        Transform buttonTr = root.transform.Find(c_MenuButtonName);

        if (buttonTr == null)
        {
            RectTransform buttonRect = CreateUI(c_MenuButtonName, root.transform);
            buttonRect.anchorMin = buttonRect.anchorMax = new Vector2(0.5f, 0f);
            buttonRect.anchoredPosition = c_MenuButtonPosition;
            buttonRect.sizeDelta = Vector2.one * c_MenuButtonSize;
            buttonRect.SetSiblingIndex(brushSelect.GetSiblingIndex() + 1);

            Image icon = buttonRect.gameObject.AddComponent<Image>();
            icon.sprite = LoadSprite(c_MenuButtonSpritePath);

            Button button = buttonRect.gameObject.AddComponent<Button>();
            button.targetGraphic = icon;
            UnityEventTools.AddPersistentListener(button.onClick, mainMenu.OnSkinSelectionButton);

            // Takes the player color, as the rest of the menu
            mainMenu.m_ColoredImages.Add(icon);
            buttonTr = buttonRect;
        }

        mainMenu.m_BrushSelect = brushSelect.gameObject;
        mainMenu.m_SkinSelectionButton = buttonTr.gameObject;

        PrefabUtility.SaveAsPrefabAsset(root, c_MainMenuPrefabPath);
        PrefabUtility.UnloadPrefabContents(root);
    }

    #endregion

    #region View

    private static SkinCell CreateCellPrefab()
    {
        SkinCell cellPrefab = AssetDatabase.LoadAssetAtPath<SkinCell>(c_CellPrefabPath);
        if (cellPrefab != null)
            return cellPrefab;

        Sprite sprite = LoadSprite(c_CellSpritePath);
        RectTransform root = CreateUI("SkinCell", null);
        SkinCell cell = root.gameObject.AddComponent<SkinCell>();

        // Slightly bigger than the cell, so it shows as a frame around it
        RectTransform frameRect = CreateUI("SelectedFrame", root);
        Stretch(frameRect);
        frameRect.offsetMin = -Vector2.one * c_SelectedFrameWidth;
        frameRect.offsetMax = Vector2.one * c_SelectedFrameWidth;
        Image frame = frameRect.gameObject.AddComponent<Image>();
        frame.sprite = sprite;
        frame.color = c_SelectedColor;
        frame.raycastTarget = false;
        cell.m_SelectedFrame = frameRect.gameObject;

        RectTransform backgroundRect = CreateUI("Background", root);
        Stretch(backgroundRect);
        Image background = backgroundRect.gameObject.AddComponent<Image>();
        background.sprite = sprite;
        background.color = c_CellColor;

        RectTransform previewRect = CreateUI("Preview", root);
        Stretch(previewRect);
        cell.m_Preview = previewRect.gameObject.AddComponent<RawImage>();
        cell.m_Preview.raycastTarget = false;

        cell.m_Button = root.gameObject.AddComponent<Button>();
        cell.m_Button.targetGraphic = background;

        PrefabUtility.SaveAsPrefabAsset(root.gameObject, c_CellPrefabPath);
        Object.DestroyImmediate(root.gameObject);

        return AssetDatabase.LoadAssetAtPath<SkinCell>(c_CellPrefabPath);
    }

    private static void CreateView(SkinPreviewAtlas _Atlas, SkinCell _CellPrefab)
    {
        SkinSelectionView view = Object.FindObjectOfType<SkinSelectionView>(true);

        if (view == null)
        {
            MainMenuView mainMenu = Object.FindObjectOfType<MainMenuView>(true);
            Transform canvas = mainMenu.transform.parent;
            GameObject viewPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(c_ViewPrefabPath);

            if (viewPrefab != null)
            {
                view = ((GameObject)PrefabUtility.InstantiatePrefab(viewPrefab, canvas)).GetComponent<SkinSelectionView>();
            }
            else
            {
                view = BuildView(canvas, _CellPrefab);
                PrefabUtility.SaveAsPrefabAssetAndConnect(view.gameObject, c_ViewPrefabPath, InteractionMode.AutomatedAction);
            }

            Undo.RegisterCreatedObjectUndo(view.gameObject, "Create skin selection view");

            // Drawn above the main menu
            view.transform.SetSiblingIndex(mainMenu.transform.GetSiblingIndex() + 1);
        }

        // The atlas lives in the scene, so it can't be referenced by the prefab itself
        view.m_Atlas = _Atlas;
        _Atlas.m_Columns = c_ColumnCount;

        // Hidden in the scene as the other views, the prefab stays visible to be edited
        view.m_Group.alpha = 0f;
        PrefabUtility.RecordPrefabInstancePropertyModifications(view.m_Group);
        PrefabUtility.RecordPrefabInstancePropertyModifications(view);
        EditorUtility.SetDirty(view);
        EditorUtility.SetDirty(_Atlas);
    }

    private static SkinSelectionView BuildView(Transform _Canvas, SkinCell _CellPrefab)
    {
        RectTransform root = CreateUI("SkinSelectionView", _Canvas);
        Stretch(root);
        SkinSelectionView view = root.gameObject.AddComponent<SkinSelectionView>();
        view.m_Group = root.gameObject.AddComponent<CanvasGroup>();
        view.m_CellPrefab = _CellPrefab;
        view.m_DisplayedSkins = GetSkinsInDisplayOrder();

        // Opaque: hides the menu and the terrain, and blocks their inputs
        RectTransform background = CreateUI("Background", root);
        Stretch(background);
        background.gameObject.AddComponent<Image>().color = c_BackgroundColor;

        BuildPodium(root, view);
        view.m_Grid = BuildList(root);
        BuildBackButton(root, view);

        return view;
    }

    private static GridLayoutGroup BuildList(Transform _Parent)
    {
        RectTransform scrollView = CreateUI("ScrollView", _Parent);
        Stretch(scrollView);
        scrollView.offsetMax = new Vector2(0f, -c_ListTopInset);
        scrollView.gameObject.AddComponent<RectMask2D>();

        // Invisible, catches the drags between the cells
        scrollView.gameObject.AddComponent<Image>().color = Color.clear;

        RectTransform content = CreateUI("Content", scrollView);
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = Vector2.one;
        content.pivot = new Vector2(0.5f, 1f);
        content.offsetMin = Vector2.zero;
        content.offsetMax = Vector2.zero;

        GridLayoutGroup grid = content.gameObject.AddComponent<GridLayoutGroup>();
        grid.padding = new RectOffset(40, 40, 20, 60);
        grid.spacing = new Vector2(30f, 30f);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = c_ColumnCount;

        content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        ScrollRect scrollRect = scrollView.gameObject.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.viewport = scrollView;
        scrollRect.content = content;

        return grid;
    }

    private static void BuildPodium(Transform _Parent, SkinSelectionView _View)
    {
        RectTransform podium = CreateUI("Podium", _Parent);
        podium.anchorMin = podium.anchorMax = podium.pivot = new Vector2(0.5f, 1f);
        podium.anchoredPosition = Vector2.zero;
        podium.sizeDelta = Vector2.zero;

        BuildImage("Base", podium, c_PodiumSpritePath, c_PodiumPosition, c_PodiumSize);
        BuildImage("Light", podium, c_LightSpritePath, c_LightPosition, c_LightSize);

        // The selected brush is a mesh: not faded by the canvas group, so it goes under a stage toggled by the view
        Transform stage = CreateChild(podium, "Stage").transform;
        Transform heroParent = CreateChild(stage, "HeroParent").transform;
        heroParent.localPosition = c_HeroPosition;

        _View.m_Stage = stage.gameObject;
        _View.m_HeroParent = heroParent;
    }

    private static Image BuildImage(string _Name, Transform _Parent, string _SpritePath, Vector2 _Position, Vector2 _Size)
    {
        RectTransform rect = CreateUI(_Name, _Parent);
        rect.anchoredPosition = _Position;
        rect.sizeDelta = _Size;

        Image image = rect.gameObject.AddComponent<Image>();
        image.sprite = LoadSprite(_SpritePath);
        image.raycastTarget = false;

        return image;
    }

    // The sprites of the screen were dropped in the project as default textures
    private static Sprite LoadSprite(string _Path)
    {
        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(_Path);

        if (importer.textureType != TextureImporterType.Sprite)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(_Path);
    }

    private static void BuildBackButton(Transform _Parent, SkinSelectionView _View)
    {
        RectTransform buttonRect = CreateUI("BackButton", _Parent);
        buttonRect.anchorMin = buttonRect.anchorMax = buttonRect.pivot = new Vector2(0f, 1f);
        buttonRect.sizeDelta = Vector2.one * c_BackButtonSize;
        buttonRect.anchoredPosition = new Vector2(c_BackButtonMargin, -c_BackButtonMargin);

        // The arrow is a hole in the sprite
        Image circle = buttonRect.gameObject.AddComponent<Image>();
        circle.sprite = LoadSprite(c_BackButtonSpritePath);
        circle.color = c_BackButtonColor;

        Button button = buttonRect.gameObject.AddComponent<Button>();
        button.targetGraphic = circle;
        UnityEventTools.AddPersistentListener(button.onClick, _View.Close);

        Image shine = BuildImage("Shine", buttonRect, c_BackButtonShineSpritePath, Vector2.zero, Vector2.zero);
        shine.rectTransform.anchorMin = shine.rectTransform.anchorMax = shine.rectTransform.pivot = new Vector2(1f, 0f);
        shine.rectTransform.anchoredPosition = new Vector2(-c_BackButtonShineMargin, c_BackButtonShineMargin);
        shine.SetNativeSize();
        shine.rectTransform.sizeDelta *= c_BackButtonSize / circle.sprite.rect.width;
    }

    // Skins are saved by index, so the mock-up order is only a matter of display:
    // rows of 3 colors, and for each of these rows one line per brush
    private static List<SkinData> GetSkinsInDisplayOrder()
    {
        List<SkinData> skins = LoadAllSkins();
        List<ColorData> colors = skins.Select(skin => skin.Color).Distinct().ToList();

        return skins
            .OrderBy(skin => colors.IndexOf(skin.Color) / c_ColumnCount)
            .ThenBy(skin => System.Array.IndexOf(c_BrushDisplayOrder, skin.Brush.name))
            .ThenBy(skin => colors.IndexOf(skin.Color))
            .ToList();
    }

    private static RectTransform CreateUI(string _Name, Transform _Parent)
    {
        GameObject go = new GameObject(_Name, typeof(RectTransform));
        go.layer = c_UILayer;
        go.transform.SetParent(_Parent, false);
        return (RectTransform)go.transform;
    }

    private static void Stretch(RectTransform _Rect)
    {
        _Rect.anchorMin = Vector2.zero;
        _Rect.anchorMax = Vector2.one;
        _Rect.offsetMin = Vector2.zero;
        _Rect.offsetMax = Vector2.zero;
    }

    #endregion
}
