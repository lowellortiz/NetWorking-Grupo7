using System.Collections.Generic;
using System.IO;
using System.Linq;
using NetworkingLab.Player;
using NetworkingLab.PowerUp;
using NetworkingLab.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NetworkingLab.Editor
{
    public static class LocalBaselineSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/LocalGameplayBaseline.unity";
        private const string InputPath = "Assets/Input/LocalPlayers.inputactions";

        [MenuItem("Networking Lab/Build Local Baseline")]
        public static void Build()
        {
            InputActionAsset inputAsset = BuildInputActions();
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            BuildEnvironment();
            BuildPlayers(inputAsset);
            BuildUi();

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("LOCAL_BASELINE_BUILT: " + ScenePath);
        }

        private static InputActionAsset BuildInputActions()
        {
            AssetDatabase.DeleteAsset(InputPath);
            InputActionAsset asset = ScriptableObject.CreateInstance<InputActionAsset>();
            AddPlayerMap(asset, "Player 1", "<Keyboard>/w", "<Keyboard>/s", "<Keyboard>/a", "<Keyboard>/d", "<Keyboard>/space");
            AddPlayerMap(asset, "Player 2", "<Keyboard>/upArrow", "<Keyboard>/downArrow", "<Keyboard>/leftArrow", "<Keyboard>/rightArrow", "<Keyboard>/rightCtrl");
            AddPlayerMap(asset, "Player 3", "<Keyboard>/i", "<Keyboard>/k", "<Keyboard>/j", "<Keyboard>/l", "<Keyboard>/o");
            AddPlayerMap(asset, "Player 4", "<Keyboard>/numpad8", "<Keyboard>/numpad5", "<Keyboard>/numpad4", "<Keyboard>/numpad6", "<Keyboard>/numpad0");
            File.WriteAllText(InputPath, asset.ToJson());
            Object.DestroyImmediate(asset);
            AssetDatabase.ImportAsset(InputPath, ImportAssetOptions.ForceSynchronousImport);
            return AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputPath);
        }

        private static void AddPlayerMap(InputActionAsset asset, string name, string up, string down, string left, string right, string powerUp)
        {
            InputActionMap map = asset.AddActionMap(name);
            InputAction move = map.AddAction("Move", InputActionType.Value);
            move.expectedControlType = "Vector2";
            move.AddCompositeBinding("2DVector")
                .With("Up", up)
                .With("Down", down)
                .With("Left", left)
                .With("Right", right);
            map.AddAction("PowerUp", InputActionType.Button).AddBinding(powerUp);
        }

        private static void BuildEnvironment()
        {
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.localScale = new Vector3(2.4f, 1f, 1.8f);
            ground.GetComponent<Renderer>().sharedMaterial = GetOrCreateMaterial("Ground", new Color(0.16f, 0.22f, 0.28f));

            GameObject lightObject = new("Directional Light");
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.4f;
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            GameObject cameraObject = new("Main Camera");
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 10.5f;
            camera.backgroundColor = new Color(0.07f, 0.09f, 0.12f);
            cameraObject.transform.SetPositionAndRotation(new Vector3(0f, 16f, -12f), Quaternion.Euler(52f, 0f, 0f));
        }

        private static void BuildPlayers(InputActionAsset inputAsset)
        {
            Vector3[] positions =
            {
                new(-5f, 0.75f, 3f), new(5f, 0.75f, 3f),
                new(-5f, 0.75f, -3f), new(5f, 0.75f, -3f)
            };
            Color[] colors = { new(0.1f, 0.55f, 1f), new(1f, 0.25f, 0.2f), new(0.25f, 0.85f, 0.35f), new(1f, 0.72f, 0.12f) };

            for (int index = 0; index < 4; index++)
            {
                string playerName = $"Player {index + 1}";
                GameObject player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                player.name = playerName;
                player.transform.position = positions[index];
                player.GetComponent<Renderer>().sharedMaterial = GetOrCreateMaterial(playerName.Replace(" ", string.Empty), colors[index]);

                PlayerInputSource input = player.AddComponent<PlayerInputSource>();
                input.Configure(inputAsset, playerName);
                player.AddComponent<PlayerController>().Configure(input, 5f, new Vector2(10.5f, 7.5f));
                player.AddComponent<PlayerPowerUp>().Configure(playerName, input);

                GameObject labelObject = new("Label");
                labelObject.transform.SetParent(player.transform, false);
                labelObject.transform.localPosition = new Vector3(0f, 1.5f, 0f);
                labelObject.transform.localRotation = Quaternion.Euler(52f, 0f, 0f);
                TextMesh label = labelObject.AddComponent<TextMesh>();
                label.text = playerName;
                label.anchor = TextAnchor.MiddleCenter;
                label.alignment = TextAlignment.Center;
                label.fontSize = 42;
                label.characterSize = 0.08f;
                label.color = Color.white;
            }
        }

        private static void BuildUi()
        {
            GameObject canvasObject = new("UI");
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            canvasObject.AddComponent<GraphicRaycaster>();

            Text title = CreateText(canvasObject.transform, "Title", "LOCAL GAMEPLAY BASELINE", 34, TextAnchor.UpperCenter);
            SetRect(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-500f, -80f), new Vector2(500f, -20f));

            string controls = "P1  WASD + Space     P2  Arrows + Right Ctrl     P3  IJKL + O     P4  Numpad 8456 + Numpad 0";
            Text help = CreateText(canvasObject.transform, "Controls", controls, 23, TextAnchor.LowerCenter);
            SetRect(help.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-850f, 20f), new Vector2(850f, 75f));

            Text message = CreateText(canvasObject.transform, "PowerUpMessage", string.Empty, 40, TextAnchor.MiddleCenter);
            message.color = new Color(1f, 0.9f, 0.2f);
            SetRect(message.rectTransform, new Vector2(0.5f, 0.82f), new Vector2(0.5f, 0.82f), new Vector2(-600f, -45f), new Vector2(600f, 45f));
            canvasObject.AddComponent<PowerUpMessageUI>().Configure(message, 2f);

            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        }

        private static Text CreateText(Transform parent, string name, string value, int fontSize, TextAnchor alignment)
        {
            GameObject textObject = new(name, typeof(RectTransform));
            textObject.transform.SetParent(parent, false);
            Text text = textObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = value;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = Color.white;
            return text;
        }

        private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        private static Material GetOrCreateMaterial(string name, Color color)
        {
            string path = $"Assets/Materials/{name}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                material = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }
            material.color = color;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void AddSceneToBuildSettings()
        {
            List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes
                .Where(existing => existing.path != ScenePath)
                .ToList();
            scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
