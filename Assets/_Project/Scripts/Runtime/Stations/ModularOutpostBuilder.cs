using UnityEngine;

namespace PulseFactor
{
    // Outposts flutuantes (referencias refs/outpost_*): nucleo de modulos ligados por trelica,
    // 3 plataformas em bracos com passarelas. Variantes:
    //   Industrial — laranja enferrujado, guindaste, tanques e bocais de motor;
    //   Cientifico — branco e azul, domo de vidro e antenas parabolicas grandes;
    //   Militar    — verde-oliva, torretas, torre de radar e plataformas com borda vermelha.
    public static class ModularOutpostBuilder
    {
        public const float PadP = 44f, PadM = 58f, PadG = 78f;

        class Palette { public Material main, second, accent, dark, glass, panel, glow, rail; }

        static Palette industrial, science, military;

        public static Station Build(StationDef def, GameConfig config, Reputation reputation, WorldPosition position,
            Material hull, Economy economy = null)
        {
            var p = PaletteFor(def.style, hull);
            var root = new GameObject("Outpost_" + def.id);
            var anchor = root.AddComponent<WorldAnchor>();
            var station = root.AddComponent<Station>();
            var t = root.transform;

            Core(t, p);
            switch (def.style)
            {
                case StationStyle.ScienceOutpost: ScienceDetails(t, p); break;
                case StationStyle.MilitaryOutpost: MilitaryDetails(t, p); break;
                default: IndustrialDetails(t, p); break;
            }

            // Plataformas em 3 bracos (esquerda-frente, esquerda-tras, direita-tras), abaixo do nucleo.
            var sizes = def.dockSizes;
            var spots = new[] { new Vector3(-190, -30, 120), new Vector3(-210, -30, -110), new Vector3(70, -30, -200) };
            var docks = new Dock[Mathf.Min(sizes.Length, spots.Length)];
            for (int i = 0; i < docks.Length; i++)
            {
                float pad = sizes[i] switch { DockSize.P => PadP, DockSize.M => PadM, _ => PadG };
                docks[i] = Arm(t, i, sizes[i], spots[i], pad, p, hull, def.style == StationStyle.MilitaryOutpost);
            }

            // Luzes gerais (o outpost fica escuro no lado oposto ao sol).
            var warm = def.style == StationStyle.MilitaryOutpost ? new Color(1f, 0.55f, 0.45f) : new Color(1f, 0.82f, 0.6f);
            PadBuilder.AddLight(t, new Vector3(0, 140, 0), warm, 700f, 3f);
            PadBuilder.AddLight(t, new Vector3(-120, 60, 0), new Color(0.75f, 0.85f, 1f), 500f, 2.5f);
            PadBuilder.AddLight(t, new Vector3(120, 60, -60), warm, 500f, 2.5f);

            station.Initialize(def, config, docks, reputation, economy);
            anchor.SetWorld(position);
            return station;
        }

