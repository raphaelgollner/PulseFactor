using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Localization;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace PulseFactor.EditorTools
{
    // Gera o conteudo da fatia 1: 4 setores, estacoes, campos, materiais, textos, UI e a cena Slice.
    // Rode depois de "Generate Primitive Blocks". Idempotente.
    public static class SliceContentFactory
    {
        const string Root = "Assets/_Project";
        const string WorldData = Root + "/Data/World";
        const string MatDir = Root + "/Materials";
        const string UiDir = Root + "/UI";
        const string LocDir = Root + "/Localization";
        const string ScenePath = Root + "/Scenes/Slice.unity";

        [MenuItem("PulseFactor/Generate Slice Content")]
        public static void Generate()
        {
            foreach (var d in new[] { WorldData, MatDir, UiDir, LocDir, Root + "/Scenes" }) Directory.CreateDirectory(d);
            AssetDatabase.Refresh();

            var res = LoadResources();
            var config = Asset<GameConfig>($"{Root}/Data/GameConfig.asset");
            config.testMode = true;
            config.startingCredits = 1000;
            EditorUtility.SetDirty(config);

            var galaxy = BuildGalaxy(res);
            var mats = BuildMaterials();
            BuildLocalization();
            var panels = BuildPanelSettings();
            BuildScene(galaxy, config, res, mats, panels);

            AssetDatabase.SaveAssets();
            if (!galaxy.Validate(out var errors)) Debug.LogError("[PulseFactor] Galaxia invalida: " + string.Join("; ", errors));
            Debug.Log("[PulseFactor] Conteudo da fatia gerado. Abra Assets/_Project/Scenes/Slice.unity e de Play.");
        }

        // ---------- Recursos ----------

        static Dictionary<string, ResourceDef> LoadResources()
        {
            var dict = new Dictionary<string, ResourceDef>();
            foreach (var guid in AssetDatabase.FindAssets("t:ResourceDef", new[] { Root + "/Data/Resources" }))
            {
                var r = AssetDatabase.LoadAssetAtPath<ResourceDef>(AssetDatabase.GUIDToAssetPath(guid));
                dict[r.id] = r;
            }
            if (dict.Count < 6) throw new System.InvalidOperationException("Rode 'PulseFactor/Generate Primitive Blocks' antes.");
            return dict;
        }

        // ---------- Mundo ----------

        static GalaxyDef BuildGalaxy(Dictionary<string, ResourceDef> r)
        {
            var aster = Sector("aster", new Vector3(0.4f, 0.15f, 1f));
            var venera = Sector("venera", new Vector3(-0.6f, 0.1f, 0.8f));
            var centaurus = Sector("centaurus", new Vector3(0.9f, -0.2f, -0.3f));
            var cinturao = Sector("cinturao", new Vector3(-0.2f, 0.4f, -1f));

            // Aster: planeta azulado, 1 lua, setor inicial.
            Body(aster, "aster", null, 6e6, 0, 0, new Color(0.3f, 0.5f, 0.85f));
            Body(aster, "aster_i", "aster", 1.7e6, 4e8, 30, new Color(0.6f, 0.6f, 0.62f));
            Station(aster, "aster_orbital", "corp_a", "aster", 2e7, 0, r,
                sells: new[] { "iron", "ice", "coal" }, buys: new[] { "iron", "ice", "coal", "copper", "titanium", "cobalt" }, buyMult: 1.2f);
            Field(aster, "aster_field", "aster", 2.15e7, 3, 150, 3000, 101, false,
                (r["iron"], 35), (r["ice"], 25), (r["coal"], 20), (r["copper"], 12), (r["titanium"], 5), (r["cobalt"], 3));

            // Venera: planeta dourado.
            Body(venera, "venera", null, 5e6, 0, 0, new Color(0.85f, 0.65f, 0.35f));
            Body(venera, "venera_i", "venera", 1.2e6, 3e8, 200, new Color(0.55f, 0.5f, 0.45f));
            Station(venera, "venera_hub", "corp_b", "venera", 1.8e7, 90, r,
                sells: new[] { "copper", "ice" }, buys: new[] { "iron", "coal", "titanium", "cobalt" }, buyMult: 1.25f);
            Field(venera, "venera_field", "venera", 2e7, 95, 120, 3000, 202, true,
                (r["copper"], 35), (r["ice"], 30), (r["iron"], 20), (r["titanium"], 10), (r["cobalt"], 5));

            // Centaurus: gigante gasoso com 4 luas.
            Body(centaurus, "centaurus", null, 4e7, 0, 0, new Color(0.88f, 0.76f, 0.55f));
            Body(centaurus, "centaurus_i", "centaurus", 1e6, 2e8, 0, new Color(0.7f, 0.68f, 0.6f));
            Body(centaurus, "centaurus_ii", "centaurus", 2e6, 3.5e8, 80, new Color(0.5f, 0.55f, 0.6f));
            Body(centaurus, "centaurus_iii", "centaurus", 1.5e6, 5e8, 170, new Color(0.65f, 0.55f, 0.5f));
            Body(centaurus, "centaurus_iv", "centaurus", 2.5e6, 7e8, 260, new Color(0.8f, 0.82f, 0.85f));
            Station(centaurus, "centaurus_ii_yard", "corp_c", "centaurus_ii", 8e6, 0, r,
                sells: new[] { "titanium", "coal" }, buys: new[] { "iron", "ice", "copper", "cobalt" }, buyMult: 1.3f);
            Field(centaurus, "centaurus_field", "centaurus_iii", 1e7, 40, 150, 3500, 303, true,
                (r["titanium"], 30), (r["cobalt"], 20), (r["iron"], 25), (r["ice"], 25));

            // Cinturao: so asteroides, estacao central no asteroide gigante.
            Body(cinturao, "cinturao", null, 4e4, 0, 0, new Color(0.45f, 0.42f, 0.4f));
            Station(cinturao, "cinturao_central", "corp_a", "cinturao", 4.5e4, 0, r,
                sells: new[] { "iron", "cobalt" }, buys: new[] { "ice", "coal", "copper" }, buyMult: 1.3f, sellMult: 0.9f);
            Field(cinturao, "cinturao_field_a", "cinturao", 5e5, 0, 260, 5000, 404, true,
                (r["iron"], 30), (r["cobalt"], 20), (r["titanium"], 15), (r["coal"], 20), (r["copper"], 15));
            Field(cinturao, "cinturao_field_b", "cinturao", 8e5, 150, 220, 5000, 505, true,
                (r["iron"], 25), (r["cobalt"], 25), (r["titanium"], 20), (r["ice"], 30));

            // Gates: Aster-Venera, Aster-Cinturao, Cinturao-Centaurus.
            Link(aster, "gate_aster_venera", "aster", 4e7, 120, venera, "gate_venera_aster", "venera", 4e7, 300);
            Link(aster, "gate_aster_cinturao", "aster", 4e7, 240, cinturao, "gate_cinturao_aster", "cinturao", 3e6, 60);
            Link(cinturao, "gate_cinturao_centaurus", "cinturao", 3e6, 240, centaurus, "gate_centaurus_cinturao", "centaurus", 1.5e8, 330);

            var galaxy = Asset<GalaxyDef>($"{WorldData}/Galaxy.asset");
            galaxy.sectors = new List<SectorDef> { aster, venera, centaurus, cinturao };
            galaxy.start = aster;
            foreach (var s in galaxy.sectors) EditorUtility.SetDirty(s);
            EditorUtility.SetDirty(galaxy);
            return galaxy;
        }

        static SectorDef Sector(string id, Vector3 sunDir)
        {
            var s = Asset<SectorDef>($"{WorldData}/Sector_{id}.asset");
            s.id = id;
            s.sunDirection = sunDir;
            s.bodies = new List<SolarSystemDef.Body>();
            s.stations = new List<StationPlacement>();
            s.fields = new List<FieldPlacement>();
            s.gates = new List<GatePlacement>();
            return s;
        }

        static void Body(SectorDef s, string id, string parent, double radius, double orbit, float angle, Color color) =>
            s.bodies.Add(new SolarSystemDef.Body { id = id, parentId = parent, radius = radius, orbitDistance = orbit, orbitAngleDeg = angle, color = color });

        static void Station(SectorDef s, string id, string corp, string parent, double dist, float angle, Dictionary<string, ResourceDef> r,
            string[] sells, string[] buys, float buyMult, float sellMult = 1f)
        {
            var def = Asset<StationDef>($"{WorldData}/Station_{id}.asset");
            def.id = id;
            def.corporationId = corp;
            def.market = new List<MarketEntry>();
            foreach (var key in sells.Union(buys))
            {
                bool sell = sells.Contains(key), buy = buys.Contains(key);
                def.market.Add(new MarketEntry { resource = r[key], sells = sell, buys = buy, priceMultiplier = buy && !sell ? buyMult : sellMult });
            }
            EditorUtility.SetDirty(def);
            s.stations.Add(new StationPlacement { def = def, at = new Placement { parentId = parent, distance = dist, angleDeg = angle } });
        }

        static void Field(SectorDef s, string id, string parent, double dist, float angle, int count, float radius, int seed, bool pirates,
            params (ResourceDef res, float weight)[] composition)
        {
            var def = Asset<AsteroidFieldDef>($"{WorldData}/Field_{id}.asset");
            def.id = id; def.count = count; def.radius = radius; def.seed = seed;
            def.composition = composition.Select(c => new AsteroidFieldDef.Composition { resource = c.res, weight = c.weight }).ToList();
            EditorUtility.SetDirty(def);
            s.fields.Add(new FieldPlacement { def = def, pirates = pirates, at = new Placement { parentId = parent, distance = dist, angleDeg = angle } });
        }

        static void Link(SectorDef a, string gateA, string parentA, double distA, float angleA,
                         SectorDef b, string gateB, string parentB, double distB, float angleB)
        {
            a.gates.Add(new GatePlacement { id = gateA, target = b, targetGateId = gateB, at = new Placement { parentId = parentA, distance = distA, angleDeg = angleA } });
            b.gates.Add(new GatePlacement { id = gateB, target = a, targetGateId = gateA, at = new Placement { parentId = parentB, distance = distB, angleDeg = angleB } });
        }

        // ---------- Materiais ----------

        class Mats { public Material planet, sun, hull, light, asteroid, gate, portal, beam, star, dust; }

        static Mats BuildMaterials()
        {
            var lit = Shader.Find("Universal Render Pipeline/Lit");
            var unlit = Shader.Find("Universal Render Pipeline/Unlit");
            var particles = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            return new Mats
            {
                planet = Mat("M_Planet", lit, Color.white, m => m.SetFloat("_Smoothness", 0.1f)),
                sun = Mat("M_Sun", unlit, new Color(1f, 0.9f, 0.7f) * 6f, null),
                hull = Mat("M_StationHull", lit, new Color(0.55f, 0.57f, 0.6f), m => { m.SetFloat("_Smoothness", 0.4f); m.SetFloat("_Metallic", 0.3f); }),
                light = Mat("M_DockLight", unlit, new Color(0.2f, 0.9f, 1f) * 3f, null),
                asteroid = Mat("M_Asteroid", lit, new Color(0.36f, 0.33f, 0.3f), m => m.SetFloat("_Smoothness", 0.05f)),
                gate = Mat("M_GateRing", lit, new Color(0.38f, 0.4f, 0.46f), m => { m.SetFloat("_Metallic", 0.7f); m.SetFloat("_Smoothness", 0.55f); }),
                portal = Mat("M_GatePortal", unlit, new Color(0.3f, 0.6f, 1f, 0.22f), Transparent),
                beam = Mat("M_MiningBeam", unlit, new Color(1f, 0.55f, 0.15f) * 4f, null),
                star = Mat("M_Stars", particles, Color.white, null),
                dust = Mat("M_SpaceDust", particles, Color.white, m =>
                {
                    m.SetFloat("_Surface", 1f);
                    m.SetFloat("_Blend", 2f); // aditivo
                    m.SetOverrideTag("RenderType", "Transparent");
                    m.renderQueue = (int)RenderQueue.Transparent;
                    m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                    m.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                    m.SetInt("_DstBlend", (int)BlendMode.One);
                    m.SetFloat("_ZWrite", 0f);
                }),
            };
        }

        static void Transparent(Material m)
        {
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 0f);
            m.SetFloat("_Cull", 0f);
            m.SetFloat("_ZWrite", 0f);
            m.SetOverrideTag("RenderType", "Transparent");
            m.renderQueue = (int)RenderQueue.Transparent;
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            m.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
        }

        static Material Mat(string name, Shader shader, Color color, System.Action<Material> setup)
        {
            string path = $"{MatDir}/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!m) { m = new Material(shader); AssetDatabase.CreateAsset(m, path); }
            m.shader = shader;
            m.SetColor("_BaseColor", color);
            setup?.Invoke(m);
            EditorUtility.SetDirty(m);
            return m;
        }

        // ---------- Localizacao ----------

        static void BuildLocalization()
        {
            var settings = LocalizationEditorSettings.ActiveLocalizationSettings;
            if (!settings)
            {
                settings = Asset<LocalizationSettings>($"{LocDir}/LocalizationSettings.asset");
                LocalizationEditorSettings.ActiveLocalizationSettings = settings;
            }

            var pt = EnsureLocale("pt-BR");
            EnsureLocale("en");

            var selectors = settings.GetStartupLocaleSelectors();
            if (!selectors.OfType<SpecificLocaleSelector>().Any())
                selectors.Add(new SpecificLocaleSelector { LocaleId = pt.Identifier });
            EditorUtility.SetDirty(settings);

            var collection = LocalizationEditorSettings.GetStringTableCollection(Loc.Table)
                             ?? LocalizationEditorSettings.CreateStringTableCollection(Loc.Table, LocDir);
            foreach (var locale in LocalizationEditorSettings.GetLocales())
                if (!collection.ContainsTable(locale.Identifier)) collection.AddNewTable(locale.Identifier);

            var ptTable = (StringTable)collection.GetTable(new LocaleIdentifier("pt-BR"));
            var enTable = (StringTable)collection.GetTable(new LocaleIdentifier("en"));
            foreach (var kv in LocalizationSeed.Strings)
            {
                Set(ptTable, kv.Key, kv.Value.pt);
                Set(enTable, kv.Key, kv.Value.en);
            }
            EditorUtility.SetDirty(ptTable);
            EditorUtility.SetDirty(enTable);
            EditorUtility.SetDirty(collection.SharedData);
        }

        static Locale EnsureLocale(string code)
        {
            var id = new LocaleIdentifier(code);
            var existing = LocalizationEditorSettings.GetLocale(id);
            if (existing) return existing;
            var locale = Locale.CreateLocale(id);
            AssetDatabase.CreateAsset(locale, $"{LocDir}/Locale_{code}.asset");
            LocalizationEditorSettings.AddLocale(locale);
            return locale;
        }

        static void Set(StringTable table, string key, string value)
        {
            var e = table.GetEntry(key);
            if (e != null) e.Value = value;
            else table.AddEntry(key, value);
        }

        // ---------- UI ----------

        class Panels { public PanelSettings screen, terminal, cockpit; }

        static Panels BuildPanelSettings()
        {
            string themePath = $"{UiDir}/PulseTheme.tss";
            if (!File.Exists(themePath))
            {
                File.WriteAllText(themePath, "@import url(\"unity-theme://default\");\n");
                AssetDatabase.ImportAsset(themePath);
            }
            var theme = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(themePath);

            PanelSettings P(string name, bool screenSpace)
            {
                var p = Asset<PanelSettings>($"{UiDir}/{name}.asset");
                p.themeStyleSheet = theme;
                if (screenSpace)
                {
                    p.scaleMode = PanelScaleMode.ScaleWithScreenSize;
                    p.referenceResolution = new Vector2Int(1920, 1080);
                    p.match = 0.5f;
                }
                else
                {
                    p.scaleMode = PanelScaleMode.ConstantPixelSize;
                    p.scale = 1f;
                }
                EditorUtility.SetDirty(p);
                return p;
            }

            return new Panels { screen = P("Panel_Screen", true), terminal = P("Panel_Terminal", true), cockpit = P("Panel_Cockpit", false) };
        }

        // ---------- Cena ----------

        static void BuildScene(GalaxyDef galaxy, GameConfig config, Dictionary<string, ResourceDef> res, Mats m, Panels panels)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cam = new GameObject("Main Camera").AddComponent<Camera>();
            cam.tag = "MainCamera";
            cam.gameObject.AddComponent<AudioListener>();

            var boot = new GameObject("GameBootstrap").AddComponent<GameBootstrap>();
            boot.galaxy = galaxy;
            boot.config = config;
            boot.startingShip = AssetDatabase.LoadAssetAtPath<ShipModelDef>($"{Root}/Data/Ships/Model_cruise.asset");
            boot.catalog = AssetDatabase.FindAssets("t:BlockDef", new[] { Root + "/Data/Ships/Blocks" })
                .Select(g => AssetDatabase.LoadAssetAtPath<BlockDef>(AssetDatabase.GUIDToAssetPath(g))).ToList();
            boot.resources = new[] { "iron", "ice", "coal", "copper", "titanium", "cobalt" }.Select(k => res[k]).ToList();
            boot.planetMaterial = m.planet; boot.sunMaterial = m.sun; boot.hullMaterial = m.hull; boot.lightMaterial = m.light;
            boot.asteroidMaterial = m.asteroid; boot.gateMaterial = m.gate; boot.portalMaterial = m.portal;
            boot.beamMaterial = m.beam; boot.starMaterial = m.star; boot.dustMaterial = m.dust;
            boot.screenPanel = panels.screen; boot.terminalPanel = panels.terminal; boot.cockpitPanelBase = panels.cockpit;

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.08f, 0.09f, 0.11f);
            RenderSettings.skybox = null;

            EditorSceneManager.SaveScene(scene, ScenePath);
            var scenes = EditorBuildSettings.scenes.Where(s => s.path != ScenePath).ToList();
            scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        static T Asset<T>(string path) where T : ScriptableObject
        {
            var a = AssetDatabase.LoadAssetAtPath<T>(path);
            if (a) return a;
            a = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(a, path);
            return a;
        }
    }
}
