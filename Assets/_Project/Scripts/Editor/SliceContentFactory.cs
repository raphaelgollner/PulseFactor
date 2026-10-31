using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Mathematics;
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
        const string MenuScenePath = Root + "/Scenes/MainMenu.unity";

        [MenuItem("PulseFactor/Generate Slice Content")]
        public static void Generate()
        {
            foreach (var d in new[] { WorldData, MatDir, UiDir, LocDir, Root + "/Scenes" }) Directory.CreateDirectory(d);
            AssetDatabase.Refresh();

            var res = LoadResources();
            BuildCorporations();
            var config = Asset<GameConfig>($"{Root}/Data/GameConfig.asset");
            config.testMode = false; // fase 2: economia real
            config.startingCredits = 1000;
            EditorUtility.SetDirty(config);

            var galaxy = BuildGalaxy(res);
            var mats = BuildMaterials();
            BuildLocalization();
            var panels = BuildPanelSettings();
            BuildScene(galaxy, config, res, mats, panels);

            // Salvar e validar ANTES de abrir outra cena: trocar de cena descarrega os assets que so a Slice usava.
            AssetDatabase.SaveAssets();
            if (!galaxy.Validate(out var errors)) Debug.LogError("[PulseFactor] Galaxia invalida: " + string.Join("; ", errors));
            BuildMenuScene(mats, panels);
            AssetDatabase.SaveAssets();
            Debug.Log("[PulseFactor] Conteudo gerado. A cena MainMenu ficou aberta: de Play para comecar pelo menu.");
        }

        // ---------- Recursos ----------

        // Ordem da lista do mercado: minerios primeiro, depois produtos.
        static readonly string[] Goods =
        {
            "iron", "ice", "coal", "copper", "titanium", "cobalt",
            "water", "scrap", "rations", "solid_fuel", "construction", "medical",
            "energy_cells", "alloys", "ship_parts", "processors",
        };

        static Dictionary<string, ResourceDef> LoadResources()
        {
            var dict = new Dictionary<string, ResourceDef>();
            foreach (var guid in AssetDatabase.FindAssets("t:ResourceDef", new[] { Root + "/Data/Resources" }))
            {
                var r = AssetDatabase.LoadAssetAtPath<ResourceDef>(AssetDatabase.GUIDToAssetPath(guid));
                dict[r.id] = r;
            }
            if (dict.Count < Goods.Length) throw new System.InvalidOperationException("Rode 'PulseFactor/Generate Primitive Blocks' antes.");
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
            Station(aster, "gollner", "nova", StationStyle.Ring, Standard, "aster", 2e7, 0, r,
                Sell("iron", 1f), Sell("ice", 1f), Sell("coal", 1f), Sell("medical", 0.95f), Sell("processors", 0.95f),
                Buy("copper", 1.25f), Buy("titanium", 1.3f), Buy("cobalt", 1.3f),
                Buy("ship_parts", 1.3f), Buy("construction", 1.25f), Buy("water", 1.3f));
            Station(aster, "apollo", "nova", StationStyle.Outpost, Outpost, "aster_i", 5e6, 0, r,
                Sell("copper", 0.9f), Sell("titanium", 0.95f), Sell("water", 0.9f), Sell("scrap", 0.9f),
                Buy("ice", 1.35f), Buy("coal", 1.3f), Buy("iron", 1.2f),
                Buy("rations", 1.3f), Buy("medical", 1.3f), Buy("energy_cells", 1.3f), Buy("solid_fuel", 1.3f));
            PlaceApolloFacingGollner(aster);
            Station(aster, "kepler_lab", "nova", StationStyle.ScienceOutpost, ScienceDocks, "aster", 2.6e7, 60, r,
                Sell("medical", 0.9f), Sell("processors", 0.95f), Sell("water", 0.95f),
                Buy("energy_cells", 1.3f), Buy("copper", 1.3f), Buy("cobalt", 1.35f), Buy("ice", 1.25f), Buy("rations", 1.25f));
            Station(aster, "halden_foundry", "magna", StationStyle.IndustrialOutpost, IndustrialDocks, "aster_i", 4e6, 180, r,
                Sell("construction", 0.9f), Sell("alloys", 0.95f), Sell("scrap", 0.9f),
                Buy("iron", 1.3f), Buy("titanium", 1.3f), Buy("coal", 1.3f), Buy("energy_cells", 1.25f), Buy("rations", 1.25f));
            Field(aster, "aster_field", "aster", 2.15e7, 3, 150, 3000, 101, false,
                (r["iron"], 35), (r["ice"], 25), (r["coal"], 20), (r["copper"], 12), (r["titanium"], 5), (r["cobalt"], 3));

            // Venera: planeta dourado.
            Body(venera, "venera", null, 5e6, 0, 0, new Color(0.85f, 0.65f, 0.35f));
            Body(venera, "venera_i", "venera", 1.2e6, 3e8, 200, new Color(0.55f, 0.5f, 0.45f));
            Station(venera, "magna", "magna", StationStyle.Industrial, Standard, "venera", 1.8e7, 90, r,
                Sell("copper", 1f), Sell("construction", 0.95f), Sell("alloys", 0.95f), Sell("ship_parts", 0.95f), Sell("solid_fuel", 0.95f),
                Buy("iron", 1.25f), Buy("ice", 1.3f), Buy("titanium", 1.35f), Buy("cobalt", 1.4f),
                Buy("scrap", 1.3f), Buy("processors", 1.3f));
            Station(venera, "ignis_refinery", "magna", StationStyle.IndustrialOutpost, IndustrialDocks, "venera", 2.4e7, 200, r,
                Sell("alloys", 0.9f), Sell("solid_fuel", 0.9f), Sell("construction", 0.95f),
                Buy("iron", 1.3f), Buy("coal", 1.35f), Buy("titanium", 1.3f), Buy("scrap", 1.3f), Buy("water", 1.25f));
            Station(venera, "bastiao_post", "federation", StationStyle.MilitaryOutpost, MilitaryDocks, "venera_i", 3e6, 0, r,
                Sell("scrap", 0.9f), Sell("energy_cells", 1f),
                Buy("rations", 1.35f), Buy("medical", 1.3f), Buy("ship_parts", 1.35f), Buy("solid_fuel", 1.3f), Buy("alloys", 1.3f));
            Field(venera, "venera_field", "venera", 2e7, 95, 120, 3000, 202, true,
                (r["copper"], 35), (r["ice"], 30), (r["iron"], 20), (r["titanium"], 10), (r["cobalt"], 5));

            // Centaurus: gigante gasoso com 4 luas.
            Body(centaurus, "centaurus", null, 4e7, 0, 0, new Color(0.88f, 0.76f, 0.55f));
            Body(centaurus, "centaurus_i", "centaurus", 1e6, 2e8, 0, new Color(0.7f, 0.68f, 0.6f));
            Body(centaurus, "centaurus_ii", "centaurus", 2e6, 3.5e8, 80, new Color(0.5f, 0.55f, 0.6f));
            Body(centaurus, "centaurus_iii", "centaurus", 1.5e6, 5e8, 170, new Color(0.65f, 0.55f, 0.5f));
            Body(centaurus, "centaurus_iv", "centaurus", 2.5e6, 7e8, 260, new Color(0.8f, 0.82f, 0.85f));
            Station(centaurus, "centaurus_ii_yard", "federation", StationStyle.Ring, Standard, "centaurus_ii", 8e6, 0, r,
                Sell("titanium", 1f), Sell("coal", 1f), Sell("energy_cells", 1f),
                Buy("iron", 1.3f), Buy("copper", 1.3f),
                Buy("ship_parts", 1.35f), Buy("alloys", 1.3f), Buy("construction", 1.3f), Buy("processors", 1.3f));
            Station(centaurus, "sentinela_base", "federation", StationStyle.MilitaryOutpost, MilitaryDocks, "centaurus_i", 4e6, 0, r,
                Sell("energy_cells", 0.95f), Sell("scrap", 0.9f),
                Buy("rations", 1.35f), Buy("medical", 1.3f), Buy("ship_parts", 1.35f), Buy("solid_fuel", 1.3f), Buy("processors", 1.3f));
            Station(centaurus, "orion_institute", "nova", StationStyle.ScienceOutpost, ScienceDocks, "centaurus_iv", 6e6, 0, r,
                Sell("medical", 0.95f), Sell("processors", 0.9f),
                Buy("titanium", 1.3f), Buy("cobalt", 1.3f), Buy("water", 1.3f), Buy("energy_cells", 1.3f), Buy("construction", 1.25f));
            Field(centaurus, "centaurus_field", "centaurus_iii", 1e7, 40, 150, 3500, 303, true,
                (r["titanium"], 30), (r["cobalt"], 20), (r["iron"], 25), (r["ice"], 25));

            // Cinturao: so asteroides, estacao central no asteroide gigante.
            Body(cinturao, "cinturao", null, 4e4, 0, 0, new Color(0.45f, 0.42f, 0.4f));
            Station(cinturao, "cinturao_central", "atlas", StationStyle.Ring, Standard, "cinturao", 4.5e4, 0, r,
                Sell("iron", 0.9f), Sell("cobalt", 0.9f), Sell("rations", 0.95f), Sell("energy_cells", 0.95f),
                Buy("ice", 1.3f), Buy("coal", 1.3f), Buy("copper", 1.3f),
                Buy("ship_parts", 1.35f), Buy("medical", 1.3f), Buy("solid_fuel", 1.3f));
            Station(cinturao, "atlas7_scrapyard", "atlas", StationStyle.IndustrialOutpost, IndustrialDocks, "cinturao", 3e5, 90, r,
                Sell("scrap", 0.8f), Sell("iron", 0.9f), Sell("alloys", 0.95f),
                Buy("energy_cells", 1.3f), Buy("rations", 1.3f), Buy("water", 1.3f), Buy("solid_fuel", 1.25f));
            Station(cinturao, "vega_observatory", "atlas", StationStyle.ScienceOutpost, ScienceDocks, "cinturao", 1.2e6, 270, r,
                Sell("processors", 0.95f), Sell("medical", 0.95f), Sell("water", 0.9f),
                Buy("ice", 1.3f), Buy("cobalt", 1.35f), Buy("copper", 1.3f), Buy("energy_cells", 1.3f));
            Field(cinturao, "cinturao_field_a", "cinturao", 5e5, 0, 260, 5000, 404, true,
                (r["iron"], 30), (r["cobalt"], 20), (r["titanium"], 15), (r["coal"], 20), (r["copper"], 15));
            Field(cinturao, "cinturao_field_b", "cinturao", 8e5, 150, 220, 5000, 505, true,
                (r["iron"], 25), (r["cobalt"], 25), (r["titanium"], 20), (r["ice"], 30));

            aster.dominantCorporationId = "nova";
            venera.dominantCorporationId = "magna";
            cinturao.dominantCorporationId = "atlas";
            centaurus.dominantCorporationId = "federation";

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

        // Escala geral das distancias (playtest 2026-10-31: -20%). Tamanhos dos corpos nao mudam.
        const double DistanceScale = 1.0; // 0,8 no teste anterior; voltou ao original
        const double StationOrbitFactor = 1.0; // 1,2 no teste anterior; voltou ao original

        // Distancia escalada, sem nunca entrar no corpo-pai (ex.: estacao colada no asteroide do Cinturao).
        static double Scaled(SectorDef s, string parentId, double dist)
        {
            var parent = s.bodies.Find(b => b.id == parentId);
            double scaled = dist * DistanceScale;
            return parent != null && scaled < parent.radius * 1.05 ? dist : scaled;
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
            s.bodies.Add(new SolarSystemDef.Body { id = id, parentId = parent, radius = radius, orbitDistance = orbit * DistanceScale, orbitAngleDeg = angle, color = color });

        static readonly DockSize[] Standard = { DockSize.P, DockSize.P, DockSize.P, DockSize.P, DockSize.M, DockSize.M, DockSize.G };
        static readonly DockSize[] Outpost = { DockSize.P, DockSize.M, DockSize.G };
        static readonly DockSize[] IndustrialDocks = { DockSize.P, DockSize.M, DockSize.G };
        static readonly DockSize[] ScienceDocks = { DockSize.P, DockSize.P, DockSize.M };
        static readonly DockSize[] MilitaryDocks = { DockSize.P, DockSize.P, DockSize.M };

        // Estaleiros (fase 3): naves vendidas em cada um. Outposts militares: so Assault P.
        static readonly string[] AllShips = { "cruise", "crusader", "rapidfire", "typea", "typec", "horder", "hulker" };
        static readonly string[] MilitaryShips = { "rapidfire", "typea" };

        static string[] ShipyardModels(string id, StationStyle style) => id switch
        {
            "gollner" or "cinturao_central" => AllShips,
            "centaurus_ii_yard" => new[] { "cruise", "crusader", "rapidfire", "typea" },
            _ => style == StationStyle.MilitaryOutpost ? MilitaryShips : new string[0],
        };

        static (string res, bool sells, float mult) Sell(string res, float mult) => (res, true, mult);
        static (string res, bool sells, float mult) Buy(string res, float mult) => (res, false, mult);

        // Cada recurso: a estacao vende OU compra, com o seu multiplicador de preco.
        static void Station(SectorDef s, string id, string corp, StationStyle style, DockSize[] docks, string parent, double dist, float angle,
            Dictionary<string, ResourceDef> r, params (string res, bool sells, float mult)[] market)
        {
            var def = Asset<StationDef>($"{WorldData}/Station_{id}.asset");
            def.id = id;
            def.corporationId = corp;
            def.corporation = AssetDatabase.LoadAssetAtPath<CorporationDef>($"{WorldData}/Corp_{corp}.asset");
            def.style = style;
            def.dockSizes = docks;
            // Magna fabrica Pecas de Naves e nao tem estaleiro; todo estaleiro consome pecas (fase 2E).
            def.shipyardModels = ShipyardModels(id, style)
                .Select(m => AssetDatabase.LoadAssetAtPath<ShipModelDef>($"{Root}/Data/Ships/Model_{m}.asset")).Where(m => m).ToList();
            def.hasShipyard = def.shipyardModels.Count > 0;
            def.market = market.Select(m => Entry(id, r[m.res], m.sells, m.mult, def.hasShipyard)).ToList();
            EditorUtility.SetDirty(def);
            // Estacoes 20% mais afastadas do corpo que orbitam (playtest 2026-10-31).
            s.stations.Add(new StationPlacement { def = def, at = new Placement { parentId = parent, distance = Scaled(s, parent, dist * StationOrbitFactor), angleDeg = angle } });
        }

        // Estoque: produtos baratos tem mais volume. Produtor repoe tudo em ~2 h de jogo; consumidor esvazia em ~3 h
        // (estaleiro consome pecas de naves 2x mais rapido). Estoque inicial varia por estacao.
        static MarketEntry Entry(string stationId, ResourceDef res, bool sells, float mult, bool shipyard)
        {
            int capacity = Mathf.Clamp(Mathf.RoundToInt(6000f / res.basePrice / 10f) * 10, 40, 600);
            float rate = sells ? capacity / 2f : capacity / 3f;
            if (!sells && shipyard && res.id == "ship_parts") rate *= 2f;
            float jitter = StableHash(stationId + res.id) % 1000 / 1000f;
            float fill = sells ? 0.55f + 0.3f * jitter : 0.1f + 0.4f * jitter;
            return new MarketEntry
            {
                resource = res, sells = sells, buys = !sells, priceMultiplier = mult,
                capacity = capacity, startStock = Mathf.RoundToInt(capacity * fill), ratePerHour = rate,
            };
        }

        static int StableHash(string s)
        {
            unchecked
            {
                int h = 17;
                foreach (var c in s) h = h * 31 + c;
                return h & 0x7fffffff;
            }
        }

        static void Field(SectorDef s, string id, string parent, double dist, float angle, int count, float radius, int seed, bool pirates,
            params (ResourceDef res, float weight)[] composition)
        {
            var def = Asset<AsteroidFieldDef>($"{WorldData}/Field_{id}.asset");
            def.id = id; def.count = count; def.radius = radius; def.seed = seed;
            def.composition = composition.Select(c => new AsteroidFieldDef.Composition { resource = c.res, weight = c.weight }).ToList();
            EditorUtility.SetDirty(def);
            s.fields.Add(new FieldPlacement { def = def, pirates = pirates, at = new Placement { parentId = parent, distance = Scaled(s, parent, dist), angleDeg = angle } });
        }

        static void Link(SectorDef a, string gateA, string parentA, double distA, float angleA,
                         SectorDef b, string gateB, string parentB, double distB, float angleB)
        {
            a.gates.Add(new GatePlacement { id = gateA, target = b, targetGateId = gateB, at = new Placement { parentId = parentA, distance = Scaled(a, parentA, distA), angleDeg = angleA } });
            b.gates.Add(new GatePlacement { id = gateB, target = a, targetGateId = gateA, at = new Placement { parentId = parentB, distance = Scaled(b, parentB, distB), angleDeg = angleB } });
        }

        // Ajuste individual (playtest 2026-10-31): o Apollo fica do lado de Aster I voltado para a Gollner,
        // um pouco acima do bloqueio de dobra da lua, para a rota direta nao cruzar a zona da lua.
        const double ApolloDistance = 2.4e6; // m do centro de Aster I (raio 1,7e6; bloqueio 1,87e6; alerta 2,04e6)

        static void PlaceApolloFacingGollner(SectorDef aster)
        {
            var bodies = aster.BodyPositions();
            var gollner = aster.stations.Find(p => p.def.id == "gollner");
            var apollo = aster.stations.Find(p => p.def.id == "apollo");
            var toGollner = SectorDef.Resolve(gollner.at, bodies).value - bodies[apollo.at.parentId];
            float angle = (float)math.degrees(math.atan2(toGollner.z, toGollner.x));
            apollo.at = new Placement { parentId = apollo.at.parentId, distance = ApolloDistance, angleDeg = angle };
            apollo.faceTowardStationId = "gollner";
        }

        static void BuildCorporations()
        {
            void Corp(string id, Color color)
            {
                var c = Asset<CorporationDef>($"{WorldData}/Corp_{id}.asset");
                c.id = id;
                c.color = color;
                EditorUtility.SetDirty(c);
            }
            Corp("nova", new Color(0.4f, 0.8f, 1f));
            Corp("magna", new Color(1f, 0.7f, 0.15f));
            Corp("atlas", new Color(0.85f, 0.25f, 0.25f));
            Corp("federation", new Color(0.9f, 0.9f, 0.95f));
            foreach (var old in new[] { "Station_aster_orbital", "Station_venera_hub" })
                AssetDatabase.DeleteAsset($"{WorldData}/{old}.asset");
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

            // Ordem entre paineis diferentes e do PanelSettings: HUD embaixo, terminal/menus por cima.
            PanelSettings P(string name, bool screenSpace, int order)
            {
                var p = Asset<PanelSettings>($"{UiDir}/{name}.asset");
                p.themeStyleSheet = theme;
                p.sortingOrder = order;
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

            return new Panels { screen = P("Panel_Screen", true, 0), terminal = P("Panel_Terminal", true, 10), cockpit = P("Panel_Cockpit", false, 0) };
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
            boot.resources = Goods.Select(k => res[k]).ToList();
            boot.smallTraders = new[] { "typec", "horder" }
                .Select(id => AssetDatabase.LoadAssetAtPath<ShipModelDef>($"{Root}/Data/Ships/Model_{id}.asset")).Where(m => m).ToList();
            boot.mediumTrader = AssetDatabase.LoadAssetAtPath<ShipModelDef>($"{Root}/Data/Ships/Model_hulker.asset");
            boot.corporations = new[] { "nova", "magna", "atlas", "federation" }
                .Select(id => AssetDatabase.LoadAssetAtPath<CorporationDef>($"{WorldData}/Corp_{id}.asset")).Where(c => c).ToList();
            boot.planetMaterial = m.planet; boot.sunMaterial = m.sun; boot.hullMaterial = m.hull; boot.lightMaterial = m.light;
            boot.asteroidMaterial = m.asteroid; boot.gateMaterial = m.gate; boot.portalMaterial = m.portal;
            boot.beamMaterial = m.beam; boot.starMaterial = m.star; boot.dustMaterial = m.dust;
            boot.screenPanel = panels.screen; boot.terminalPanel = panels.terminal; boot.cockpitPanelBase = panels.cockpit;

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.08f, 0.09f, 0.11f);
            RenderSettings.skybox = null;

            EditorSceneManager.SaveScene(scene, ScenePath);
        }

        // Menu principal: camera escura com estrelas e o UIDocument do menu. Abre primeiro no build.
        static void BuildMenuScene(Mats m, Panels panels)
        {
            // Trocar de cena descarrega os assets da Slice: guardar os caminhos e recarregar depois.
            string panelPath = AssetDatabase.GetAssetPath(panels.terminal);
            string starPath = AssetDatabase.GetAssetPath(m.star);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var panel = AssetDatabase.LoadAssetAtPath<PanelSettings>(panelPath);
            var star = AssetDatabase.LoadAssetAtPath<Material>(starPath);
            var cam = new GameObject("Main Camera").AddComponent<Camera>();
            cam.tag = "MainCamera";
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.005f, 0.008f, 0.015f);
            cam.farClipPlane = 100000f;
            cam.gameObject.AddComponent<AudioListener>();
            new GameObject("StarField").AddComponent<StarField>().Material = star;

            var menu = new GameObject("MainMenu");
            menu.SetActive(false);
            var doc = menu.AddComponent<UIDocument>();
            doc.panelSettings = panel;
            menu.AddComponent<MainMenu>();
            if (!panel) Debug.LogError("[PulseFactor] Menu principal sem PanelSettings: " + panelPath);
            menu.SetActive(true);
            EditorSceneManager.SaveScene(scene, MenuScenePath);

            var scenes = EditorBuildSettings.scenes.Where(s => s.path != ScenePath && s.path != MenuScenePath).ToList();
            scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            scenes.Insert(0, new EditorBuildSettingsScene(MenuScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
            EditorSceneManager.OpenScene(MenuScenePath);
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