        // Nucleo comum: modulo central, viga de trelica, dois modulos laterais, tanques e paineis solares.
        static void Core(Transform t, Palette p)
        {
            Box(t, "CoreMain", new Vector3(0, 20, 0), new Vector3(110, 70, 80), p.main);
            Box(t, "CoreBand", new Vector3(0, 20, 0), new Vector3(112, 10, 82), p.second);
            Box(t, "CoreTop", new Vector3(0, 62, 0), new Vector3(70, 14, 50), p.second);
            for (int i = 0; i < 4; i++)
                Box(t, "Window" + i, new Vector3(-36 + i * 24, 34, 40.6f), new Vector3(14, 6, 0.8f), p.glow);

            // Viga de trelica atravessando o nucleo.
            Box(t, "TrussTop", new Vector3(-20, 40, 0), new Vector3(260, 4, 4), p.rail);
            Box(t, "TrussBottom", new Vector3(-20, 22, 0), new Vector3(260, 4, 4), p.rail);
            for (int i = 0; i < 12; i++)
                Box(t, "TrussBrace" + i, new Vector3(-145 + i * 22, 31, 0), new Vector3(3, 22, 3), p.rail, new Vector3(0, 0, i % 2 == 0 ? 35 : -35));

            // Modulos laterais.
            Box(t, "ModuleL", new Vector3(-120, 30, 0), new Vector3(60, 44, 54), p.main);
            Box(t, "ModuleLCap", new Vector3(-152, 30, 0), new Vector3(6, 36, 46), p.dark);
            Box(t, "ModuleR", new Vector3(110, 26, 40), new Vector3(70, 48, 60), p.main);
            Box(t, "ModuleRBand", new Vector3(110, 26, 40), new Vector3(72, 8, 62), p.accent);

            // Tanques cilindricos laranja (as tres referencias tem).
            for (int i = 0; i < 3; i++)
                Cyl(t, "Tank" + i, new Vector3(50 + i * 22, 8, -46), new Vector3(18, 16, 18), p.accent, new Vector3(90, 0, 0));

            // Paineis solares.
            for (int i = 0; i < 4; i++)
                Box(t, "Solar" + i, new Vector3(120 + (i % 2) * 30, 58, 50 + (i / 2) * 22 - 11), new Vector3(26, 1, 18), p.panel, new Vector3(20, 0, 0));
        }

        static void IndustrialDetails(Transform t, Palette p)
        {
            // Guindaste.
            Box(t, "CraneMast", new Vector3(20, 100, 20), new Vector3(5, 70, 5), p.accent);
            Box(t, "CraneArm", new Vector3(-5, 134, 20), new Vector3(60, 4, 4), p.accent);
            Box(t, "CraneHook", new Vector3(-32, 120, 20), new Vector3(2, 26, 2), p.dark);
            // Bocais de motor na ponta (como na referencia).
            Box(t, "EngineBlock", new Vector3(175, 26, 40), new Vector3(30, 40, 50), p.second);
            for (int i = 0; i < 4; i++)
                Cyl(t, "Nozzle" + i, new Vector3(194, 14 + (i / 2) * 24, 28 + (i % 2) * 24), new Vector3(10, 6, 10), p.dark, new Vector3(0, 0, 90));
            for (int i = 0; i < 4; i++)
                Box(t, "NozzleGlow" + i, new Vector3(200.5f, 14 + (i / 2) * 24, 28 + (i % 2) * 24), new Vector3(0.6f, 6, 6), p.glow);
            // Tanques extras e duas antenas.
            for (int i = 0; i < 2; i++)
                Cyl(t, "BigTank" + i, new Vector3(-70 + i * 30, 10, 52), new Vector3(22, 14, 22), p.second);
            Dish(t, new Vector3(-110, 54, 10), 18f, p);
            Dish(t, new Vector3(-90, 54, -16), 12f, p);
        }

        static void ScienceDetails(Transform t, Palette p)
        {
            // Domo de vidro sobre o nucleo.
            Cyl(t, "DomeRing", new Vector3(-10, 70, 0), new Vector3(52, 2, 52), p.second);
            var dome = PadBuilder.Part(t, PrimitiveType.Sphere, "Dome", new Vector3(-10, 70, 0), new Vector3(50, 40, 50), p.glass);
            Object.Destroy(dome.GetComponent<Collider>());
            Box(t, "DomeLab", new Vector3(-10, 76, 0), new Vector3(18, 10, 18), p.main); // laboratorio dentro do domo
            // Antenas parabolicas grandes.
            Dish(t, new Vector3(-120, 56, 10), 34f, p);
            Dish(t, new Vector3(60, 74, -10), 40f, p);
            Dish(t, new Vector3(110, 56, 60), 16f, p);
            // Faixas azuis nos modulos.
            Box(t, "BlueStripeL", new Vector3(-120, 44, 0), new Vector3(62, 5, 56), p.second);
            Box(t, "BlueStripeR", new Vector3(110, 44, 40), new Vector3(72, 5, 62), p.second);
        }

