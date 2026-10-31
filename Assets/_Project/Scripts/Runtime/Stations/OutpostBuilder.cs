using UnityEngine;

namespace PulseFactor
{
    // Posto avancado sobre um asteroide (referencia: base de superficie com domos, tubos,
    // antenas, paineis solares, guindaste e 3 plataformas sobre trelicas: 01 P, 02 M, 03 G). Estatico.
    public static class OutpostBuilder
    {
        const float RockRadius = 1200f;
        const float PlateauFactor = 0.82f; // o topo do asteroide e achatado para a base

        static Material white, grey, dark, orange, glass, panel, rock, crater;

        public static Station Build(StationDef def, GameConfig config, Reputation reputation, WorldPosition position, Material hull, Material rockMat, Economy economy = null)
        {
            CreateMaterials(hull, rockMat);
            var root = new GameObject("Outpost_" + def.id);
            var anchor = root.AddComponent<WorldAnchor>();
            var station = root.AddComponent<Station>();
            var t = root.transform;

            BuildRock(t);
            float y = RockRadius * PlateauFactor;
            var basePos = new Vector3(0, y, 0);

            // Domos de vidro e modulos ligados por tubos.
            Dome(t, basePos + new Vector3(-60, 0, 40), 38f);
            Dome(t, basePos + new Vector3(-60, 0, -50), 38f);
            Module(t, basePos + new Vector3(30, 0, 40), 26f, 30f);
            Module(t, basePos + new Vector3(30, 0, -50), 22f, 26f);
            Tube(t, basePos + new Vector3(-60, 8, 40), basePos + new Vector3(30, 8, 40));
            Tube(t, basePos + new Vector3(-60, 8, -50), basePos + new Vector3(30, 8, -50));
            Tube(t, basePos + new Vector3(-60, 8, 40), basePos + new Vector3(-60, 8, -50));
            Tube(t, basePos + new Vector3(30, 8, 40), basePos + new Vector3(110, 8, 40));
            Capsule(t, basePos + new Vector3(110, 12, 40), new Vector3(24, 20, 24), white);   // armazem
            PadBuilder.Part(t, PrimitiveType.Cylinder, "Band", basePos + new Vector3(110, 12, 40), new Vector3(25, 2, 25), orange);

            // Tanques, antenas, pratos e paineis solares.
            for (int i = 0; i < 3; i++)
                Capsule(t, basePos + new Vector3(-130 + i * 16, 10, 0), new Vector3(10, 10, 10), white);
            Dish(t, basePos + new Vector3(70, 0, 110));
            Dish(t, basePos + new Vector3(130, 0, 90));
            for (int i = 0; i < 3; i++)
                PadBuilder.Part(t, PrimitiveType.Cylinder, "Mast" + i, basePos + new Vector3(150 - i * 40, 30, 130 - i * 10), new Vector3(1.5f, 30, 1.5f), dark);
            for (int i = 0; i < 4; i++)
                PadBuilder.Part(t, PrimitiveType.Cube, "Solar" + i, basePos + new Vector3(100 + (i % 2) * 34, 10, 140 + (i / 2) * 24), new Vector3(30, 1, 18), panel, new Vector3(25, 0, 0));

            // Guindaste.
            PadBuilder.Part(t, PrimitiveType.Cube, "CraneBase", basePos + new Vector3(110, 6, -60), new Vector3(16, 12, 16), orange);
            PadBuilder.Part(t, PrimitiveType.Cube, "CraneMast", basePos + new Vector3(110, 30, -60), new Vector3(4, 40, 4), orange);
            PadBuilder.Part(t, PrimitiveType.Cube, "CraneArm", basePos + new Vector3(90, 48, -60), new Vector3(44, 3, 3), orange);

            // Holofotes da base (o asteroide fica escuro no lado oposto ao sol).
            var warm = new Color(1f, 0.82f, 0.6f);
            PadBuilder.AddLight(t, basePos + new Vector3(-40, 60, 0), warm, 520f, 4f);
            PadBuilder.AddLight(t, basePos + new Vector3(110, 60, 60), warm, 420f, 3.5f);
            PadBuilder.AddLight(t, basePos + new Vector3(-100, 50, -140), new Color(0.7f, 0.85f, 1f), 400f, 3f);
            PadBuilder.AddLight(t, basePos + new Vector3(0, 250, 0), new Color(0.75f, 0.85f, 1f), 1400f, 2f); // luz geral do plato

            // Plataformas sobre trelicas: 01 P, 02 M, 03 G.
            var docks = new Dock[3];
            docks[0] = ElevatedPad(t, 0, DockSize.P, basePos + new Vector3(-160, 0, 130), 44f, 28f, hull);
            docks[1] = ElevatedPad(t, 1, DockSize.M, basePos + new Vector3(-140, 0, -150), 58f, 32f, hull);
            docks[2] = ElevatedPad(t, 2, DockSize.G, basePos + new Vector3(-20, 0, -190), 78f, 36f, hull);

            station.Initialize(def, config, docks, reputation, economy);
            anchor.SetWorld(position);
            return station;
        }

