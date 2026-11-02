// MomentumSceneBuilder.cs
//
// WHY THIS IS A SCRIPT AND NOT A .prefab / .unity FILE:
// Unity's scene and prefab files are serialized formats with internal
// GUIDs and file IDs that only Unity itself generates reliably. Hand-writing
// them outside the Editor is a common source of "corrupt asset" / broken
// reference errors. This script instead builds everything through Unity's
// own GameObject/Component/AssetDatabase API, at the click of a menu item —
// so whatever it creates is guaranteed to be something Unity itself made.
//
// SETUP:
//   1. Put this file inside a folder literally named "Editor"
//      (e.g. Assets/MomentumAssets/Editor/MomentumSceneBuilder.cs).
//      This is required for Unity to treat it as editor-only code.
//   2. Make sure PlayerMovementController.cs, CameraLook.cs,
//      GrappleHookController.cs, MomentumMeleeAttack.cs, IDamageable.cs,
//      and TestDummyEnemy.cs are already in your project (NOT in an
//      Editor folder — they're runtime scripts).
//   3. In Unity: menu bar > Momentum > Build Gray-Box Test Level.
//
// WHAT IT BUILDS:
//   - Ground plane (Ground layer)
//   - A wall-run corridor: two parallel walls spaced for wall-running (Wall layer)
//   - A Player rig: CharacterController + camera + all movement/attack
//     scripts, with every Inspector reference already wired up
//   - Two TestDummyEnemy cubes (Hittable layer) to try the attack on
//   - Four simple colored Materials, saved to Assets/MomentumAssets/Materials

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public static class MomentumSceneBuilder
{
    private const string RootFolder = "Assets/MomentumAssets";
    private const string MaterialsFolder = "Assets/MomentumAssets/Materials";

    [MenuItem("Momentum/Build Gray-Box Test Level")]
    public static void BuildTestLevel()
    {
        EnsureLayer("Ground");
        EnsureLayer("Wall");
        EnsureLayer("Hittable");
        EnsureLayer("Grappleable");

        EnsureFolder(RootFolder);
        EnsureFolder(MaterialsFolder);

        Material groundMat = CreateOrLoadMaterial("Ground_Gray", new Color(0.55f, 0.55f, 0.55f));
        Material wallMat = CreateOrLoadMaterial("Wall_Blue", new Color(0.30f, 0.50f, 0.80f));
        Material enemyMat = CreateOrLoadMaterial("Enemy_Red", new Color(0.80f, 0.20f, 0.20f));
        Material playerMat = CreateOrLoadMaterial("Player_Green", new Color(0.20f, 0.80f, 0.30f));

        BuildGround(groundMat);
        BuildWallRunCorridor(wallMat);
        BuildPlayer(playerMat);
        BuildEnemies(enemyMat);

        Debug.Log("Momentum gray-box test level built. Press Play and try wall-running through the corridor.");
    }

    // ---------------- Layers ----------------

    private static void EnsureLayer(string layerName)
    {
        Object tagManagerAsset = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0];
        SerializedObject tagManager = new SerializedObject(tagManagerAsset);
        SerializedProperty layersProp = tagManager.FindProperty("layers");

        for (int i = 0; i < layersProp.arraySize; i++)
        {
            SerializedProperty existing = layersProp.GetArrayElementAtIndex(i);
            if (existing.stringValue == layerName)
            {
                return; // already exists
            }
        }

        for (int i = 8; i < layersProp.arraySize; i++) // slots 0-7 are built-in/reserved
        {
            SerializedProperty slot = layersProp.GetArrayElementAtIndex(i);
            if (string.IsNullOrEmpty(slot.stringValue))
            {
                slot.stringValue = layerName;
                tagManager.ApplyModifiedProperties();
                return;
            }
        }

        Debug.LogWarning($"MomentumSceneBuilder: no free layer slot for '{layerName}'. " +
                          "Add it manually in Edit > Project Settings > Tags and Layers.");
    }

    // ---------------- Materials ----------------

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
        {
            return;
        }

        string parent = System.IO.Path.GetDirectoryName(path).Replace("\\", "/");
        string folderName = System.IO.Path.GetFileName(path);
        AssetDatabase.CreateFolder(parent, folderName);
    }

    private static Material CreateOrLoadMaterial(string name, Color color)
    {
        string path = $"{MaterialsFolder}/{name}.mat";
        Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null)
        {
            return existing;
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        if (shader == null) shader = Shader.Find("Diffuse");

        Material mat = new Material(shader);
        if (mat.HasProperty("_BaseColor"))
        {
            mat.SetColor("_BaseColor", color); // URP/Lit
        }
        if (mat.HasProperty("_Color"))
        {
            mat.SetColor("_Color", color); // Built-in Standard / Diffuse
        }

        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    // ---------------- Level geometry ----------------

    private static void BuildGround(Material mat)
    {
        GameObject ground = FindOrCreatePrimitive("Ground", PrimitiveType.Cube);
        ground.transform.position = new Vector3(0f, -0.5f, 20f);
        ground.transform.localScale = new Vector3(40f, 1f, 60f);
        ground.layer = LayerMask.NameToLayer("Ground");
        ground.GetComponent<Renderer>().sharedMaterial = mat;
    }

    private static void BuildWallRunCorridor(Material mat)
    {
        CreateWall("WallRun_Left", new Vector3(-1.75f, 2.5f, 20f), new Vector3(0.5f, 5f, 20f), mat);
        CreateWall("WallRun_Right", new Vector3(1.75f, 2.5f, 20f), new Vector3(0.5f, 5f, 20f), mat);
    }

    private static void CreateWall(string name, Vector3 position, Vector3 scale, Material mat)
    {
        GameObject wall = FindOrCreatePrimitive(name, PrimitiveType.Cube);
        wall.transform.position = position;
        wall.transform.localScale = scale;
        wall.layer = LayerMask.NameToLayer("Wall");
        wall.GetComponent<Renderer>().sharedMaterial = mat;
    }

    private static GameObject FindOrCreatePrimitive(string name, PrimitiveType type)
    {
        GameObject existing = GameObject.Find(name);
        if (existing != null)
        {
            Object.DestroyImmediate(existing);
        }
        GameObject obj = GameObject.CreatePrimitive(type);
        obj.name = name;
        return obj;
    }

    // ---------------- Player ----------------

    private static void BuildPlayer(Material mat)
    {
        GameObject existingPlayer = GameObject.Find("Player");
        if (existingPlayer != null)
        {
            Object.DestroyImmediate(existingPlayer);
        }

        GameObject player = new GameObject("Player");
        player.transform.position = new Vector3(0f, 0.05f, 5f); // feet near ground, before the corridor

        CharacterController controller = player.AddComponent<CharacterController>();
        controller.height = 2f;
        controller.radius = 0.4f;
        controller.center = new Vector3(0f, 1f, 0f);

        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        visual.name = "Visual_Capsule";
        visual.transform.SetParent(player.transform);
        visual.transform.localPosition = new Vector3(0f, 1f, 0f);
        Object.DestroyImmediate(visual.GetComponent<CapsuleCollider>()); // CharacterController already handles collision
        visual.GetComponent<Renderer>().sharedMaterial = mat;

        GameObject cameraObj = new GameObject("PlayerCamera");
        cameraObj.transform.SetParent(player.transform);
        cameraObj.transform.localPosition = new Vector3(0f, 1.6f, 0f);
        cameraObj.AddComponent<Camera>();
        cameraObj.AddComponent<AudioListener>();
        CameraLook look = cameraObj.AddComponent<CameraLook>();

        PlayerMovementController movement = player.AddComponent<PlayerMovementController>();
        GrappleHookController grapple = player.AddComponent<GrappleHookController>();
        MomentumMeleeAttack attack = player.AddComponent<MomentumMeleeAttack>();

        AssignObjectReference(look, "playerBody", player.transform);
        AssignObjectReference(movement, "playerCamera", cameraObj.transform);
        AssignObjectReference(grapple, "playerCamera", cameraObj.transform);
        AssignObjectReference(attack, "playerCamera", cameraObj.transform);

        AssignLayerMask(movement, "groundLayer", LayerMask.NameToLayer("Ground"));
        AssignLayerMask(movement, "wallLayer", LayerMask.NameToLayer("Wall"));
        AssignLayerMask(grapple, "grappleableLayer", LayerMask.NameToLayer("Wall"));
        AssignLayerMask(attack, "hittableLayer", LayerMask.NameToLayer("Hittable"));
    }

    private static void AssignObjectReference(Object component, string fieldName, Object value)
    {
        SerializedObject so = new SerializedObject(component);
        SerializedProperty prop = so.FindProperty(fieldName);
        if (prop == null)
        {
            Debug.LogWarning($"MomentumSceneBuilder: field '{fieldName}' not found on {component.GetType().Name}.");
            return;
        }
        prop.objectReferenceValue = value;
        so.ApplyModifiedProperties();
    }

    private static void AssignLayerMask(Object component, string fieldName, int layerIndex)
    {
        SerializedObject so = new SerializedObject(component);
        SerializedProperty prop = so.FindProperty(fieldName);
        if (prop == null)
        {
            Debug.LogWarning($"MomentumSceneBuilder: field '{fieldName}' not found on {component.GetType().Name}.");
            return;
        }
        prop.intValue = 1 << layerIndex;
        so.ApplyModifiedProperties();
    }

    // ---------------- Enemies ----------------

    private static void BuildEnemies(Material mat)
    {
        CreateEnemy("TestEnemy_1", new Vector3(3f, 0.5f, 35f), mat);
        CreateEnemy("TestEnemy_2", new Vector3(-3f, 0.5f, 38f), mat);
    }

    private static void CreateEnemy(string name, Vector3 position, Material mat)
    {
        GameObject enemy = FindOrCreatePrimitive(name, PrimitiveType.Cube);
        enemy.transform.position = position;
        enemy.layer = LayerMask.NameToLayer("Hittable");
        enemy.GetComponent<Renderer>().sharedMaterial = mat;
        enemy.AddComponent<TestDummyEnemy>();
    }
}
#endif