        static void MilitaryDetails(Transform t, Palette p)
        {
            // Bunker de comando com janelas vermelhas.
            Box(t, "Command", new Vector3(-30, 78, 0), new Vector3(46, 22, 40), p.second);
            for (int i = 0; i < 3; i++)
                Box(t, "CommandWindow" + i, new Vector3(-44 + i * 14, 80, 20.6f), new Vector3(10, 4, 0.8f), p.glow);
            // Torretas (base + dois canos).
            Turret(t, new Vector3(30, 76, 10), p);
            Turret(t, new Vector3(-120, 58, 14), p);
            Turret(t, new Vector3(110, 56, 20), p);
            Turret(t, new Vector3(130, 56, 60), p);
            // Torre de radar.
            Box(t, "RadarTower", new Vector3(60, 110, -20), new Vector3(6, 90, 6), p.dark);
            for (int i = 0; i < 3; i++)
                Dish(t, new Vector3(60, 120 + i * 18, -20), 10f, p);
            // Hangar aberto (estaleiro militar).
            Box(t, "Hangar", new Vector3(150, 30, -40), new Vector3(60, 46, 50), p.main);
            Box(t, "HangarMouth", new Vector3(180.5f, 30, -40), new Vector3(1, 32, 36), p.glow);
        }

        static void Turret(Transform t, Vector3 pos, Palette p)
        {
            Cyl(t, "TurretBase", pos, new Vector3(12, 3, 12), p.dark);
            Box(t, "TurretHead", pos + new Vector3(0, 6, 0), new Vector3(10, 6, 12), p.second);
            Cyl(t, "BarrelA", pos + new Vector3(-2, 7, 12), new Vector3(1.5f, 8, 1.5f), p.dark, new Vector3(80, 0, 0));
            Cyl(t, "BarrelB", pos + new Vector3(2, 7, 12), new Vector3(1.5f, 8, 1.5f), p.dark, new Vector3(80, 0, 0));
        }

        static void Dish(Transform t, Vector3 pos, float size, Palette p)
        {
            Cyl(t, "DishPost", pos + Vector3.up * size * 0.25f, new Vector3(size * 0.08f, size * 0.25f, size * 0.08f), p.dark);
            PadBuilder.Part(t, PrimitiveType.Sphere, "Dish", pos + Vector3.up * size * 0.6f, new Vector3(size, size * 0.2f, size), p.main, new Vector3(-40, 0, 0));
        }

        // Braco com passarela e a plataforma na ponta.
        static Dock Arm(Transform t, int index, DockSize size, Vector3 padPos, float padSize, Palette p, Material hull, bool military)
        {
            var from = new Vector3(Mathf.Sign(padPos.x) * 40f, 0, Mathf.Sign(padPos.z) * 30f);
            var to = padPos;
            var mid = (from + to) / 2f;
            var dir = to - from;
            var walk = PadBuilder.Part(t, PrimitiveType.Cube, "Walkway" + index, mid, new Vector3(10, 3, dir.magnitude), p.dark);
            walk.localRotation = Quaternion.LookRotation(dir.normalized);
            var rail = PadBuilder.Part(t, PrimitiveType.Cube, "WalkRail" + index, mid + Vector3.up * 4f, new Vector3(11, 1, dir.magnitude), p.rail);
            rail.localRotation = walk.localRotation;
            // Suporte sob a plataforma e moldura (vermelha nos militares).
            Box(t, "PadFrame" + index, padPos + Vector3.down * 2.5f, new Vector3(padSize + 6, 3, padSize + 6), military ? p.glow : p.second);
            Box(t, "PadPylon" + index, padPos + Vector3.down * 30f, new Vector3(8, 54, 8), p.rail);
            for (int i = 0; i < 4; i++)
            {
                float sx = i % 2 == 0 ? -1 : 1, sz = i < 2 ? -1 : 1;
                Box(t, $"PadLight{index}_{i}", padPos + new Vector3(sx * (padSize / 2f + 2f), 1.5f, sz * (padSize / 2f + 2f)), new Vector3(2, 2, 2), p.glow);
            }
            return PadBuilder.Build(t, index, size, padPos, 0f, padSize, hull);
        }

