using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace PulseFactor.EditorTools
{
    // Gera os blocos provisorios (primitivas) das 6 naves e todos os dados de naves.
    // Idempotente: rodar de novo sobrescreve. Convencoes de sockets: ver plano, Task 2.
    public static class PrimitiveBlockFactory
    {
        public const string ExteriorCockpitLayer = "ShipExteriorCockpit";

        const string DataRoot = "Assets/_Project/Data";
        const string ShipsData = DataRoot + "/Ships";
        const string BlocksData = ShipsData + "/Blocks";
        const string ResourcesData = DataRoot + "/Resources";
        const string PrefabRoot = "Assets/_Project/Prefabs/Blocks";
        const string MaterialRoot = "Assets/_Project/Materials";

        static Material paintMat, glassMat, interiorMat, glowMat, screenMat;

        enum Ch { Primary = 0, Secondary = 1, Accent = 2, Glass = 10, Interior = 11, Glow = 12, Screen = 13 }

        [MenuItem("PulseFactor/Generate Primitive Blocks")]
        public static void Generate()
        {
            EnsureFolders();
            EnsureLayer(ExteriorCockpitLayer);
            CreateMaterials();

            var standard = Hull("hull_standard", 100);
            var military = Hull("hull_military", 200);
            var scout = Class(ShipClass.Scout, 40, 220, 90, 50, 1, standard);
            var cargo = Class(ShipClass.Cargo, 15, 130, 45, 500, 2, standard);
            var assault = Class(ShipClass.Assault, 30, 180, 75, 10, 3, military);

            var razzor = Manufacturer("razzor", new Color(0.42f, 0.46f, 0.50f), new Color(0.30f, 0.36f, 0.22f), new Color(1f, 0.45f, 0.1f));
            var cyber = Manufacturer("cybertech", new Color(0.15f, 0.16f, 0.18f), new Color(0.85f, 0.90f, 0.95f), new Color(0f, 0.9f, 1f));
            var hauling = Manufacturer("masterhauling", new Color(0.92f, 0.92f, 0.90f), new Color(0.12f, 0.20f, 0.35f), new Color(1f, 0.8f, 0.1f));

            Model("crusader", hauling, scout, false, BuildCrusader);
            Model("cruise", razzor, scout, true, BuildCruise);
            Model("rapidfire", razzor, assault, false, BuildRapidFire);
            Model("typea", cyber, assault, false, BuildTypeA);
            Model("typec", cyber, cargo, true, BuildTypeC);
            Model("horder", hauling, cargo, true, BuildHorder);

            // Primeira nave M: Hulker (Master Hauling), cargueira pesada sem asas. So NPC por enquanto.
            var reinforced = Hull("hull_reinforced", 200);
            var cargoM = Class(ShipClass.Cargo, 40, 300, 35, 600, 3, reinforced, "Cargo_M");
            cargoM.dockSize = DockSize.M;
            cargoM.boostCapacity = 200f;
            cargoM.scannerRange = 6000;
            EditorUtility.SetDirty(cargoM);
            Model("hulker", hauling, cargoM, false, BuildHulker, withWings: false);

            Resource("iron", 10); Resource("ice", 8); Resource("coal", 12);
            Resource("copper", 25); Resource("titanium", 60); Resource("cobalt", 80);
            // Produtos de comercio entre estacoes (fase 2E).
            Resource("medical", 45); Resource("construction", 30); Resource("ship_parts", 90);
            Resource("scrap", 6); Resource("rations", 20); Resource("processors", 120);
            Resource("energy_cells", 55); Resource("solid_fuel", 25); Resource("alloys", 70); Resource("water", 12);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[PulseFactor] Blocos primitivos e dados de naves gerados.");
        }

        // ---------- Modelos ----------

        delegate void PartBuilder(BlockType type, Transform root);

        static void Model(string id, ManufacturerDef maker, ShipClassDef classDef, bool multiclass, PartBuilder build, bool withWings = true)
        {
            var model = Asset<ShipModelDef>($"{ShipsData}/Model_{id}.asset");
            model.id = id;
            model.cockpit = Block(id, BlockType.Cockpit, maker, classDef, multiclass, build);
            model.body = Block(id, BlockType.Body, maker, classDef, multiclass, build);
            model.tail = Block(id, BlockType.Tail, maker, classDef, multiclass, build);
            model.wing = withWings ? Block(id, BlockType.Wing, maker, classDef, multiclass, build) : null;
            EditorUtility.SetDirty(model);
        }

        static BlockDef Block(string modelId, BlockType type, ManufacturerDef maker, ShipClassDef classDef, bool multiclass, PartBuilder build)
        {
            string name = $"{modelId}_{type.ToString().ToLower()}";
            var root = new GameObject(name);
            build(type, root.transform);
            if (type != BlockType.Body) Empty(root.transform, "Socket_Attach", Vector3.zero);

            string prefabPath = $"{PrefabRoot}/{name}.prefab";
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);

            var def = Asset<BlockDef>($"{BlocksData}/Block_{name}.asset");
            def.id = name;
            def.type = type;
            def.manufacturer = maker;
            def.originClass = classDef.shipClass;
            def.multiclass = multiclass;
            def.prefab = prefab;
            def.classDef = type == BlockType.Body ? classDef : null;
            EditorUtility.SetDirty(def);
            return def;
        }

        // Cada builder recebe o tipo e monta a geometria a partir do socket de encaixe (origem):
        // cockpit cresce para +z, cauda para -z, asa (esquerda) para -x. O corpo fica centrado.

        static void BuildCrusader(BlockType t, Transform r)
        {
            switch (t)
            {
                case BlockType.Cockpit:
                    Cockpit(r, length: 4f, width: 4f, height: 3.6f, eyeZ: 2f, glass: g =>
                    {
                        Prim(g, PrimitiveType.Sphere, "Dome", new Vector3(0, 0.2f, 2f), new Vector3(4f, 3.6f, 4f), Ch.Glass);
                    }, shell: s =>
                    {
                        Prim(s, PrimitiveType.Cylinder, "Ring", new Vector3(0, -0.6f, 2f), new Vector3(4.3f, 0.25f, 4.3f), Ch.Accent);
                        Prim(s, PrimitiveType.Cylinder, "Base", new Vector3(0, -1.2f, 2f), new Vector3(3.6f, 0.5f, 3.6f), Ch.Primary);
                        Prim(s, PrimitiveType.Cylinder, "Antenna", new Vector3(0.6f, 2.4f, 2f), new Vector3(0.08f, 0.6f, 0.08f), Ch.Secondary);
                    });
                    break;
                case BlockType.Body:
                    Body(r, length: 8f, width: 3.2f, wingX: 1.6f, wingZ: -1f);
                    Prim(r, PrimitiveType.Cylinder, "Hull", Vector3.zero, new Vector3(3.2f, 4f, 3.2f), Ch.Primary, rot: new Vector3(90, 0, 0));
                    Prim(r, PrimitiveType.Cylinder, "CapFront", new Vector3(0, 0, 3.6f), new Vector3(3.4f, 0.4f, 3.4f), Ch.Accent, rot: new Vector3(90, 0, 0));
                    Prim(r, PrimitiveType.Cylinder, "CapBack", new Vector3(0, 0, -3.6f), new Vector3(3.4f, 0.4f, 3.4f), Ch.Accent, rot: new Vector3(90, 0, 0));
                    Prim(r, PrimitiveType.Cylinder, "Dish", new Vector3(0, 2.1f, 0.5f), new Vector3(1.8f, 0.08f, 1.8f), Ch.Secondary, rot: new Vector3(-20, 0, 0));
                    Prim(r, PrimitiveType.Capsule, "TankL", new Vector3(-1.7f, 0.6f, 0), new Vector3(0.7f, 2f, 0.7f), Ch.Secondary, rot: new Vector3(90, 0, 0));
                    Prim(r, PrimitiveType.Capsule, "TankR", new Vector3(1.7f, 0.6f, 0), new Vector3(0.7f, 2f, 0.7f), Ch.Secondary, rot: new Vector3(90, 0, 0));
                    break;
                case BlockType.Tail:
                    Prim(r, PrimitiveType.Cube, "Block", new Vector3(0, 0, -1.5f), new Vector3(3f, 2.4f, 3f), Ch.Primary);
                    Prim(r, PrimitiveType.Cube, "Stripe", new Vector3(0, 1.25f, -1.5f), new Vector3(3.05f, 0.2f, 3.05f), Ch.Accent);
                    Nozzle(r, new Vector3(-0.7f, 0, -3.2f), 1f);
                    Nozzle(r, new Vector3(0.7f, 0, -3.2f), 1f);
                    break;
                case BlockType.Wing:
                    Prim(r, PrimitiveType.Cube, "Truss", new Vector3(-3f, 0, 0), new Vector3(6f, 0.3f, 0.5f), Ch.Primary);
                    Prim(r, PrimitiveType.Cube, "Solar", new Vector3(-3.6f, 0.05f, 0), new Vector3(4f, 0.08f, 2.2f), Ch.Secondary);
                    break;
            }
        }

        static void BuildCruise(BlockType t, Transform r)
        {
            switch (t)
            {
                case BlockType.Cockpit:
                    Cockpit(r, length: 3.4f, width: 3.2f, height: 3f, eyeZ: 1.7f, glass: g =>
                    {
                        Prim(g, PrimitiveType.Sphere, "Dome", new Vector3(0, 0.3f, 1.8f), new Vector3(3f, 2.6f, 3f), Ch.Glass);
                    }, shell: s =>
                    {
                        Prim(s, PrimitiveType.Sphere, "Lower", new Vector3(0, -0.5f, 1.6f), new Vector3(3.2f, 2f, 3.2f), Ch.Primary);
                    });
                    break;
                case BlockType.Body:
                    Body(r, length: 9f, width: 3f, wingX: 1.5f, wingZ: -2.5f);
                    Prim(r, PrimitiveType.Cube, "Hull", Vector3.zero, new Vector3(3f, 2.4f, 9f), Ch.Primary);
                    Prim(r, PrimitiveType.Cube, "Panel", new Vector3(0, 1.25f, 0.5f), new Vector3(2.2f, 0.15f, 5f), Ch.Secondary);
                    Prim(r, PrimitiveType.Capsule, "TankL", new Vector3(-1.7f, 0.4f, 0.5f), new Vector3(0.8f, 1.8f, 0.8f), Ch.Accent, rot: new Vector3(90, 0, 0));
                    Prim(r, PrimitiveType.Capsule, "TankR", new Vector3(1.7f, 0.4f, 0.5f), new Vector3(0.8f, 1.8f, 0.8f), Ch.Accent, rot: new Vector3(90, 0, 0));
                    break;
                case BlockType.Tail:
                    Prim(r, PrimitiveType.Cylinder, "Engine", new Vector3(0, 0, -1.8f), new Vector3(2.4f, 1.8f, 2.4f), Ch.Primary, rot: new Vector3(90, 0, 0));
                    Prim(r, PrimitiveType.Cube, "FinL", new Vector3(-2f, 0, -2.4f), new Vector3(2.4f, 0.15f, 1.2f), Ch.Secondary);
                    Prim(r, PrimitiveType.Cube, "FinR", new Vector3(2f, 0, -2.4f), new Vector3(2.4f, 0.15f, 1.2f), Ch.Secondary);
                    Nozzle(r, new Vector3(0, 0, -3.8f), 1.8f);
                    break;
                case BlockType.Wing:
                    Prim(r, PrimitiveType.Cube, "Plate", new Vector3(-2f, 0, 0), new Vector3(4f, 0.2f, 2.4f), Ch.Primary);
                    Nacelle(r, new Vector3(-2.6f, 0.3f, 0.3f), 0.9f, 3f);
                    break;
            }
        }

        static void BuildRapidFire(BlockType t, Transform r)
        {
            switch (t)
            {
                case BlockType.Cockpit:
                    Cockpit(r, length: 5f, width: 2.6f, height: 2f, eyeZ: 1.6f, glass: g =>
                    {
                        Prim(g, PrimitiveType.Cube, "Canopy", new Vector3(0, 0.95f, 1.6f), new Vector3(1.2f, 0.5f, 1.8f), Ch.Glass);
                    }, shell: s =>
                    {
                        Prim(s, PrimitiveType.Cube, "Nose", new Vector3(0, 0, 2.5f), new Vector3(2.6f, 1.6f, 5f), Ch.Primary);
                        Prim(s, PrimitiveType.Cube, "Tip", new Vector3(0, -0.2f, 5.4f), new Vector3(1.2f, 0.8f, 1.6f), Ch.Secondary, rot: new Vector3(0, 45, 0));
                    });
                    break;
                case BlockType.Body:
                    Body(r, length: 9f, width: 5f, wingX: 2.5f, wingZ: -1.5f);
                    Prim(r, PrimitiveType.Cube, "Hull", Vector3.zero, new Vector3(5f, 2f, 9f), Ch.Primary);
                    Prim(r, PrimitiveType.Cube, "Plate", new Vector3(0, 1.1f, 0), new Vector3(3.6f, 0.4f, 6f), Ch.Secondary);
                    Prim(r, PrimitiveType.Cube, "IntakeL", new Vector3(-2.6f, -0.2f, 1.5f), new Vector3(0.4f, 1f, 3f), Ch.Accent);
                    Prim(r, PrimitiveType.Cube, "IntakeR", new Vector3(2.6f, -0.2f, 1.5f), new Vector3(0.4f, 1f, 3f), Ch.Accent);
                    break;
                case BlockType.Tail:
                    Prim(r, PrimitiveType.Cube, "Frame", new Vector3(0, 0, -1.5f), new Vector3(4f, 2.6f, 3f), Ch.Primary);
                    Nozzle(r, new Vector3(-1.1f, -0.5f, -3.2f), 1.3f);
                    Nozzle(r, new Vector3(1.1f, -0.5f, -3.2f), 1.3f);
                    Nozzle(r, new Vector3(0, 0.8f, -3.2f), 1.3f);
                    Prim(r, PrimitiveType.Cube, "FinV", new Vector3(0, 2f, -2f), new Vector3(0.2f, 2f, 1.8f), Ch.Accent);
                    break;
                case BlockType.Wing:
                    Prim(r, PrimitiveType.Cube, "Plate", new Vector3(-2.5f, 0, -0.5f), new Vector3(5f, 0.25f, 3f), Ch.Primary, rot: new Vector3(0, -15, 0));
                    Nacelle(r, new Vector3(-1.6f, 0, 0.4f), 1.1f, 3.4f);
                    Prim(r, PrimitiveType.Cube, "Tip", new Vector3(-5f, 0, -1.2f), new Vector3(0.6f, 0.3f, 1.2f), Ch.Accent);
                    break;
            }
        }

        static void BuildTypeA(BlockType t, Transform r)
        {
            switch (t)
            {
                case BlockType.Cockpit:
                    Cockpit(r, length: 4.6f, width: 3f, height: 2.4f, eyeZ: 1.8f, glass: g =>
                    {
                        Prim(g, PrimitiveType.Cube, "Canopy", new Vector3(0, 1f, 2f), new Vector3(1.8f, 0.8f, 3.2f), Ch.Glass);
                    }, shell: s =>
                    {
                        Prim(s, PrimitiveType.Cube, "Nose", new Vector3(0, 0, 2.3f), new Vector3(3f, 1.8f, 4.6f), Ch.Primary);
                        Prim(s, PrimitiveType.Cube, "Chin", new Vector3(0, -0.8f, 4.2f), new Vector3(2f, 0.6f, 1.2f), Ch.Secondary);
                    });
                    break;
                case BlockType.Body:
                    Body(r, length: 9f, width: 4.4f, wingX: 2.2f, wingZ: -1.5f);
                    Prim(r, PrimitiveType.Cube, "Hull", Vector3.zero, new Vector3(4.4f, 2.2f, 9f), Ch.Primary);
                    Prim(r, PrimitiveType.Cube, "Line", new Vector3(0, 1.12f, 0), new Vector3(0.2f, 0.05f, 8f), Ch.Accent);
                    Prim(r, PrimitiveType.Cube, "DorsalFin", new Vector3(0, 1.8f, -2.5f), new Vector3(0.25f, 1.4f, 2.2f), Ch.Secondary);
                    break;
                case BlockType.Tail:
                    Prim(r, PrimitiveType.Cube, "EngineL", new Vector3(-1f, 0, -1.8f), new Vector3(1.8f, 1.6f, 3.6f), Ch.Primary);
                    Prim(r, PrimitiveType.Cube, "EngineR", new Vector3(1f, 0, -1.8f), new Vector3(1.8f, 1.6f, 3.6f), Ch.Primary);
                    Glow(r, "Thruster_L", new Vector3(-1f, 0, -3.65f), new Vector3(1.4f, 1.2f, 0.1f));
                    Glow(r, "Thruster_R", new Vector3(1f, 0, -3.65f), new Vector3(1.4f, 1.2f, 0.1f));
                    Prim(r, PrimitiveType.Cube, "FinVL", new Vector3(-2f, 1.2f, -2.6f), new Vector3(0.2f, 1.8f, 1.6f), Ch.Secondary);
                    Prim(r, PrimitiveType.Cube, "FinVR", new Vector3(2f, 1.2f, -2.6f), new Vector3(0.2f, 1.8f, 1.6f), Ch.Secondary);
                    break;
                case BlockType.Wing:
                    Prim(r, PrimitiveType.Cube, "Plate", new Vector3(-2.4f, 0, -0.6f), new Vector3(4.8f, 0.2f, 2.6f), Ch.Primary, rot: new Vector3(0, -20, 0));
                    Nacelle(r, new Vector3(-1.4f, 0, 0.3f), 1f, 3f);
                    Prim(r, PrimitiveType.Cube, "TipFin", new Vector3(-4.8f, 0.7f, -1.4f), new Vector3(0.2f, 1.6f, 1.4f), Ch.Accent);
                    break;
            }
        }

        static void BuildTypeC(BlockType t, Transform r)
        {
            switch (t)
            {
                case BlockType.Cockpit:
                    Cockpit(r, length: 3.6f, width: 4f, height: 3.4f, eyeZ: 1.6f, glass: g =>
                    {
                        Prim(g, PrimitiveType.Cube, "Windshield", new Vector3(0, 0.6f, 3.4f), new Vector3(3.6f, 1.8f, 0.15f), Ch.Glass, rot: new Vector3(-25, 0, 0));
                    }, shell: s =>
                    {
                        Prim(s, PrimitiveType.Cube, "Cab", new Vector3(0, -0.4f, 1.6f), new Vector3(4f, 1.6f, 3.2f), Ch.Primary);
                        Prim(s, PrimitiveType.Cube, "Roof", new Vector3(0, 1.55f, 1.4f), new Vector3(4f, 0.3f, 2.8f), Ch.Secondary);
                        Prim(s, PrimitiveType.Cube, "PillarL", new Vector3(-1.9f, 0.6f, 3.1f), new Vector3(0.2f, 1.9f, 0.3f), Ch.Accent);
                        Prim(s, PrimitiveType.Cube, "PillarR", new Vector3(1.9f, 0.6f, 3.1f), new Vector3(0.2f, 1.9f, 0.3f), Ch.Accent);
                    });
                    break;
                case BlockType.Body:
                    Body(r, length: 10f, width: 6f, wingX: 3f, wingZ: -2f);
                    Prim(r, PrimitiveType.Cube, "Container", Vector3.zero, new Vector3(6f, 6f, 10f), Ch.Primary);
                    Prim(r, PrimitiveType.Cube, "FrameFront", new Vector3(0, 0, 4.8f), new Vector3(6.3f, 6.3f, 0.5f), Ch.Secondary);
                    Prim(r, PrimitiveType.Cube, "FrameBack", new Vector3(0, 0, -4.8f), new Vector3(6.3f, 6.3f, 0.5f), Ch.Secondary);
                    Prim(r, PrimitiveType.Cube, "Line", new Vector3(3.02f, 0, 0), new Vector3(0.05f, 0.2f, 9f), Ch.Accent);
                    break;
                case BlockType.Tail:
                    Prim(r, PrimitiveType.Cube, "Block", new Vector3(0, 0, -1.5f), new Vector3(5f, 5f, 3f), Ch.Primary);
                    Nozzle(r, new Vector3(-1.2f, -1.2f, -3.2f), 1.6f);
                    Nozzle(r, new Vector3(1.2f, -1.2f, -3.2f), 1.6f);
                    Nozzle(r, new Vector3(-1.2f, 1.2f, -3.2f), 1.6f);
                    Nozzle(r, new Vector3(1.2f, 1.2f, -3.2f), 1.6f);
                    Prim(r, PrimitiveType.Cube, "FinV", new Vector3(0, 3.4f, -2f), new Vector3(0.25f, 1.8f, 2f), Ch.Accent);
                    break;
                case BlockType.Wing:
                    Prim(r, PrimitiveType.Cube, "Plate", new Vector3(-1.6f, 0, 0), new Vector3(3.2f, 0.3f, 2.2f), Ch.Primary);
                    Nacelle(r, new Vector3(-1.4f, 0.4f, 0f), 1f, 2.6f);
                    break;
            }
        }

        static void BuildHorder(BlockType t, Transform r)
        {
            switch (t)
            {
                case BlockType.Cockpit:
                    Cockpit(r, length: 3.8f, width: 4.2f, height: 3.4f, eyeZ: 1.6f, glass: g =>
                    {
                        Prim(g, PrimitiveType.Cube, "WindowL", new Vector3(-0.9f, 0.7f, 3.55f), new Vector3(1.4f, 0.9f, 0.15f), Ch.Glass, rot: new Vector3(-20, 0, 0));
                        Prim(g, PrimitiveType.Cube, "WindowR", new Vector3(0.9f, 0.7f, 3.55f), new Vector3(1.4f, 0.9f, 0.15f), Ch.Glass, rot: new Vector3(-20, 0, 0));
                    }, shell: s =>
                    {
                        Prim(s, PrimitiveType.Cube, "Cab", new Vector3(0, -0.3f, 1.7f), new Vector3(4.2f, 1.8f, 3.4f), Ch.Secondary);
                        Prim(s, PrimitiveType.Cube, "Roof", new Vector3(0, 1.4f, 1.6f), new Vector3(4.2f, 0.6f, 3.2f), Ch.Secondary);
                        Prim(s, PrimitiveType.Cube, "Grille", new Vector3(0, -0.6f, 3.45f), new Vector3(3f, 0.8f, 0.2f), Ch.Accent);
                    });
                    break;
                case BlockType.Body:
                    Body(r, length: 10f, width: 6f, wingX: 3f, wingZ: -2f);
                    Prim(r, PrimitiveType.Cube, "Container", Vector3.zero, new Vector3(6f, 6f, 10f), Ch.Secondary);
                    Prim(r, PrimitiveType.Cube, "Band", new Vector3(0, 0, -2.5f), new Vector3(6.3f, 6.3f, 1.2f), Ch.Accent);
                    for (int i = 0; i < 4; i++)
                    {
                        float x = (i % 2 == 0 ? -1 : 1) * 3f, y = (i < 2 ? -1 : 1) * 3f;
                        Prim(r, PrimitiveType.Cylinder, $"Edge{i}", new Vector3(x, y, 0), new Vector3(0.7f, 5f, 0.7f), Ch.Primary, rot: new Vector3(90, 0, 0));
                    }
                    break;
                case BlockType.Tail:
                    Prim(r, PrimitiveType.Cylinder, "Turbine", new Vector3(0, 0, -2f), new Vector3(4.6f, 2f, 4.6f), Ch.Secondary, rot: new Vector3(90, 0, 0));
                    Prim(r, PrimitiveType.Cylinder, "Lip", new Vector3(0, 0, -0.2f), new Vector3(4.8f, 0.25f, 4.8f), Ch.Accent, rot: new Vector3(90, 0, 0));
                    Prim(r, PrimitiveType.Cube, "FinL", new Vector3(-2.8f, 0, -2.6f), new Vector3(1.6f, 0.2f, 1.2f), Ch.Primary);
                    Prim(r, PrimitiveType.Cube, "FinR", new Vector3(2.8f, 0, -2.6f), new Vector3(1.6f, 0.2f, 1.2f), Ch.Primary);
                    Nozzle(r, new Vector3(0, 0, -4.1f), 3.6f);
                    break;
                case BlockType.Wing:
                    Prim(r, PrimitiveType.Cube, "Plate", new Vector3(-2f, -0.4f, -0.4f), new Vector3(4f, 0.25f, 2f), Ch.Secondary);
                    Prim(r, PrimitiveType.Cylinder, "Fan", new Vector3(-1.8f, 0.3f, 0.2f), new Vector3(1.8f, 1.8f, 1.8f), Ch.Secondary, rot: new Vector3(90, 0, 0));
                    Prim(r, PrimitiveType.Cylinder, "Intake", new Vector3(-1.8f, 0.3f, 2f), new Vector3(1.9f, 0.15f, 1.9f), Ch.Accent, rot: new Vector3(90, 0, 0));
                    Nozzle(r, new Vector3(-1.8f, 0.3f, -1.7f), 1.2f);
                    break;
            }
        }

        // Hulker: ~3 naves P de comprimento (~51 m) e ~2 de largura (~24 m). Cabine de caminhao com antena e prato,
        // corpo de carga com trelica no topo e tanques laranja nas laterais, cauda com 6 bocais e propulsores laterais.
        static void BuildHulker(BlockType t, Transform r)
        {
            switch (t)
            {
                case BlockType.Cockpit:
                    Cockpit(r, length: 10f, width: 12f, height: 9f, eyeZ: 5f, glass: g =>
                    {
                        Prim(g, PrimitiveType.Cube, "WindowL", new Vector3(-2.6f, 1.4f, 9.6f), new Vector3(4.6f, 2.6f, 0.2f), Ch.Glass, rot: new Vector3(-20, 0, 0));
                        Prim(g, PrimitiveType.Cube, "WindowR", new Vector3(2.6f, 1.4f, 9.6f), new Vector3(4.6f, 2.6f, 0.2f), Ch.Glass, rot: new Vector3(-20, 0, 0));
                    }, shell: s =>
                    {
                        Prim(s, PrimitiveType.Cube, "Cab", new Vector3(0, -0.5f, 5f), new Vector3(12f, 6.5f, 9.5f), Ch.Primary);
                        Prim(s, PrimitiveType.Cube, "Roof", new Vector3(0, 3.4f, 4.4f), new Vector3(11f, 1.6f, 7.5f), Ch.Accent);
                        Prim(s, PrimitiveType.Cube, "Grille", new Vector3(0, -2.4f, 9.9f), new Vector3(8f, 2.2f, 0.4f), Ch.Secondary);
                        Prim(s, PrimitiveType.Cube, "PodL", new Vector3(-7f, -0.6f, 3f), new Vector3(2.2f, 3f, 6f), Ch.Secondary);
                        Prim(s, PrimitiveType.Cube, "PodR", new Vector3(7f, -0.6f, 3f), new Vector3(2.2f, 3f, 6f), Ch.Secondary);
                        Prim(s, PrimitiveType.Cylinder, "Antenna", new Vector3(-3f, 6f, 3f), new Vector3(0.25f, 2.4f, 0.25f), Ch.Secondary);
                        Prim(s, PrimitiveType.Cylinder, "Dish", new Vector3(2f, 4.8f, 3f), new Vector3(2.6f, 0.15f, 2.6f), Ch.Secondary, rot: new Vector3(-30, 0, 0));
                    });
                    break;
                case BlockType.Body:
                    Body(r, length: 30f, width: 14f, wingX: 7f, wingZ: 0f);
                    Prim(r, PrimitiveType.Cube, "Frame", Vector3.zero, new Vector3(14f, 10f, 30f), Ch.Secondary);
                    Prim(r, PrimitiveType.Cube, "CornerF", new Vector3(0, 0, 14.4f), new Vector3(14.6f, 10.6f, 1.4f), Ch.Accent);
                    Prim(r, PrimitiveType.Cube, "CornerB", new Vector3(0, 0, -14.4f), new Vector3(14.6f, 10.6f, 1.4f), Ch.Accent);
                    for (int i = 0; i < 6; i++)
                        Prim(r, PrimitiveType.Cube, "Truss" + i, new Vector3(0, 5.6f, -12.5f + i * 5f), new Vector3(12.5f, 0.6f, 0.6f), Ch.Primary);
                    Prim(r, PrimitiveType.Cube, "TrussL", new Vector3(-6.2f, 5.6f, 0), new Vector3(0.6f, 0.6f, 28f), Ch.Primary);
                    Prim(r, PrimitiveType.Cube, "TrussR", new Vector3(6.2f, 5.6f, 0), new Vector3(0.6f, 0.6f, 28f), Ch.Primary);
                    for (int i = 0; i < 3; i++)
                    {
                        Prim(r, PrimitiveType.Capsule, "TankL" + i, new Vector3(-8.3f, -1.5f, -8f + i * 8f), new Vector3(3f, 3.4f, 3f), Ch.Accent, rot: new Vector3(90, 0, 0));
                        Prim(r, PrimitiveType.Capsule, "TankR" + i, new Vector3(8.3f, -1.5f, -8f + i * 8f), new Vector3(3f, 3.4f, 3f), Ch.Accent, rot: new Vector3(90, 0, 0));
                    }
                    break;
                case BlockType.Tail:
                    Prim(r, PrimitiveType.Cube, "Block", new Vector3(0, 0, -5f), new Vector3(14f, 11f, 10f), Ch.Primary);
                    Prim(r, PrimitiveType.Cube, "Stripe", new Vector3(0, 5.8f, -4f), new Vector3(8f, 0.8f, 6f), Ch.Accent);
                    for (int x = -1; x <= 1; x++)
                    for (int y = 0; y < 2; y++)
                        Nozzle(r, new Vector3(x * 4f, -2.2f + y * 4.4f, -10.3f), 3.4f);
                    foreach (int side in new[] { -1, 1 })
                    {
                        Prim(r, PrimitiveType.Cylinder, "Booster", new Vector3(side * 8.6f, 0, -6f), new Vector3(3f, 4f, 3f), Ch.Secondary, rot: new Vector3(90, 0, 0));
                        Nozzle(r, new Vector3(side * 8.6f, 0, -10.2f), 2.6f);
                        Prim(r, PrimitiveType.Cube, "Fin", new Vector3(side * 7.6f, -5.5f, -8f), new Vector3(0.5f, 3f, 4f), Ch.Accent);
                    }
                    break;
            }
        }

        // ---------- Pecas genericas ----------

        static void Body(Transform r, float length, float width, float wingX, float wingZ)
        {
            Empty(r, "Socket_Front", new Vector3(0, 0, length / 2f));
            Empty(r, "Socket_Back", new Vector3(0, 0, -length / 2f));
            Empty(r, "Socket_WingL", new Vector3(-wingX, 0, wingZ));
            Empty(r, "Socket_WingR", new Vector3(wingX, 0, wingZ));
            Empty(r, "WeaponMount_Body", Vector3.zero);
        }

        // Cockpit: casca externa (layer escondida na camera de cockpit), vidro compartilhado
        // (visto de dentro e de fora) e interior com EyePoint e ScreenMounts.
        static void Cockpit(Transform r, float length, float width, float height, float eyeZ,
            System.Action<Transform> glass, System.Action<Transform> shell)
        {
            var shellRoot = Empty(r, "Exterior", Vector3.zero);
            shell(shellRoot);
            SetLayer(shellRoot.gameObject, LayerMask.NameToLayer(ExteriorCockpitLayer));

            var glassRoot = Empty(r, "Glass", Vector3.zero);
            glass(glassRoot);

            // Interior: painel baixo a frente do piloto e 3 telas apoiadas nele, viradas para o olho.
            var interior = Empty(r, "Interior", Vector3.zero);
            float w = width * 0.8f;
            var eye = new Vector3(0, 0.1f, eyeZ - 0.4f);
            Prim(interior, PrimitiveType.Cube, "Dash", eye + new Vector3(0, -0.55f, 0.95f), new Vector3(w, 0.5f, 0.6f), Ch.Interior);
            Prim(interior, PrimitiveType.Cube, "FrameL", eye + new Vector3(-w / 2f, 0.2f, 0.95f), new Vector3(0.05f, 1f, 0.05f), Ch.Interior, rot: new Vector3(-20, 0, 0));
            Prim(interior, PrimitiveType.Cube, "FrameR", eye + new Vector3(w / 2f, 0.2f, 0.95f), new Vector3(0.05f, 1f, 0.05f), Ch.Interior, rot: new Vector3(-20, 0, 0));

            Empty(interior, "EyePoint", eye);
            // 3 embaixo (status, alvo, abas) e 2 em cima (hora, nome da nave). Proporcoes iguais as texturas.
            Screen(interior, "ScreenMount_Center", eye, eye + new Vector3(0, -0.24f, 0.74f), new Vector2(0.39f, 0.26f));
            Screen(interior, "ScreenMount_Left", eye, eye + new Vector3(-0.42f, -0.24f, 0.66f), new Vector2(0.3f, 0.225f));
            Screen(interior, "ScreenMount_Right", eye, eye + new Vector3(0.42f, -0.24f, 0.66f), new Vector2(0.3f, 0.225f));
            Screen(interior, "ScreenMount_TopLeft", eye, eye + new Vector3(-0.3f, 0.3f, 0.78f), new Vector2(0.24f, 0.09f));
            Screen(interior, "ScreenMount_TopRight", eye, eye + new Vector3(0.3f, 0.3f, 0.78f), new Vector2(0.24f, 0.09f));

            Empty(r, "WeaponMount_Cockpit", new Vector3(0, -height * 0.4f, length));
        }

        static void Screen(Transform parent, string name, Vector3 eye, Vector3 pos, Vector2 size)
        {
            var mount = Empty(parent, name, pos);
            mount.localRotation = Quaternion.LookRotation(pos - eye, Vector3.up); // a face do quad fica para o olho
            mount.localScale = new Vector3(size.x, size.y, 1f);
            Prim(mount, PrimitiveType.Quad, "Display", Vector3.zero, Vector3.one, Ch.Screen);
        }

        static void Nozzle(Transform r, Vector3 pos, float diameter)
        {
            Prim(r, PrimitiveType.Cylinder, "Nozzle", pos, new Vector3(diameter, 0.4f, diameter), Ch.Secondary, rot: new Vector3(90, 0, 0));
            Glow(r, "Thruster_" + r.childCount, pos + new Vector3(0, 0, -0.42f), new Vector3(diameter * 0.75f, diameter * 0.75f, 0.05f));
        }

        static void Nacelle(Transform r, Vector3 pos, float diameter, float length)
        {
            Prim(r, PrimitiveType.Cylinder, "Nacelle", pos, new Vector3(diameter, length / 2f, diameter), Ch.Secondary, rot: new Vector3(90, 0, 0));
            Prim(r, PrimitiveType.Cylinder, "Intake", pos + new Vector3(0, 0, length / 2f), new Vector3(diameter * 1.05f, 0.1f, diameter * 1.05f), Ch.Accent, rot: new Vector3(90, 0, 0));
            Glow(r, "Thruster_" + r.childCount, pos + new Vector3(0, 0, -length / 2f - 0.02f), new Vector3(diameter * 0.7f, diameter * 0.7f, 0.05f));
            Empty(r, "WeaponMount_" + r.childCount, pos + new Vector3(0, -diameter * 0.6f, 0));
        }

        static void Glow(Transform r, string name, Vector3 pos, Vector3 scale)
        {
            var t = Prim(r, PrimitiveType.Cylinder, name, pos, new Vector3(scale.x, scale.z, scale.y), Ch.Glow, rot: new Vector3(90, 0, 0));
            t.name = name.StartsWith("Thruster_") ? name : "Thruster_" + name;
        }

        static Transform Prim(Transform parent, PrimitiveType type, string name, Vector3 pos, Vector3 scale, Ch ch, Vector3 rot = default)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            var t = go.transform;
            t.SetParent(parent, false);
            t.localPosition = pos;
            t.localEulerAngles = rot;
            t.localScale = scale;
            var mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterial = ch switch
            {
                Ch.Glass => glassMat,
                Ch.Interior => interiorMat,
                Ch.Glow => glowMat,
                Ch.Screen => screenMat,
                _ => paintMat,
            };
            if ((int)ch <= 2) go.AddComponent<PaintableRegion>().channel = (int)ch;
            if (ch == Ch.Glass || ch == Ch.Glow || ch == Ch.Screen) mr.shadowCastingMode = ShadowCastingMode.Off;
            return t;
        }

        static Transform Empty(Transform parent, string name, Vector3 pos)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            t.localPosition = pos;
            return t;
        }

        static void SetLayer(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform c in go.transform) SetLayer(c.gameObject, layer);
        }

        // ---------- Dados ----------

        static HullTypeDef Hull(string id, int hp)
        {
            var h = Asset<HullTypeDef>($"{ShipsData}/Hull_{id}.asset");
            h.id = id; h.baseHp = hp;
            EditorUtility.SetDirty(h);
            return h;
        }

        static ShipClassDef Class(ShipClass c, float accel, float maxSpeed, float turn, int cargo, int weapons, HullTypeDef hull, string assetName = null)
        {
            var d = Asset<ShipClassDef>($"{ShipsData}/Class_{assetName ?? c.ToString()}.asset");
            d.shipClass = c; d.acceleration = accel; d.maxSpeed = maxSpeed; d.turnRateDeg = turn;
            d.cargoCapacity = cargo; d.weaponSlots = weapons; d.skillSlots = 1; d.startingHull = hull;
            d.fullCargoSpeedPenalty = 0.15f; d.releaseDecelFactor = 0.3f; d.cruiseAdjustRate = 60f;
            EditorUtility.SetDirty(d);
            return d;
        }

        static ManufacturerDef Manufacturer(string id, Color primary, Color secondary, Color accent)
        {
            var m = Asset<ManufacturerDef>($"{ShipsData}/Manufacturer_{id}.asset");
            m.id = id; m.primary = primary; m.secondary = secondary; m.accent = accent;
            EditorUtility.SetDirty(m);
            return m;
        }

        static void Resource(string id, int price)
        {
            var r = Asset<ResourceDef>($"{ResourcesData}/Resource_{id}.asset");
            r.id = id; r.basePrice = price;
            EditorUtility.SetDirty(r);
        }

        static T Asset<T>(string path) where T : ScriptableObject
        {
            var a = AssetDatabase.LoadAssetAtPath<T>(path);
            if (a != null) return a;
            a = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(a, path);
            return a;
        }

        // ---------- Materiais, pastas e layer ----------

        static void CreateMaterials()
        {
            var lit = Shader.Find("Universal Render Pipeline/Lit");
            var unlit = Shader.Find("Universal Render Pipeline/Unlit");

            paintMat = Mat("M_BlockPaint", lit, Color.white, m => { m.SetFloat("_Smoothness", 0.35f); });
            interiorMat = Mat("M_CockpitInterior", lit, new Color(0.12f, 0.13f, 0.15f), m => m.SetFloat("_Smoothness", 0.2f));
            screenMat = Mat("M_CockpitScreen", unlit, new Color(0.02f, 0.08f, 0.1f), null);
            glowMat = Mat("M_ThrusterGlow", unlit, new Color(0.4f, 0.8f, 1f) * 2.5f, null);
            glassMat = Mat("M_CockpitGlass", lit, new Color(0.55f, 0.8f, 0.9f, 0.06f), m =>
            {
                // Quase invisivel por dentro: sem brilho especular nem reflexo do ambiente.
                m.SetFloat("_SpecularHighlights", 0f);
                m.SetFloat("_EnvironmentReflections", 0f);
                m.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
                m.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");
                m.SetFloat("_Surface", 1f);      // transparente
                m.SetFloat("_Blend", 0f);
                m.SetFloat("_Cull", 0f);         // duas faces: visto de dentro e de fora
                m.SetFloat("_Smoothness", 0.5f);
                m.SetFloat("_ZWrite", 0f);
                m.SetOverrideTag("RenderType", "Transparent");
                m.renderQueue = (int)RenderQueue.Transparent;
                m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                m.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                m.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            });
        }

        static Material Mat(string name, Shader shader, Color color, System.Action<Material> setup)
        {
            string path = $"{MaterialRoot}/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(shader);
                AssetDatabase.CreateAsset(m, path);
            }
            m.shader = shader;
            m.SetColor("_BaseColor", color);
            setup?.Invoke(m);
            EditorUtility.SetDirty(m);
            return m;
        }

        static void EnsureFolders()
        {
            foreach (var p in new[] { BlocksData, ResourcesData, PrefabRoot, MaterialRoot })
                Directory.CreateDirectory(p);
            AssetDatabase.Refresh();
        }

        public static void EnsureLayer(string layerName)
        {
            if (LayerMask.NameToLayer(layerName) >= 0) return;
            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layers = tagManager.FindProperty("layers");
            for (int i = 8; i < layers.arraySize; i++)
            {
                var sp = layers.GetArrayElementAtIndex(i);
                if (string.IsNullOrEmpty(sp.stringValue))
                {
                    sp.stringValue = layerName;
                    tagManager.ApplyModifiedProperties();
                    return;
                }
            }
            Debug.LogError("[PulseFactor] Sem layer livre para " + layerName);
        }
    }
}
