using UnityEngine;

namespace PulseFactor
{
    // Estacao industrial da Magna Hauling Co. (referencia: plataforma de carga com conteineres,
    // tanques, guindastes, hangar aberto, torres de controle e plataformas nas pontas).
    // 7 docas: 01-04 P nas pontas, 05-06 M e 07 G no topo. Estatica.
    public static class IndustrialStationBuilder
    {
        const float Length = 560f, Width = 300f, Height = 60f;

        static Material hullDark, hullMid, yellow, orange, steel, windows, panel, glass;
        static Material[] containerMats;
        static readonly Color[] containerColors =
        {
            new(0.85f, 0.45f, 0.15f), new(0.55f, 0.57f, 0.6f), new(0.9f, 0.7f, 0.15f), new(0.35f, 0.38f, 0.42f), new(0.7f, 0.3f, 0.2f),
        };

        public static Station Build(StationDef def, GameConfig config, Reputation reputation, WorldPosition position, Material hull)
        {
            CreateMaterials(hull);
            var root = new GameObject("Industrial_" + def.id);
            var anchor = root.AddComponent<WorldAnchor>();
            var station = root.AddComponent<Station>();
            var t = root.transform;
            float top = Height / 2f;

            // Casco principal e hangar aberto no meio.
            PadBuilder.Part(t, PrimitiveType.Cube, "Hull", Vector3.zero, new Vector3(Length, Height, Width), hullDark);
            PadBuilder.Part(t, PrimitiveType.Cube, "HangarFloor", new Vector3(0, top - 6f, 0), new Vector3(220, 4, 150), steel);
            PadBuilder.Part(t, PrimitiveType.Cube, "HangarWallN", new Vector3(0, top + 18f, 78), new Vector3(230, 40, 6), hullMid);
            PadBuilder.Part(t, PrimitiveType.Cube, "HangarWallS", new Vector3(0, top + 18f, -78), new Vector3(230, 40, 6), hullMid);
            for (int i = 0; i < 5; i++)
                PadBuilder.Part(t, PrimitiveType.Cube, "Truss" + i, new Vector3(-100 + i * 50, top + 38f, 0), new Vector3(4, 4, 160), orange);
            PadBuilder.Part(t, PrimitiveType.Cube, "HangarLights", new Vector3(0, top + 10f, 74.5f), new Vector3(200, 3, 1), windows);

            // Faixas amarelas de casco e janelas iluminadas.
            PadBuilder.Part(t, PrimitiveType.Cube, "StripeN", new Vector3(0, 0, Width / 2f + 0.5f), new Vector3(Length * 0.9f, 6, 1), yellow);
            PadBuilder.Part(t, PrimitiveType.Cube, "StripeS", new Vector3(0, 0, -Width / 2f - 0.5f), new Vector3(Length * 0.9f, 6, 1), yellow);
            PadBuilder.Part(t, PrimitiveType.Cube, "WindowsN", new Vector3(0, -14, Width / 2f + 0.6f), new Vector3(Length * 0.8f, 3, 0.6f), windows);
            PadBuilder.Part(t, PrimitiveType.Cube, "WindowsS", new Vector3(0, -14, -Width / 2f - 0.6f), new Vector3(Length * 0.8f, 3, 0.6f), windows);

            // Pilhas de conteineres nas duas pontas.
            var rng = new System.Random(11);
            ContainerStack(t, new Vector3(-Length / 2f + 55, top, 0), rng);
            ContainerStack(t, new Vector3(Length / 2f - 55, top, 70), rng);

            // Tanques, torres de controle, paineis solares e antena.
            for (int i = 0; i < 3; i++)
                PadBuilder.Part(t, PrimitiveType.Capsule, "Tank" + i, new Vector3(150 + i * 24, top + 22f, -90), new Vector3(20, 22, 20), steel);
            Tower(t, new Vector3(-150, top, -110));
            Tower(t, new Vector3(190, top, 110));
            for (int i = 0; i < 4; i++)
                PadBuilder.Part(t, PrimitiveType.Cube, "Solar" + i, new Vector3(60 + (i % 2) * 40, top + 6f, 110 + (i / 2) * 26), new Vector3(36, 1, 22), panel, new Vector3(20, 0, 0));
            PadBuilder.Part(t, PrimitiveType.Cylinder, "DishPost", new Vector3(-200, top + 14f, 110), new Vector3(4, 14, 4), steel);
            PadBuilder.Part(t, PrimitiveType.Sphere, "Dish", new Vector3(-200, top + 34f, 110), new Vector3(40, 8, 40), hullMid, new Vector3(-35, 0, 0));
            PadBuilder.Part(t, PrimitiveType.Sphere, "Dome", new Vector3(110, top + 4f, -20), new Vector3(40, 30, 40), glass);

            // Plataformas: 01-04 P em bracos para fora, 05-06 M e 07 G no topo.
            var docks = new Dock[7];
            var pSpots = new[]
            {
                new Vector3(-Length / 2f - 40, 0, -100), new Vector3(-Length / 2f - 40, 0, 100),
                new Vector3(Length / 2f + 40, 0, -100), new Vector3(Length / 2f + 40, 0, 100),
            };
            for (int i = 0; i < 4; i++)
            {
                var spot = pSpots[i];
                float arm = Mathf.Sign(spot.x);
                PadBuilder.Part(t, PrimitiveType.Cube, "PadArm" + i, new Vector3(spot.x - arm * 30f, -8f, spot.z), new Vector3(40, 10, 16), hullMid);
                docks[i] = PadBuilder.Build(t, i, DockSize.P, spot, 0f, 50f, hull);
            }
            docks[4] = PadBuilder.Build(t, 4, DockSize.M, new Vector3(-150, top + 2f, 60), 0f, 60f, hull);
            docks[5] = PadBuilder.Build(t, 5, DockSize.M, new Vector3(-80, top + 2f, -115), 0f, 60f, hull);
            docks[6] = PadBuilder.Build(t, 6, DockSize.G, new Vector3(200, top + 2f, -10), 0f, 82f, hull);

            var flood = new Color(1f, 0.85f, 0.55f);
            PadBuilder.AddLight(t, new Vector3(0, top + 70f, 0), flood, 420f, 3.5f);
            PadBuilder.AddLight(t, new Vector3(-Length / 2f, 60f, 0), flood, 380f, 3f);
            PadBuilder.AddLight(t, new Vector3(Length / 2f, 60f, 0), flood, 380f, 3f);
            PadBuilder.AddLight(t, new Vector3(150, top + 80f, -90), new Color(0.8f, 0.9f, 1f), 300f, 2.5f);

            station.Initialize(def, config, docks, reputation);
            anchor.SetWorld(position);
            return station;
        }

