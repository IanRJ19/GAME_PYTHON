using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// One-click assembly of the Phase 0 vertical-slice test scene. Built entirely through
// Unity's own GameObject/AssetDatabase/SerializedObject APIs rather than a hand-written
// .unity file, so a mistake here surfaces as a script error instead of a silently
// corrupt scene. Menu: PixelDepth > Build Test Scene.
public static class SceneBootstrapper
{
    private const string GeneratedFolder = "Assets/Generated";
    private const string SceneFolder = "Assets/Scenes";
    private const string ScenePath = SceneFolder + "/Main.unity";
    private const string DarkKnightPath = "Assets/sprites/player/dark_knight/idle/";

    [MenuItem("PixelDepth/Build Test Scene")]
    public static void BuildTestScene()
    {
        EnsureFolder(GeneratedFolder);
        EnsureFolder(SceneFolder);

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        BuildCamera();
        BuildBootstrap();

        Sprite floorSprite = CreateAndImportSprite("floor_tile", SolidTexture(64, 64, new Color(0.16f, 0.16f, 0.20f)), 64f);
        BuildFloorAndWalls(floorSprite);

        Sprite enemySprite = CreateAndImportSprite("enemy_placeholder", CircleTexture(64, new Color(0.75f, 0.20f, 0.22f)), 64f);

        Sprite[] knightSprites = LoadDarkKnightSprites();
        GameObject player = BuildPlayer(knightSprites);

        BuildEnemy(new Vector2(2.2f, 1.2f), enemySprite);
        BuildEnemy(new Vector2(-2.0f, -1.0f), enemySprite);

        BuildCombatFeedback();
        BuildHud(player);

        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log("PixelDepth test scene built at " + ScenePath);
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path)?.Replace("\\", "/");
        string leaf = Path.GetFileName(path);
        AssetDatabase.CreateFolder(parent, leaf);
    }

    static void BuildCamera()
    {
        GameObject camGO = new GameObject("Main Camera");
        Camera cam = camGO.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 5f;
        cam.transform.position = new Vector3(0f, 0f, -10f);
        camGO.tag = "MainCamera";
        camGO.AddComponent<AudioListener>();
    }

    static void BuildBootstrap()
    {
        GameObject go = new GameObject("Bootstrap");
        go.AddComponent<GameEvents>();
        go.AddComponent<GameState>();
    }

    static Texture2D SolidTexture(int w, int h, Color color)
    {
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[w * h];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = color;
        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    static Texture2D CircleTexture(int diameter, Color color)
    {
        Texture2D tex = new Texture2D(diameter, diameter, TextureFormat.RGBA32, false);
        Vector2 center = new Vector2(diameter / 2f, diameter / 2f);
        float radius = diameter / 2f - 1f;
        Color clear = new Color(color.r, color.g, color.b, 0f);
        for (int y = 0; y < diameter; y++)
        {
            for (int x = 0; x < diameter; x++)
            {
                float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                tex.SetPixel(x, y, dist <= radius ? color : clear);
            }
        }
        tex.Apply();
        return tex;
    }

    static Sprite CreateAndImportSprite(string name, Texture2D tex, float pixelsPerUnit)
    {
        string assetPath = $"{GeneratedFolder}/{name}.png";
        string fullPath = Path.Combine(Application.dataPath, assetPath.Substring("Assets/".Length));
        File.WriteAllBytes(fullPath, tex.EncodeToPNG());
        AssetDatabase.ImportAsset(assetPath);

        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(assetPath);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = pixelsPerUnit;
        importer.alphaIsTransparency = true;
        importer.filterMode = FilterMode.Point;
        importer.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
    }

    static void BuildFloorAndWalls(Sprite floorSprite)
    {
        GameObject floor = new GameObject("Floor");
        SpriteRenderer sr = floor.AddComponent<SpriteRenderer>();
        sr.sprite = floorSprite;
        sr.sortingOrder = -10;
        floor.transform.localScale = new Vector3(12f, 8f, 1f);

        CreateWall("WallTop", new Vector2(0f, 4.1f), new Vector2(12.4f, 0.2f));
        CreateWall("WallBottom", new Vector2(0f, -4.1f), new Vector2(12.4f, 0.2f));
        CreateWall("WallLeft", new Vector2(-6.1f, 0f), new Vector2(0.2f, 8.4f));
        CreateWall("WallRight", new Vector2(6.1f, 0f), new Vector2(0.2f, 8.4f));
    }

    static void CreateWall(string name, Vector2 position, Vector2 size)
    {
        GameObject wall = new GameObject(name);
        wall.transform.position = position;
        BoxCollider2D col = wall.AddComponent<BoxCollider2D>();
        col.size = size;
    }

    static Sprite[] LoadDarkKnightSprites()
    {
        // Order must match PlayerController.idleSprites: N, NE, E, SE, S, SW, W, NW
        string[] dirs = { "n", "ne", "e", "se", "s", "sw", "w", "nw" };
        Sprite[] sprites = new Sprite[8];
        for (int i = 0; i < dirs.Length; i++)
        {
            string path = $"{DarkKnightPath}dark_knight_idle_{dirs[i]}.png";
            sprites[i] = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprites[i] == null)
            {
                Debug.LogWarning($"Could not load sprite at {path} — check that its .meta imported as a Sprite.");
            }
        }
        return sprites;
    }

    static GameObject BuildPlayer(Sprite[] idleSprites)
    {
        GameObject player = new GameObject("Player");
        player.transform.position = Vector2.zero;

        Rigidbody2D rb = player.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        CircleCollider2D col = player.AddComponent<CircleCollider2D>();
        col.radius = 0.4f;

        SpriteRenderer sr = player.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 10;
        if (idleSprites != null && idleSprites.Length > 4) sr.sprite = idleSprites[4]; // "S" — faces camera

        StatsComponent stats = player.AddComponent<StatsComponent>();
        HealthComponent health = player.AddComponent<HealthComponent>();

        GameObject attackPivot = new GameObject("AttackPivot");
        attackPivot.transform.SetParent(player.transform, false);

        GameObject meleeGO = new GameObject("MeleeHitbox");
        meleeGO.transform.SetParent(attackPivot.transform, false);
        BoxCollider2D meleeCol = meleeGO.AddComponent<BoxCollider2D>();
        meleeCol.isTrigger = true;
        meleeCol.size = new Vector2(0.5f, 0.45f);
        MeleeHitbox meleeHitbox = meleeGO.AddComponent<MeleeHitbox>();
        SetLayerMaskField(meleeHitbox, "targetMask", LayerMask.GetMask("EnemyHurtbox"));

        GameObject hurtboxGO = new GameObject("Hurtbox");
        hurtboxGO.transform.SetParent(player.transform, false);
        hurtboxGO.layer = LayerMask.NameToLayer("PlayerHurtbox");
        CircleCollider2D hurtCol = hurtboxGO.AddComponent<CircleCollider2D>();
        hurtCol.isTrigger = true;
        hurtCol.radius = 0.42f;
        Hurtbox hurtbox = hurtboxGO.AddComponent<Hurtbox>();

        PlayerController controller = player.AddComponent<PlayerController>();

        SerializedObject so = new SerializedObject(controller);
        so.FindProperty("attackPivot").objectReferenceValue = attackPivot.transform;
        so.FindProperty("meleeHitbox").objectReferenceValue = meleeHitbox;
        so.FindProperty("health").objectReferenceValue = health;
        so.FindProperty("stats").objectReferenceValue = stats;
        so.FindProperty("spriteRenderer").objectReferenceValue = sr;
        SerializedProperty spritesProp = so.FindProperty("idleSprites");
        spritesProp.arraySize = 8;
        for (int i = 0; i < 8; i++)
        {
            spritesProp.GetArrayElementAtIndex(i).objectReferenceValue =
                (idleSprites != null && i < idleSprites.Length) ? idleSprites[i] : null;
        }
        so.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject hurtSo = new SerializedObject(hurtbox);
        hurtSo.FindProperty("health").objectReferenceValue = health;
        hurtSo.FindProperty("ownerBody").objectReferenceValue = player;
        hurtSo.ApplyModifiedPropertiesWithoutUndo();

        return player;
    }

    static void BuildEnemy(Vector2 position, Sprite sprite)
    {
        GameObject enemy = new GameObject("DummyEnemy");
        enemy.transform.position = position;
        enemy.tag = "Enemy";

        Rigidbody2D rb = enemy.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        CircleCollider2D col = enemy.AddComponent<CircleCollider2D>();
        col.radius = 0.35f;

        SpriteRenderer sr = enemy.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 10;
        sr.sprite = sprite;

        HealthComponent health = enemy.AddComponent<HealthComponent>();

        GameObject hurtboxGO = new GameObject("Hurtbox");
        hurtboxGO.transform.SetParent(enemy.transform, false);
        hurtboxGO.layer = LayerMask.NameToLayer("EnemyHurtbox");
        CircleCollider2D hurtCol = hurtboxGO.AddComponent<CircleCollider2D>();
        hurtCol.isTrigger = true;
        hurtCol.radius = 0.37f;
        Hurtbox hurtbox = hurtboxGO.AddComponent<Hurtbox>();

        DummyEnemy dummy = enemy.AddComponent<DummyEnemy>();

        SerializedObject so = new SerializedObject(dummy);
        so.FindProperty("health").objectReferenceValue = health;
        so.FindProperty("visual").objectReferenceValue = sr;
        so.ApplyModifiedPropertiesWithoutUndo();
        SetLayerMaskField(dummy, "playerMask", LayerMask.GetMask("PlayerHurtbox"));

        SerializedObject hurtSo = new SerializedObject(hurtbox);
        hurtSo.FindProperty("health").objectReferenceValue = health;
        hurtSo.FindProperty("ownerBody").objectReferenceValue = enemy;
        hurtSo.ApplyModifiedPropertiesWithoutUndo();
    }

    static void SetLayerMaskField(Object target, string fieldName, LayerMask mask)
    {
        SerializedObject so = new SerializedObject(target);
        SerializedProperty prop = so.FindProperty(fieldName);
        prop.intValue = mask.value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void BuildCombatFeedback()
    {
        GameObject go = new GameObject("CombatFeedback");
        go.AddComponent<CombatFeedback>();
    }

    static void BuildHud(GameObject player)
    {
        GameObject canvasGO = new GameObject("Canvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        Text healthLabel = CreateLabel(canvasGO.transform, "HealthLabel", new Vector2(16, -16));
        Text manaLabel = CreateLabel(canvasGO.transform, "ManaLabel", new Vector2(16, -40));
        Text xpLabel = CreateLabel(canvasGO.transform, "XpLabel", new Vector2(16, -64));
        Text levelLabel = CreateLabel(canvasGO.transform, "LevelLabel", new Vector2(16, -88));

        GameObject hudGO = new GameObject("PlayerHUD");
        hudGO.transform.SetParent(canvasGO.transform, false);
        PlayerHUD hud = hudGO.AddComponent<PlayerHUD>();

        SerializedObject so = new SerializedObject(hud);
        so.FindProperty("healthLabel").objectReferenceValue = healthLabel;
        so.FindProperty("manaLabel").objectReferenceValue = manaLabel;
        so.FindProperty("xpLabel").objectReferenceValue = xpLabel;
        so.FindProperty("levelLabel").objectReferenceValue = levelLabel;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static Text CreateLabel(Transform parent, string name, Vector2 anchoredPos)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        Text text = go.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 18;
        text.color = Color.white;
        text.alignment = TextAnchor.UpperLeft;
        text.text = name;

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = new Vector2(320, 24);
        return text;
    }
}