        static Transform Box(Transform t, string name, Vector3 pos, Vector3 scale, Material m, Vector3 rot = default) =>
            PadBuilder.Part(t, PrimitiveType.Cube, name, pos, scale, m, rot);

        static Transform Cyl(Transform t, string name, Vector3 pos, Vector3 scale, Material m, Vector3 rot = default) =>
            PadBuilder.Part(t, PrimitiveType.Cylinder, name, pos, scale, m, rot);

        static Palette PaletteFor(StationStyle style, Material hull)
        {
            switch (style)
            {
                case StationStyle.ScienceOutpost:
                    return science ??= new Palette
                    {
                        main = PadBuilder.Tint(hull, new Color(0.9f, 0.92f, 0.95f)),
                        second = PadBuilder.Tint(hull, new Color(0.3f, 0.58f, 0.85f)),
                        accent = PadBuilder.Tint(hull, new Color(0.95f, 0.55f, 0.2f)),
                        dark = PadBuilder.Tint(hull, new Color(0.15f, 0.2f, 0.33f)),
                        rail = PadBuilder.Tint(hull, new Color(0.6f, 0.62f, 0.66f)),
                        panel = PadBuilder.Tint(hull, new Color(0.2f, 0.3f, 0.65f)),
                        glass = PadBuilder.Transparent(new Color(0.6f, 0.85f, 1f, 0.35f)),
                        glow = PadBuilder.Emissive(new Color(1f, 0.9f, 0.6f), 2f),
                    };
                case StationStyle.MilitaryOutpost:
                    return military ??= new Palette
                    {
                        main = PadBuilder.Tint(hull, new Color(0.33f, 0.37f, 0.22f)),
                        second = PadBuilder.Tint(hull, new Color(0.24f, 0.27f, 0.16f)),
                        accent = PadBuilder.Tint(hull, new Color(0.45f, 0.42f, 0.3f)),
                        dark = PadBuilder.Tint(hull, new Color(0.16f, 0.17f, 0.14f)),
                        rail = PadBuilder.Tint(hull, new Color(0.35f, 0.36f, 0.33f)),
                        panel = PadBuilder.Tint(hull, new Color(0.2f, 0.28f, 0.45f)),
                        glass = PadBuilder.Transparent(new Color(0.9f, 0.4f, 0.3f, 0.35f)),
                        glow = PadBuilder.Emissive(new Color(1f, 0.15f, 0.1f), 2.5f),
                    };
                default:
                    return industrial ??= new Palette
                    {
                        main = PadBuilder.Tint(hull, new Color(0.72f, 0.45f, 0.2f)),
                        second = PadBuilder.Tint(hull, new Color(0.45f, 0.44f, 0.42f)),
                        accent = PadBuilder.Tint(hull, new Color(0.92f, 0.6f, 0.15f)),
                        dark = PadBuilder.Tint(hull, new Color(0.24f, 0.22f, 0.2f)),
                        rail = PadBuilder.Tint(hull, new Color(0.5f, 0.48f, 0.45f)),
                        panel = PadBuilder.Tint(hull, new Color(0.2f, 0.3f, 0.6f)),
                        glass = PadBuilder.Transparent(new Color(0.6f, 0.85f, 1f, 0.35f)),
                        glow = PadBuilder.Emissive(new Color(1f, 0.75f, 0.35f), 2f),
                    };
            }
        }
    }
}