        static void ContainerStack(Transform t, Vector3 basePos, System.Random rng)
        {
            const float cw = 16f, ch = 9f, cl = 30f;
            for (int x = 0; x < 3; x++)
            for (int z = 0; z < 4; z++)
            {
                int levels = 1 + rng.Next(4);
                for (int y = 0; y < levels; y++)
                {
                    var m = containerMats[rng.Next(containerMats.Length)];
                    PadBuilder.Part(t, PrimitiveType.Cube, "Container",
                        basePos + new Vector3((x - 1) * (cw + 1), ch / 2f + y * (ch + 0.5f), (z - 1.5f) * (cl + 1)),
                        new Vector3(cw, ch, cl), m);
                }
            }
        }

        static void Tower(Transform t, Vector3 basePos)
        {
            PadBuilder.Part(t, PrimitiveType.Cube, "Tower", basePos + Vector3.up * 30f, new Vector3(30, 60, 30), hullMid);
            PadBuilder.Part(t, PrimitiveType.Cube, "TowerWindows", basePos + Vector3.up * 52f, new Vector3(31, 6, 31), windows);
            PadBuilder.Part(t, PrimitiveType.Sphere, "TowerDome", basePos + Vector3.up * 62f, new Vector3(20, 14, 20), windows);
            PadBuilder.Part(t, PrimitiveType.Cylinder, "TowerMast", basePos + Vector3.up * 80f, new Vector3(1.5f, 12, 1.5f), steel);
        }

        static void CreateMaterials(Material hull)
        {
            if (hullDark) return;
            hullDark = PadBuilder.Tint(hull, new Color(0.24f, 0.26f, 0.3f));
            hullMid = PadBuilder.Tint(hull, new Color(0.38f, 0.4f, 0.44f));
            steel = PadBuilder.Tint(hull, new Color(0.6f, 0.62f, 0.66f));
            yellow = PadBuilder.Tint(hull, new Color(0.95f, 0.72f, 0.12f));
            orange = PadBuilder.Tint(hull, new Color(0.95f, 0.5f, 0.12f));
            panel = PadBuilder.Tint(hull, new Color(0.18f, 0.25f, 0.5f));
            windows = PadBuilder.Emissive(new Color(1f, 0.85f, 0.45f), 2.5f);
            glass = PadBuilder.Transparent(new Color(1f, 0.9f, 0.6f, 0.45f));
            containerMats = System.Array.ConvertAll(containerColors, c => PadBuilder.Tint(hull, c));
        }
    }
}