        static Dock ElevatedPad(Transform t, int index, DockSize size, Vector3 groundPos, float padSize, float height, Material hull)
        {
            float h = padSize / 2f - 3f;
            for (int i = 0; i < 4; i++)
            {
                float sx = i % 2 == 0 ? -1 : 1, sz = i < 2 ? -1 : 1;
                var leg = groundPos + new Vector3(sx * h, height / 2f, sz * h);
                PadBuilder.Part(t, PrimitiveType.Cube, $"Leg{index}_{i}", leg, new Vector3(2.5f, height, 2.5f), grey);
            }
            PadBuilder.Part(t, PrimitiveType.Cube, $"Brace{index}a", groundPos + new Vector3(0, height * 0.45f, -h), new Vector3(padSize - 6f, 1.5f, 1.5f), grey, new Vector3(0, 0, 20));
            PadBuilder.Part(t, PrimitiveType.Cube, $"Brace{index}b", groundPos + new Vector3(0, height * 0.45f, h), new Vector3(padSize - 6f, 1.5f, 1.5f), grey, new Vector3(0, 0, -20));
            return PadBuilder.Build(t, index, size, groundPos + Vector3.up * (height + 1f), 0f, padSize, hull);
        }

        static void BuildRock(Transform t)
        {
            var go = new GameObject("Asteroid");
            go.transform.SetParent(t, false);
            var mesh = RockMesh(RockRadius, 28, 56);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = rock;
            go.AddComponent<MeshCollider>().sharedMesh = mesh;

            // Crateras nas laterais.
            var rng = new System.Random(7);
            for (int i = 0; i < 9; i++)
            {
                var dir = Quaternion.Euler((float)rng.NextDouble() * 120f - 20f, (float)rng.NextDouble() * 360f, 0) * Vector3.forward;
                if (dir.y > 0.6f) continue; // nao em cima da base
                float size = 120f + (float)rng.NextDouble() * 220f;
                var c = PadBuilder.Part(t, PrimitiveType.Sphere, "Crater" + i, dir * (RockRadius * 0.97f), new Vector3(size, size * 0.25f, size), crater);
                c.rotation = Quaternion.FromToRotation(Vector3.up, dir);
                Object.Destroy(c.GetComponent<Collider>());
            }
        }

