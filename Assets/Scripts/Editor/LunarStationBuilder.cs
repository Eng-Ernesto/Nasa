using System.Collections.Generic;
using System.IO;
using System.Text;
using LunarEyes.Core;
using LunarEyes.Interaction;
using LunarEyes.Player;
using LunarEyes.UI;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace LunarEyes.EditorTools
{
    /// <summary>
    /// Gera a cena core "LunarStation": terreno lunar, estação (Scifi Kit), Terra, Sol, lander,
    /// astronauta jogável, porta e terminal. Menu: Lunar Eyes > Build LunarStation Scene.
    /// Pode ser executado de novo: recria a cena do zero.
    /// </summary>
    public static class LunarStationBuilder
    {
        const string ScenePath = "Assets/Scenes/LunarStation.unity";
        const string KitFolder = "Assets/ThirdParty/Creepy_Cat/3D Scifi Kit Starter Kit_HD/";
        const string KitRoot = KitFolder + "Prefabs/";
        const string FontRegular = "Assets/UI/Fonts/JetBrainsMono-Regular.ttf";
        const string FontBold = "Assets/UI/Fonts/JetBrainsMono-Bold.ttf";

        // Paleta do ecrã (alinhada ao design system: fundo profundo, acento ciano, alerta âmbar)
        static readonly Color ScreenBg = new Color(0.02f, 0.05f, 0.08f, 0.96f);
        static readonly Color Accent = new Color(0.55f, 0.85f, 1f);
        static readonly Color Alert = new Color(1f, 0.74f, 0.3f);
        static readonly Color TextMain = new Color(0.9f, 0.94f, 0.97f);
        static readonly Color TextMuted = new Color(0.55f, 0.65f, 0.72f);
        const string AstronautFbx = "Assets/Models/Characters/Astronaut/Astronaut.fbx";
        const string AstronautV2Fbx = "Assets/Models/Characters/AstronautV2/AstronautV2.fbx";
        const string MixamoFolder = "Assets/Animations/Mixamo";
        const string HumanoidControllerPath = "Assets/Animations/Player/AstronautV2.controller";
        static readonly string[] MixamoClips = { "Idle", "Walk", "Run", "OpenDoor", "Interact" };
        const string ControllerPath = "Assets/Animations/Player/Astronaut.controller";
        const string IdleClipPath = "Assets/Animations/Player/Idle.anim";

        static readonly StringBuilder Log = new StringBuilder();

        [MenuItem("Lunar Eyes/Build LunarStation Scene")]
        public static void BuildMenu() => Debug.Log(Build());

        public static string Build()
        {
            Log.Clear();
            ConfigureAstronautImport();
            ConfigureModelMaterials();
            bool useV2 = ConfigureAstronautV2();
            var controller = useV2 ? BuildHumanoidAnimator() : BuildAnimator();
            Log.AppendLine(useV2 ? "Personagem: AstronautV2 (Humanoid + Mixamo)." : "Personagem: Astronaut (Generic). Faltam clips Mixamo em " + MixamoFolder);
            var regolith = CreateRegolithTexture();
            var sky = CreateSkybox();
            var sunMat = CreateSunMaterial();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            ConfigureEnvironment(sky);
            var sunLight = BuildSun(sunMat);
            BuildTerrain(regolith);
            BuildEarth();
            BuildLander();
            var station = BuildStation(out var door, out var terminal);
            var cam = BuildCamera();
            var player = BuildPlayer(controller, cam, useV2);
            BuildHUD();
            BuildPostProcessing();

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddToBuildSettings(ScenePath);
            AssetDatabase.SaveAssets();

            Log.AppendLine("Cena guardada em " + ScenePath);
            return Log.ToString();
        }

        // ---------------------------------------------------------------- Astronauta

        static void ConfigureAstronautImport()
        {
            var imp = (ModelImporter)AssetImporter.GetAtPath(AstronautFbx);
            var rename = new Dictionary<string, string>
            {
                { "walk", "Walk" },
                { "run", "Run" },
                { "cria uma animação de interagir com objetos", "Interact" },
                { "animação de sentar e apertar o botão", "SitPress" },
                { "animação de abrir a porta", "OpenDoor" },
            };

            var clips = imp.defaultClipAnimations;
            foreach (var c in clips)
            {
                if (rename.TryGetValue(c.takeName, out var n)) c.name = n;
                c.loopTime = c.name == "Walk" || c.name == "Run";
                c.lockRootRotation = true;
                c.lockRootHeightY = true;
                c.lockRootPositionXZ = true;
            }
            imp.animationType = ModelImporterAnimationType.Generic;
            imp.clipAnimations = clips;
            imp.SaveAndReimport();
            Log.AppendLine("Astronauta: " + clips.Length + " clips configurados.");
        }

        /// <summary>
        /// Cria materiais URP/Lit com as texturas das pastas .fbm e remapeia-os nos FBX
        /// (o material embutido vem sem textura).
        /// </summary>
        static void ConfigureModelMaterials()
        {
            var models = new (string fbx, string matFolder, string name, float metallic, float smooth)[]
            {
                (AstronautFbx, "Assets/Materials/Characters", "Astronaut", 0.3f, 0.45f),
                ("Assets/Models/Props/LunarLander/LunarLander.fbx", "Assets/Materials/Props", "LunarLander", 0.5f, 0.5f),
                ("Assets/Models/Environment/EarthSun/Earth.fbx", "Assets/Materials/Environment", "Earth", 0f, 0.35f),
                ("Assets/Models/Environment/Moon/Moon.fbx", "Assets/Materials/Environment", "Moon", 0f, 0.05f),
                ("Assets/Models/Station/SpaceCockpit.fbx", "Assets/Materials/Station", "SpaceCockpit", 0.4f, 0.5f),
            };

            foreach (var m in models)
            {
                string fbm = Path.ChangeExtension(m.fbx, ".fbm");
                Texture2D baseTex = null, normalTex = null;
                foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { fbm }))
                {
                    string p = AssetDatabase.GUIDToAssetPath(guid);
                    string lower = Path.GetFileNameWithoutExtension(p).ToLowerInvariant();
                    if (lower.Contains("normal"))
                    {
                        var ti = (TextureImporter)AssetImporter.GetAtPath(p);
                        if (ti.textureType != TextureImporterType.NormalMap) { ti.textureType = TextureImporterType.NormalMap; ti.SaveAndReimport(); }
                        normalTex = AssetDatabase.LoadAssetAtPath<Texture2D>(p);
                    }
                    else if (lower.Contains("basecolor") || lower.Contains("base_color") || lower.Contains("albedo"))
                        baseTex = AssetDatabase.LoadAssetAtPath<Texture2D>(p);
                }

                string matPath = $"{m.matFolder}/{m.name}.mat";
                var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                if (mat == null)
                {
                    mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    AssetDatabase.CreateAsset(mat, matPath);
                }
                mat.SetTexture("_BaseMap", baseTex);
                mat.SetColor("_BaseColor", Color.white);
                mat.SetFloat("_Metallic", m.metallic);
                mat.SetFloat("_Smoothness", m.smooth);
                if (normalTex != null)
                {
                    mat.SetTexture("_BumpMap", normalTex);
                    mat.EnableKeyword("_NORMALMAP");
                }
                EditorUtility.SetDirty(mat);

                var imp = (ModelImporter)AssetImporter.GetAtPath(m.fbx);
                foreach (var o in AssetDatabase.LoadAllAssetRepresentationsAtPath(m.fbx))
                    if (o is Material embedded)
                        imp.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), embedded.name), mat);
                imp.SaveAndReimport();
                Log.AppendLine($"Material {m.name}: base={(baseTex ? baseTex.name : "—")} normal={(normalTex ? normalTex.name : "—")}");
            }
        }

        static AnimationClip LoadClip(string name)
        {
            foreach (var a in AssetDatabase.LoadAllAssetsAtPath(AstronautFbx))
                if (a is AnimationClip c && c.name == name) return c;
            Log.AppendLine("AVISO: clip não encontrado: " + name);
            return null;
        }

        /// <summary>Pose parada a partir do primeiro frame do clip "Interact" (o FBX não traz idle).</summary>
        static AnimationClip BuildIdleClip()
        {
            var source = LoadClip("Interact") ?? LoadClip("Walk");
            var idle = new AnimationClip { name = "Idle" };
            foreach (var b in AnimationUtility.GetCurveBindings(source))
            {
                var curve = AnimationUtility.GetEditorCurve(source, b);
                float v = curve.Evaluate(0f);
                AnimationUtility.SetEditorCurve(idle, b, AnimationCurve.Constant(0f, 1f, v));
            }
            var settings = AnimationUtility.GetAnimationClipSettings(idle);
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(idle, settings);

            AssetDatabase.DeleteAsset(IdleClipPath);
            AssetDatabase.CreateAsset(idle, IdleClipPath);
            return idle;
        }

        static AnimatorController BuildAnimator()
        {
            var idle = BuildIdleClip();
            AssetDatabase.DeleteAsset(ControllerPath);
            var ac = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            ac.AddParameter("Speed", AnimatorControllerParameterType.Float);

            var sm = ac.layers[0].stateMachine;
            var loco = ac.CreateBlendTreeInController("Locomotion", out BlendTree tree, 0);
            tree.blendType = BlendTreeType.Simple1D;
            tree.blendParameter = "Speed";
            tree.useAutomaticThresholds = false;
            tree.AddChild(idle, 0f);
            tree.AddChild(LoadClip("Walk"), 2.2f);
            tree.AddChild(LoadClip("Run"), 4.5f);
            sm.defaultState = loco;

            foreach (var name in new[] { "OpenDoor", "Interact", "SitPress" })
            {
                var st = sm.AddState(name);
                st.motion = LoadClip(name);
            }
            EditorUtility.SetDirty(ac);
            Log.AppendLine("Animator criado: Locomotion (Idle/Walk/Run) + OpenDoor, Interact, SitPress.");
            return ac;
        }

        // ---------------------------------------------------------------- Ambiente

        static Texture2D CreateRegolithTexture()
        {
            const string path = "Assets/Textures/Environment/Regolith_Generated.png";
            const int size = 512;
            var tex = new Texture2D(size, size, TextureFormat.RGB24, true);
            var rng = new System.Random(7);
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float u = (float)x / size, v = (float)y / size;
                float n = 0f, amp = 0.5f, freq = 4f;
                for (int o = 0; o < 5; o++)
                {
                    // Perlin periódico (tileable) via soma de 4 amostras com pesos
                    float a = Mathf.PerlinNoise(u * freq, v * freq);
                    float b = Mathf.PerlinNoise((u - 1f) * freq, v * freq);
                    float c = Mathf.PerlinNoise(u * freq, (v - 1f) * freq);
                    float d = Mathf.PerlinNoise((u - 1f) * freq, (v - 1f) * freq);
                    float p = Mathf.Lerp(Mathf.Lerp(a, b, u), Mathf.Lerp(c, d, u), v);
                    n += p * amp; amp *= 0.5f; freq *= 2f;
                }
                float speck = rng.NextDouble() < 0.02 ? (float)rng.NextDouble() * 0.12f : 0f;
                float g = Mathf.Clamp01(0.30f + n * 0.30f + speck);
                tex.SetPixel(x, y, new Color(g, g, g * 0.98f));
            }
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            AssetDatabase.ImportAsset(path);
            var ti = (TextureImporter)AssetImporter.GetAtPath(path);
            ti.wrapMode = TextureWrapMode.Repeat;
            ti.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        static Material CreateSkybox()
        {
            const string path = "Assets/Materials/Environment/Skybox_Stars.mat";
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Environment/Stars_MilkyWay_8k.jpg");
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Skybox/Panoramic"));
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.SetTexture("_MainTex", tex);
            mat.SetFloat("_Exposure", 0.55f);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static Material CreateSunMaterial()
        {
            const string path = "Assets/Materials/Environment/Sun_Unlit.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                AssetDatabase.CreateAsset(mat, path);
            }
            var tex = FindTexture("Assets/Models/Environment/EarthSun/Sun.fbm");
            if (tex != null) mat.SetTexture("_BaseMap", tex);
            mat.SetColor("_BaseColor", new Color(1.6f, 1.45f, 1.2f));
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static Texture2D FindTexture(string folder)
        {
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { folder }))
                return AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(guid));
            return null;
        }

        static void ConfigureEnvironment(Material sky)
        {
            RenderSettings.skybox = sky;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.07f, 0.07f, 0.08f);
            RenderSettings.fog = false;
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox;
        }

        static Light BuildSun(Material sunMat)
        {
            var sunPos = new Vector3(-900f, 330f, -700f);

            var lightGo = new GameObject("Sun Light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 2.6f;
            light.color = new Color(1f, 0.97f, 0.92f);
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 1f;
            lightGo.transform.rotation = Quaternion.LookRotation(-sunPos.normalized);
            RenderSettings.sun = light;

            var sun = InstantiateModel("Assets/Models/Environment/EarthSun/Sun.fbx", "Sun");
            sun.transform.position = sunPos;
            sun.transform.localScale = Vector3.one * 45f;
            foreach (var r in sun.GetComponentsInChildren<Renderer>())
            {
                r.sharedMaterial = sunMat;
                r.shadowCastingMode = ShadowCastingMode.Off;
            }
            return light;
        }

        static void BuildTerrain(Texture2D regolith)
        {
            const string dataPath = "Assets/Prefabs/Environment/LunarTerrain.asset";
            const string layerPath = "Assets/Materials/Environment/Regolith.terrainlayer";
            const string matPath = "Assets/Materials/Environment/Terrain_URP.mat";
            const int res = 257;
            var size = new Vector3(400f, 40f, 400f);
            var origin = new Vector3(-200f, -4f, -180f);

            var layer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(layerPath);
            if (layer == null) { layer = new TerrainLayer(); AssetDatabase.CreateAsset(layer, layerPath); }
            layer.diffuseTexture = regolith;
            layer.tileSize = new Vector2(8f, 8f);
            layer.smoothness = 0.05f;
            EditorUtility.SetDirty(layer);

            AssetDatabase.DeleteAsset(dataPath);
            var data = new TerrainData { heightmapResolution = res, size = size };
            data.terrainLayers = new[] { layer };

            var rng = new System.Random(42);
            var craters = new List<Vector4>(); // x, z, radius, depth
            for (int i = 0; i < 40; i++)
            {
                float r = Mathf.Lerp(3f, 28f, Mathf.Pow((float)rng.NextDouble(), 2f));
                craters.Add(new Vector4(
                    origin.x + (float)rng.NextDouble() * size.x,
                    origin.z + (float)rng.NextDouble() * size.z,
                    r, r * 0.22f));
            }

            var h = new float[res, res];
            float flatWorldY = -0.03f;
            for (int zi = 0; zi < res; zi++)
            for (int xi = 0; xi < res; xi++)
            {
                float wx = origin.x + xi / (float)(res - 1) * size.x;
                float wz = origin.z + zi / (float)(res - 1) * size.z;

                float y = 0f;
                y += (Mathf.PerlinNoise(wx * 0.008f + 11f, wz * 0.008f + 3f) - 0.5f) * 6f;
                y += (Mathf.PerlinNoise(wx * 0.03f + 5f, wz * 0.03f + 9f) - 0.5f) * 1.5f;
                foreach (var c in craters)
                {
                    float d = Vector2.Distance(new Vector2(wx, wz), new Vector2(c.x, c.y)) / c.z;
                    if (d < 1f) y -= c.w * (1f - d * d);
                    else if (d < 1.35f) y += c.w * 0.35f * Mathf.Sin((d - 1f) / 0.35f * Mathf.PI);
                }

                // Zona plana à volta da estação e do caminho de chegada
                float dx = Mathf.Max(0f, Mathf.Abs(wx) - 18f);
                float dz = Mathf.Max(0f, Mathf.Max(-22f - wz, wz - 40f));
                float dist = Mathf.Sqrt(dx * dx + dz * dz);
                float flat = 1f - Mathf.SmoothStep(0f, 1f, dist / 20f);
                y = Mathf.Lerp(y, flatWorldY, flat);

                h[zi, xi] = Mathf.Clamp01((y - origin.y) / size.y);
            }
            data.SetHeights(0, 0, h);
            AssetDatabase.CreateAsset(data, dataPath);

            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Terrain/Lit"));
                AssetDatabase.CreateAsset(mat, matPath);
            }

            var go = Terrain.CreateTerrainGameObject(data);
            go.name = "Lunar Terrain";
            go.transform.position = origin;
            var terrain = go.GetComponent<Terrain>();
            terrain.materialTemplate = mat;
            terrain.drawInstanced = true;
            Log.AppendLine("Terreno 400x400 m com " + craters.Count + " crateras.");
        }

        static void BuildEarth()
        {
            var earth = InstantiateModel("Assets/Models/Environment/EarthSun/Earth.fbx", "Earth");
            // Baixa no céu a leste: enquadrada pela janela (parede este) e visível do exterior
            earth.transform.position = new Vector3(700f, 95f, 60f);
            earth.transform.localScale = Vector3.one * 90f;
            earth.transform.rotation = Quaternion.Euler(0f, 0f, 23.4f);
            earth.AddComponent<Rotator>();
            foreach (var r in earth.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = ShadowCastingMode.Off;
        }

        static void BuildLander()
        {
            var lander = InstantiateModel("Assets/Models/Props/LunarLander/LunarLander.fbx", "Lunar Lander");
            lander.transform.localScale = Vector3.one * 6.5f;
            lander.transform.rotation = Quaternion.Euler(0f, -35f, 0f);
            lander.transform.position = new Vector3(15f, 0f, 2f);
            SnapToGround(lander, 0f);
            var b = GetBounds(lander);
            var col = lander.AddComponent<BoxCollider>();
            col.center = lander.transform.InverseTransformPoint(b.center);
            col.size = new Vector3(b.size.x / 6.5f * 0.7f, b.size.y / 6.5f, b.size.z / 6.5f * 0.7f);
        }

        // ---------------------------------------------------------------- Estação

        static GameObject BuildStation(out StationDoor door, out MonitorTerminal terminal)
        {
            var root = new GameObject("Station").transform;
            var shell = new GameObject("Shell").transform; shell.SetParent(root);
            var props = new GameObject("Props").transform; props.SetParent(root);

            string floor = KitRoot + "Floors/Floor_Squared_01_6x6.prefab";
            string roof = KitRoot + "Roofs/Roof_Squared_01_6x6.prefab";
            string wall = KitRoot + "Walls/Wall_Cube_01_Long.prefab";

            // Interior: x -6..6, z 20..32, altura 4
            for (int i = 0; i < 2; i++)
            for (int j = 0; j < 2; j++)
            {
                Place(floor, shell, 0f, new Vector3(-6f + i * 6f, 0f, 20f + j * 6f));
                var r = Place(roof, shell, 0f, new Vector3(-6f + i * 6f, 4f, 20f + j * 6f));
                foreach (var rr in r.GetComponentsInChildren<Renderer>()) rr.shadowCastingMode = ShadowCastingMode.TwoSided;
            }

            // Cobertura exterior (o teto do kit só tem face interior e deixa passar o sol)
            var cap = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cap.name = "Roof Cap";
            cap.transform.SetParent(shell);
            cap.transform.position = new Vector3(0f, 4.6f, 26f);
            cap.transform.localScale = new Vector3(16.2f, 0.25f, 16.2f);
            var wallRenderer = LoadPrefab(wall).GetComponentInChildren<Renderer>();
            if (wallRenderer != null) cap.GetComponent<Renderer>().sharedMaterial = wallRenderer.sharedMaterial;

            // Parede oeste (x -8..-6), z 18..34
            Place(wall, shell, 0f, new Vector3(-8f, 0f, 18f), new Vector3(1f, 1f, 8f / 6f));
            Place(wall, shell, 0f, new Vector3(-8f, 0f, 26f), new Vector3(1f, 1f, 8f / 6f));
            // Parede este com janela (vista para a Terra)
            Place(wall, shell, 0f, new Vector3(6f, 0f, 18f), new Vector3(1f, 1f, 8f / 6f));
            Place(KitRoot + "Walls/Porthole_01_Long.prefab", shell, 0f, new Vector3(6f, 0f, 26f));
            Place(wall, shell, 0f, new Vector3(6f, 0f, 32f), new Vector3(1f, 1f, 2f / 6f));
            // Parede norte (z 32..34)
            Place(wall, shell, 90f, new Vector3(-6f, 0f, 32f));
            Place(wall, shell, 90f, new Vector3(0f, 0f, 32f));
            // Parede sul com a entrada (z 18..20)
            Place(wall, shell, 90f, new Vector3(-6f, 0f, 18f), new Vector3(1f, 1f, 3.5f / 6f));
            Place(wall, shell, 90f, new Vector3(2.5f, 0f, 18f), new Vector3(1f, 1f, 3.5f / 6f));
            var doorway = Place(KitRoot + "Doors/DoorWay_01_Large.prefab", shell, 0f, new Vector3(-2.5f, 0f, 18.94f));

            // Luzes do teto
            foreach (float z in new[] { 23f, 29f })
            {
                var fixture = (GameObject)PrefabUtility.InstantiatePrefab(LoadPrefab(KitRoot + "Lights/Light_Corridor_01.prefab"), props);
                fixture.transform.position = new Vector3(0f, 3.98f, z);
                var lgo = new GameObject("Point Light");
                lgo.transform.SetParent(fixture.transform, false);
                lgo.transform.localPosition = new Vector3(0f, -0.6f, 0f);
                var l = lgo.AddComponent<Light>();
                l.type = LightType.Point;
                l.range = 10f;
                l.intensity = 2.5f;
                l.color = new Color(0.85f, 0.92f, 1f);
                l.shadows = LightShadows.Soft;
            }

            var intercom = (GameObject)PrefabUtility.InstantiatePrefab(LoadPrefab(KitRoot + "Stuff/Intercom_01.prefab"), props);
            intercom.transform.SetPositionAndRotation(new Vector3(-5.95f, 1.6f, 22f), Quaternion.Euler(0f, 0f, 0f));

            // Poucos props para dar vida à sala (o kit orienta as peças de parede para -X a 0°)
            Place(KitRoot + "Walls/Wall_Bed_01.prefab", props, 180f, new Vector3(-6f, 0f, 20.3f));
            foreach (var (pos, rot) in new[] { (new Vector3(4.6f, 0f, 21.2f), 12f), (new Vector3(4.4f, 0f, 22.6f), -20f) })
            {
                var crate = (GameObject)PrefabUtility.InstantiatePrefab(LoadPrefab(KitRoot + "Props/Crate_01.prefab"), props);
                crate.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, rot, 0f));
                SnapToGround(crate, 0f);
            }
            AddMeshColliders(props.gameObject);

            door = BuildDoor(root, doorway);
            terminal = BuildTerminal(root, props);

            AddMeshColliders(shell.gameObject);
            Log.AppendLine("Porta (DoorWay) bounds: " + GetBounds(doorway).ToString("F2"));
            return root.gameObject;
        }

        static StationDoor BuildDoor(Transform root, GameObject doorway)
        {
            var go = new GameObject("Station Door");
            go.transform.SetParent(root);
            // Abertura medida por raycast no DoorWay_01_Large: x ≈ -2.4..0.7, y ≈ 0..3.1
            go.transform.position = new Vector3(-0.75f, 0f, 19.47f);

            var panel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            panel.name = "Door Panel";
            panel.transform.SetParent(go.transform, false);
            panel.transform.localPosition = new Vector3(0f, 1.62f, 0f);
            panel.transform.localScale = new Vector3(3.9f, 3.3f, 0.12f);
            var doorMesh = LoadPrefab(KitRoot + "Doors/Door_Left_01.prefab").GetComponentInChildren<Renderer>();
            if (doorMesh != null) panel.GetComponent<Renderer>().sharedMaterial = doorMesh.sharedMaterial;

            var stand = new GameObject("Stand Point").transform;
            stand.SetParent(go.transform, false);
            stand.localPosition = new Vector3(0f, 0f, -1.6f);

            var audio = go.AddComponent<AudioSource>();
            audio.clip = AssetDatabase.LoadAssetAtPath<AudioClip>(KitFolder + "Sounds/Pneumatic-door.wav");
            audio.playOnAwake = false;
            audio.spatialBlend = 1f;
            audio.minDistance = 2f;
            audio.maxDistance = 25f;
            audio.volume = 0.8f;

            var door = go.AddComponent<StationDoor>();
            SetRef(door, "sfx", audio);
            SetRef(door, "panel", panel.transform);
            SetRef(door, "standPoint", stand);
            SetFloat(door, "radius", 2.6f);
            var so = new SerializedObject(door);
            so.FindProperty("openOffset").vector3Value = new Vector3(4.0f, 0f, 0f);
            so.ApplyModifiedPropertiesWithoutUndo();
            return door;
        }

        static MonitorTerminal BuildTerminal(Transform root, Transform props)
        {

            // Mesa do terminal (space cockpit) em frente ao ecrã
            var desk = (GameObject)PrefabUtility.InstantiatePrefab(LoadPrefab("Assets/Models/Station/SpaceCockpit.fbx"), props);
            desk.name = "Terminal Desk";
            desk.transform.localScale = Vector3.one * 2.4f;
            desk.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            desk.transform.position = Vector3.zero;
            var db = GetBounds(desk);
            desk.transform.position = new Vector3(-db.center.x, -db.min.y, 30.45f - db.max.z);
            var cleaned = CockpitWithoutLever();
            if (cleaned != null) desk.GetComponentInChildren<MeshFilter>().sharedMesh = cleaned;
            db = GetBounds(desk);
            var deskCol = desk.AddComponent<BoxCollider>();
            deskCol.center = desk.transform.InverseTransformPoint(db.center);
            var ls = desk.transform.lossyScale;
            deskCol.size = new Vector3(db.size.x / ls.x, db.size.y / ls.y, db.size.z / ls.z);
            Log.AppendLine("Mesa bounds: " + GetBounds(desk).ToString("F2"));

            var go = new GameObject("Monitor Terminal");
            go.transform.SetParent(root);
            go.transform.position = new Vector3(0f, 0f, 30.4f);

            var stand = new GameObject("Stand Point").transform;
            stand.SetParent(go.transform, false);
            stand.position = new Vector3(0f, 0f, 28.75f);
            stand.rotation = Quaternion.identity;

            var screenPos = new Vector3(0f, 2.75f, 31.9f);

            // Moldura do monitor, fixa na parede norte (z = 32)
            var bezel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bezel.name = "Monitor Bezel";
            bezel.transform.SetParent(props);
            bezel.transform.position = screenPos + new Vector3(0f, 0f, 0.05f);
            bezel.transform.localScale = new Vector3(2.06f, 1.16f, 0.08f);
            var bezelMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Station/Monitor_Bezel.mat");
            if (bezelMat == null)
            {
                bezelMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(bezelMat, "Assets/Materials/Station/Monitor_Bezel.mat");
            }
            bezelMat.SetColor("_BaseColor", new Color(0.08f, 0.09f, 0.1f));
            bezelMat.SetFloat("_Metallic", 0.6f);
            bezelMat.SetFloat("_Smoothness", 0.55f);
            bezel.GetComponent<Renderer>().sharedMaterial = bezelMat;
            var camPoint = new GameObject("Camera Point").transform;
            camPoint.SetParent(go.transform, false);
            camPoint.position = new Vector3(0f, 2.72f, 30.3f);
            camPoint.rotation = Quaternion.LookRotation(screenPos - camPoint.position);

            // Ecrã (Canvas World Space)
            var canvasGo = new GameObject("Screen Canvas", typeof(Canvas), typeof(CanvasScaler));
            canvasGo.transform.SetParent(go.transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvasGo.GetComponent<CanvasScaler>().dynamicPixelsPerUnit = 4f;
            var rt = (RectTransform)canvasGo.transform;
            rt.sizeDelta = new Vector2(1900f, 1050f);
            rt.localScale = Vector3.one * 0.001f;
            rt.position = screenPos;

            var idle = BuildScreenIdle(rt);
            var active = BuildScreenActive(rt);
            Log.AppendLine("Ecrã do terminal em " + screenPos);

            var glow = new GameObject("Screen Light").AddComponent<Light>();
            glow.transform.SetParent(go.transform, false);
            glow.transform.position = screenPos + Vector3.back * 0.6f;
            glow.type = LightType.Point;
            glow.range = 3.5f;
            glow.intensity = 1.2f;
            glow.color = new Color(0.45f, 0.75f, 1f);

            var terminal = go.AddComponent<MonitorTerminal>();
            SetRef(terminal, "standPoint", stand);
            SetRef(terminal, "cameraPoint", camPoint);
            SetRef(terminal, "screenIdle", idle.gameObject);
            SetRef(terminal, "screenActive", active.gameObject);
            SetFloat(terminal, "radius", 2.2f);
            return terminal;
        }

        static RectTransform BuildScreenIdle(RectTransform canvas)
        {
            var root = Panel(canvas, "Screen Idle", ScreenBg);
            var header = Box(root, "Header", new Vector2(0f, 0.86f), new Vector2(1f, 1f), new Color(0.05f, 0.12f, 0.18f, 1f));
            LabelAt(header, "LUNAR EYES  ·  ESTAÇÃO DE OBSERVAÇÃO", 62, TextAnchor.MiddleLeft, Accent, Vector2.zero, Vector2.one, true);

            LabelAt(root, "SISTEMA EM ESPERA", 120, TextAnchor.MiddleCenter, TextMain, new Vector2(0f, 0.48f), new Vector2(1f, 0.72f), true);
            LabelAt(root, "Aproxime-se do terminal e carregue  [E]", 60, TextAnchor.MiddleCenter, TextMuted, new Vector2(0f, 0.32f), new Vector2(1f, 0.46f), false);

            var footer = Box(root, "Footer", new Vector2(0f, 0f), new Vector2(1f, 0.1f), new Color(0.04f, 0.09f, 0.13f, 1f));
            LabelAt(footer, "MISSÃO 01  ·  ANGOLA", 46, TextAnchor.MiddleLeft, TextMuted, Vector2.zero, Vector2.one, false);
            return root;
        }

        static RectTransform BuildScreenActive(RectTransform canvas)
        {
            var root = Panel(canvas, "Screen Active", ScreenBg);

            var header = Box(root, "Header", new Vector2(0f, 0.86f), new Vector2(1f, 1f), new Color(0.05f, 0.12f, 0.18f, 1f));
            var pill = Box(header, "Alert Pill", new Vector2(0.015f, 0.18f), new Vector2(0.2f, 0.82f), Alert);
            LabelAt(pill, "ALERTA", 56, TextAnchor.MiddleCenter, new Color(0.08f, 0.06f, 0.02f), Vector2.zero, Vector2.one, true);
            LabelAt(header, "MISSÃO 01  ·  ANGOLA", 56, TextAnchor.MiddleLeft, Accent, new Vector2(0.23f, 0f), Vector2.one, true);

            LabelAt(root, "Sul de Angola — possível seca 2018/19", 72, TextAnchor.MiddleLeft, TextMain, new Vector2(0f, 0.68f), new Vector2(1f, 0.84f), true);

            var rows = new[]
            {
                ("REGIÃO", "Cunene · Huíla · Namibe"),
                ("ESTADO", "Candidato em validação"),
                ("VARIÁVEIS", "Temperatura · Chuva · Solo · Vegetação"),
            };
            for (int i = 0; i < rows.Length; i++)
            {
                float top = 0.64f - i * 0.15f;
                var row = Box(root, "Row " + i, new Vector2(0.02f, top - 0.13f), new Vector2(0.98f, top), new Color(0.05f, 0.1f, 0.15f, 1f));
                LabelAt(row, rows[i].Item1, 46, TextAnchor.MiddleLeft, TextMuted, Vector2.zero, new Vector2(0.3f, 1f), true);
                LabelAt(row, rows[i].Item2, 50, TextAnchor.MiddleLeft, TextMain, new Vector2(0.28f, 0f), Vector2.one, false);
            }

            var footer = Box(root, "Footer", new Vector2(0f, 0f), new Vector2(1f, 0.1f), new Color(0.04f, 0.09f, 0.13f, 1f));
            LabelAt(footer, "FONTE: NASA POWER · GPM IMERG · SMAP · MODIS", 40, TextAnchor.MiddleLeft, TextMuted, Vector2.zero, new Vector2(0.75f, 1f), false);
            var badge = Box(footer, "Mock Badge", new Vector2(0.77f, 0.15f), new Vector2(0.985f, 0.85f), new Color(0f, 0f, 0f, 0f));
            badge.GetComponent<Image>().color = new Color(1f, 0.74f, 0.3f, 0.18f);
            LabelAt(badge, "DADOS: MOCK", 44, TextAnchor.MiddleCenter, Alert, Vector2.zero, Vector2.one, true);
            return root;
        }

        /// <summary>
        /// Copia a malha do space cockpit sem a peça solta do topo (a "alavanca" que flutuava):
        /// separa a malha em peças ligadas e remove as peças pequenas acima do corpo principal.
        /// </summary>
        static Mesh CockpitWithoutLever()
        {
            const string outPath = "Assets/Models/Station/SpaceCockpit_NoLever.asset";
            var srcGo = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Station/SpaceCockpit.fbx");
            var srcMf = srcGo != null ? srcGo.GetComponentInChildren<MeshFilter>() : null;
            var src = srcMf != null ? srcMf.sharedMesh : null;
            if (src == null) return null;

            var v = src.vertices; var t = src.triangles; int n = v.Length;
            var parent = new int[n];
            for (int i = 0; i < n; i++) parent[i] = i;
            int Find(int x) { while (parent[x] != x) { parent[x] = parent[parent[x]]; x = parent[x]; } return x; }
            void Union(int a, int c) { a = Find(a); c = Find(c); if (a != c) parent[a] = c; }
            var byPos = new Dictionary<Vector3Int, int>();
            for (int i = 0; i < n; i++)
            {
                var k = Vector3Int.RoundToInt(v[i] * 10000f);
                if (byPos.TryGetValue(k, out int o)) Union(i, o); else byPos[k] = i;
            }
            for (int i = 0; i < t.Length; i += 3) { Union(t[i], t[i + 1]); Union(t[i], t[i + 2]); }

            var minY = new Dictionary<int, float>(); var count = new Dictionary<int, int>();
            for (int i = 0; i < t.Length; i += 3)
            {
                int r = Find(t[i]);
                float y = Mathf.Min(v[t[i]].y, Mathf.Min(v[t[i + 1]].y, v[t[i + 2]].y));
                minY[r] = minY.TryGetValue(r, out float m) ? Mathf.Min(m, y) : y;
                count[r] = count.TryGetValue(r, out int c) ? c + 1 : 1;
            }
            int body = -1;
            foreach (var kv in count) if (body < 0 || kv.Value > count[body]) body = kv.Key;
            float top = src.bounds.max.y;

            var keep = new List<int>(t.Length);
            int removed = 0;
            for (int i = 0; i < t.Length; i += 3)
            {
                int r = Find(t[i]);
                if (r != body && minY[r] > top * 0.6f) { removed++; continue; }
                keep.Add(t[i]); keep.Add(t[i + 1]); keep.Add(t[i + 2]);
            }

            var mesh = Object.Instantiate(src);
            mesh.name = "SpaceCockpit_NoLever";
            mesh.subMeshCount = 1;
            mesh.SetTriangles(keep, 0);
            mesh.RecalculateBounds();
            AssetDatabase.DeleteAsset(outPath);
            AssetDatabase.CreateAsset(mesh, outPath);
            Log.AppendLine("Cockpit: removidos " + removed + " triângulos da alavanca.");
            return mesh;
        }

        // ---------------------------------------------------------------- Personagem V2 (Humanoid + Mixamo)

        /// <summary>Configura o AstronautV2 (Humanoid) e os FBX do Mixamo. Devolve false se faltar algum clip.</summary>
        static bool ConfigureAstronautV2()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(AstronautV2Fbx) == null) return false;

            var imp = (ModelImporter)AssetImporter.GetAtPath(AstronautV2Fbx);
            imp.animationType = ModelImporterAnimationType.Human;
            imp.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;

            // Material URP com a textura da pasta .fbm
            const string matPath = "Assets/Materials/Characters/AstronautV2.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null) { mat = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(mat, matPath); }
            mat.SetTexture("_BaseMap", FindTexture("Assets/Models/Characters/AstronautV2/AstronautV2.fbm"));
            mat.SetFloat("_Metallic", 0.2f);
            mat.SetFloat("_Smoothness", 0.45f);
            EditorUtility.SetDirty(mat);
            foreach (var o in AssetDatabase.LoadAllAssetRepresentationsAtPath(AstronautV2Fbx))
                if (o is Material embedded) imp.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), embedded.name), mat);
            imp.SaveAndReimport();

            if (!AssetDatabase.IsValidFolder(MixamoFolder)) return false;
            foreach (var name in MixamoClips)
            {
                string p = MixamoFolder + "/" + name + ".fbx";
                var mi = AssetImporter.GetAtPath(p) as ModelImporter;
                if (mi == null) { Log.AppendLine("AVISO: falta " + p); return false; }

                mi.animationType = ModelImporterAnimationType.Human;
                mi.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                mi.materialImportMode = ModelImporterMaterialImportMode.None;
                var clips = mi.defaultClipAnimations;
                foreach (var c in clips)
                {
                    c.name = name;
                    c.loopTime = name == "Idle" || name == "Walk" || name == "Run" || name == "Interact";
                    c.lockRootRotation = true;
                    c.keepOriginalOrientation = true;
                    c.lockRootHeightY = true;
                    c.keepOriginalPositionY = false;
                    c.heightFromFeet = true;
                    c.lockRootPositionXZ = true;
                    c.keepOriginalPositionXZ = true;
                }
                mi.clipAnimations = clips;
                mi.SaveAndReimport();
            }
            return true;
        }

        static AnimationClip LoadMixamoClip(string name)
        {
            foreach (var a in AssetDatabase.LoadAllAssetsAtPath(MixamoFolder + "/" + name + ".fbx"))
                if (a is AnimationClip c && !c.name.StartsWith("__preview")) return c;
            Log.AppendLine("AVISO: clip Mixamo não encontrado: " + name);
            return null;
        }

        static AnimatorController BuildHumanoidAnimator()
        {
            AssetDatabase.DeleteAsset(HumanoidControllerPath);
            var ac = AnimatorController.CreateAnimatorControllerAtPath(HumanoidControllerPath);
            ac.AddParameter("Speed", AnimatorControllerParameterType.Float);
            var sm = ac.layers[0].stateMachine;
            var loco = ac.CreateBlendTreeInController("Locomotion", out BlendTree tree, 0);
            tree.blendType = BlendTreeType.Simple1D;
            tree.blendParameter = "Speed";
            tree.useAutomaticThresholds = false;
            tree.AddChild(LoadMixamoClip("Idle"), 0f);
            tree.AddChild(LoadMixamoClip("Walk"), 2.2f);
            tree.AddChild(LoadMixamoClip("Run"), 4.5f);
            sm.defaultState = loco;
            foreach (var name in new[] { "OpenDoor", "Interact" })
                sm.AddState(name).motion = LoadMixamoClip(name);
            EditorUtility.SetDirty(ac);
            Log.AppendLine("Animator Humanoid criado: Locomotion (Idle/Walk/Run) + OpenDoor, Interact.");
            return ac;
        }

        // ---------------------------------------------------------------- Pós-processamento

        static void BuildPostProcessing()
        {
            const string path = "Assets/Settings/LunarStation_Volume.asset";
            AssetDatabase.DeleteAsset(path);
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, path);

            var tone = profile.Add<UnityEngine.Rendering.Universal.Tonemapping>(true);
            tone.mode.value = UnityEngine.Rendering.Universal.TonemappingMode.Neutral;
            var bloom = profile.Add<UnityEngine.Rendering.Universal.Bloom>(true);
            bloom.threshold.value = 1.1f;
            bloom.intensity.value = 0.5f;
            bloom.scatter.value = 0.6f;
            var color = profile.Add<UnityEngine.Rendering.Universal.ColorAdjustments>(true);
            color.postExposure.value = 0.25f;
            color.contrast.value = 12f;
            color.saturation.value = -8f;
            var vignette = profile.Add<UnityEngine.Rendering.Universal.Vignette>(true);
            vignette.intensity.value = 0.22f;
            vignette.smoothness.value = 0.4f;
            foreach (var c in profile.components) AssetDatabase.AddObjectToAsset(c, profile);
            EditorUtility.SetDirty(profile);

            var go = new GameObject("Global Volume");
            var vol = go.AddComponent<Volume>();
            vol.isGlobal = true;
            vol.sharedProfile = profile;

            var cam = Camera.main;
            if (cam != null)
            {
                var data = cam.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
                if (data == null) data = cam.gameObject.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
                data.renderPostProcessing = true;
                data.antialiasing = UnityEngine.Rendering.Universal.AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            }
            Log.AppendLine("Pós-processamento: tone mapping neutro, bloom, ajuste de cor e vinheta.");
        }

        // ---------------------------------------------------------------- Jogador, câmara, HUD

        static ThirdPersonCamera BuildCamera()
        {
            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            var cam = go.AddComponent<Camera>();
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 3000f;
            cam.fieldOfView = 55f;
            go.AddComponent<AudioListener>();
            go.transform.position = new Vector3(0f, 2.5f, -15f);
            return go.AddComponent<ThirdPersonCamera>();
        }

        static GameObject BuildPlayer(AnimatorController controller, ThirdPersonCamera cam, bool useV2)
        {
            const float skin = 0.06f;
            var player = new GameObject("Player");
            player.transform.position = new Vector3(0f, 0.02f, -12f);

            var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(useV2 ? AstronautV2Fbx : AstronautFbx), player.transform);
            model.name = "Astronaut";
            model.transform.localPosition = Vector3.zero;
            model.transform.localScale = Vector3.one;
            float h = GetBounds(model).size.y;
            model.transform.localScale = Vector3.one * (1.83f / h);
            // Pés no chão: o CharacterController assenta skinWidth acima do solo, por isso o modelo desce esse valor.
            var b = GetBounds(model);
            float feet = useV2 ? 0f : b.min.y - player.transform.position.y;
            model.transform.localPosition = new Vector3(0f, -feet - skin, 0f);

            var animator = model.GetComponent<Animator>();
            if (animator == null) animator = model.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            var cc = player.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.32f;
            cc.center = new Vector3(0f, 0.92f, 0f);
            cc.stepOffset = 0.35f;
            cc.skinWidth = skin;
            cc.slopeLimit = 50f;

            var pc = player.AddComponent<PlayerController>();
            SetRef(pc, "animator", animator);
            SetRef(pc, "cameraTransform", cam.transform);

            var pi = player.AddComponent<PlayerInteractor>();
            SetRef(pi, "playerCamera", cam);

            cam.Target = player.transform;
            var so = new SerializedObject(cam);
            so.FindProperty("target").objectReferenceValue = player.transform;
            so.ApplyModifiedPropertiesWithoutUndo();

            Log.AppendLine("Astronauta: altura " + GetBounds(model).size.y.ToString("F2") + " m.");
            return player;
        }

        static void BuildHUD()
        {
            var canvasGo = new GameObject("HUD", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            var root = (RectTransform)canvasGo.transform;

            var prompt = Panel(root, "Prompt", new Color(0.02f, 0.05f, 0.08f, 0.8f));
            prompt.anchorMin = prompt.anchorMax = new Vector2(0.5f, 0.12f);
            prompt.sizeDelta = new Vector2(520f, 70f);
            var promptText = Label(prompt, "[E]  Interagir", 30, TextAnchor.MiddleCenter, Color.white);

            var term = Panel(root, "Terminal Panel", new Color(0.02f, 0.05f, 0.08f, 0.85f));
            term.anchorMin = new Vector2(0f, 1f);
            term.anchorMax = new Vector2(1f, 1f);
            term.pivot = new Vector2(0.5f, 1f);
            term.sizeDelta = new Vector2(0f, 64f);
            Label(term, "TERMINAL ATIVO   ·   [Esc] sair", 28, TextAnchor.MiddleCenter, new Color(0.6f, 0.85f, 1f));

            var hud = canvasGo.AddComponent<HUD>();
            SetRef(hud, "promptRoot", prompt.gameObject);
            SetRef(hud, "promptText", promptText);
            SetRef(hud, "terminalPanel", term.gameObject);
        }

        // ---------------------------------------------------------------- Utilitários

        static GameObject LoadPrefab(string path)
        {
            var p = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (p == null) Log.AppendLine("AVISO: prefab em falta: " + path);
            return p;
        }

        static GameObject InstantiateModel(string path, string name)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path));
            go.name = name;
            return go;
        }

        /// <summary>Instancia um prefab, roda-o em Y e alinha o mínimo X/Z dos seus bounds a <paramref name="min"/>; Y é a posição do pivot.</summary>
        static GameObject Place(string prefabPath, Transform parent, float rotY, Vector3 min, Vector3? localScale = null)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(LoadPrefab(prefabPath), parent);
            go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.Euler(0f, rotY, 0f));
            go.transform.localScale = localScale ?? Vector3.one;
            var b = GetBounds(go);
            go.transform.position = new Vector3(min.x - b.min.x, min.y, min.z - b.min.z);
            return go;
        }

        static void SnapToGround(GameObject go, float groundY)
        {
            var b = GetBounds(go);
            go.transform.position += Vector3.up * (groundY - b.min.y);
        }

        static Bounds GetBounds(GameObject go)
        {
            var rs = go.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.zero);
            var b = rs[0].bounds;
            for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
            return b;
        }

        static void AddMeshColliders(GameObject root)
        {
            foreach (var mf in root.GetComponentsInChildren<MeshFilter>())
                if (mf.GetComponent<Collider>() == null && mf.sharedMesh != null)
                    mf.gameObject.AddComponent<MeshCollider>().sharedMesh = mf.sharedMesh;
        }

        static RectTransform Panel(RectTransform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            go.GetComponent<Image>().color = color;
            return rt;
        }

        static Text Label(RectTransform parent, string text, int size, TextAnchor anchor, Color color)
            => LabelAt(parent, text, size, anchor, color, Vector2.zero, Vector2.one, false);

        static RectTransform Box(RectTransform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Color color)
        {
            var rt = Panel(parent, name, color);
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            return rt;
        }

        static Font LoadFont(bool bold)
        {
            var f = AssetDatabase.LoadAssetAtPath<Font>(bold ? FontBold : FontRegular);
            return f != null ? f : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        static Text LabelAt(RectTransform parent, string text, int size, TextAnchor anchor, Color color, Vector2 anchorMin, Vector2 anchorMax, bool bold)
        {
            var go = new GameObject("Text", typeof(RectTransform), typeof(Text));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = new Vector2(32f, 8f);
            rt.offsetMax = new Vector2(-32f, -8f);
            var t = go.GetComponent<Text>();
            t.font = LoadFont(bold);
            t.text = text;
            t.fontSize = size;
            t.alignment = anchor;
            t.color = color;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }

        static void SetRef(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(field);
            if (p == null) { Log.AppendLine("AVISO: campo em falta " + target.GetType().Name + "." + field); return; }
            p.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetFloat(Object target, string field, float value)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(field);
            if (p == null) { Log.AppendLine("AVISO: campo em falta " + target.GetType().Name + "." + field); return; }
            p.floatValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void AddToBuildSettings(string path)
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (scenes.Exists(s => s.path == path)) return;
            scenes.Insert(0, new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