        // Esfera deformada por ruido, com o topo achatado (plato da base).
        static Mesh RockMesh(float radius, int lat, int lon)
        {
            var verts = new Vector3[(lat + 1) * (lon + 1)];
            for (int i = 0; i <= lat; i++)
            {
                float theta = Mathf.PI * i / lat;
                for (int j = 0; j <= lon; j++)
                {
                    float phi = 2f * Mathf.PI * (j % lon) / lon;
                    var n = new Vector3(Mathf.Sin(theta) * Mathf.Cos(phi), Mathf.Cos(theta), Mathf.Sin(theta) * Mathf.Sin(phi));
                    float noise = 1f + 0.08f * Mathf.Sin(n.x * 5.1f + n.z * 3.7f) + 0.05f * Mathf.Sin(n.y * 9.3f + n.x * 6.1f) + 0.03f * Mathf.Sin(n.z * 17f);
                    var p = n * radius * noise;
                    p.y = Mathf.Min(p.y, radius * PlateauFactor);
                    verts[i * (lon + 1) + j] = p;
                }
            }
            var tris = new int[lat * lon * 6];
            int k = 0;
            for (int i = 0; i < lat; i++)
            for (int j = 0; j < lon; j++)
            {
                int a = i * (lon + 1) + j, b = a + lon + 1;
                tris[k++] = a; tris[k++] = a + 1; tris[k++] = b;
                tris[k++] = b; tris[k++] = a + 1; tris[k++] = b + 1;
            }
            var m = new Mesh { name = "OutpostRock", vertices = verts, triangles = tris };
            m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }

        static void Dome(Transform t, Vector3 pos, float size)
        {
            PadBuilder.Part(t, PrimitiveType.Cylinder, "DomeRing", pos + Vector3.up * 2f, new Vector3(size * 1.05f, 2f, size * 1.05f), orange);
            PadBuilder.Part(t, PrimitiveType.Sphere, "Dome", pos + Vector3.up * 2f, new Vector3(size, size * 0.9f, size), glass);
        }

        static void Module(Transform t, Vector3 pos, float size, float height)
        {
            PadBuilder.Part(t, PrimitiveType.Cylinder, "Module", pos + Vector3.up * (height / 2f), new Vector3(size, height / 2f, size), white);
            PadBuilder.Part(t, PrimitiveType.Cylinder, "ModuleBand", pos + Vector3.up * (height * 0.6f), new Vector3(size * 1.04f, 1.5f, size * 1.04f), orange);
            PadBuilder.Part(t, PrimitiveType.Sphere, "ModuleCap", pos + Vector3.up * height, new Vector3(size, size * 0.4f, size), grey);
        }

        static void Tube(Transform t, Vector3 a, Vector3 b)
        {
            var mid = (a + b) / 2f;
            var dir = b - a;
            var tube = PadBuilder.Part(t, PrimitiveType.Cylinder, "Tube", mid, new Vector3(9, dir.magnitude / 2f, 9), grey);
            tube.rotation = Quaternion.FromToRotation(Vector3.up, dir.normalized);
        }

        static void Capsule(Transform t, Vector3 pos, Vector3 scale, Material m) =>
            PadBuilder.Part(t, PrimitiveType.Capsule, "Tank", pos, scale, m);

        static void Dish(Transform t, Vector3 pos)
        {
            PadBuilder.Part(t, PrimitiveType.Cylinder, "DishPost", pos + Vector3.up * 8f, new Vector3(3, 8, 3), grey);
            PadBuilder.Part(t, PrimitiveType.Sphere, "Dish", pos + Vector3.up * 20f, new Vector3(26, 6, 26), white, new Vector3(-35, 0, 0));
        }

        static void CreateMaterials(Material hull, Material rockMat)
        {
            if (white) return;
            white = PadBuilder.Tint(hull, new Color(0.88f, 0.89f, 0.9f));
            grey = PadBuilder.Tint(hull, new Color(0.5f, 0.53f, 0.57f));
            dark = PadBuilder.Tint(hull, new Color(0.25f, 0.27f, 0.3f));
            orange = PadBuilder.Tint(hull, new Color(1f, 0.5f, 0.12f));
            panel = PadBuilder.Tint(hull, new Color(0.2f, 0.3f, 0.6f));
            glass = PadBuilder.Transparent(new Color(0.6f, 0.85f, 1f, 0.35f));
            rock = PadBuilder.Tint(rockMat, new Color(0.33f, 0.33f, 0.35f));
            crater = PadBuilder.Tint(rockMat, new Color(0.22f, 0.22f, 0.24f));
        }
    }
}
