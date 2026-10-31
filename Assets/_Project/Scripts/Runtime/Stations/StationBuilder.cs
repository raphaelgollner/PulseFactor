using UnityEngine;
using UnityEngine.Rendering;

namespace PulseFactor
{
    // Estacao provisoria em primitivas, no estilo da referencia: anel plano branco/azul, eixo central
    // com 4 bracos, domos, paineis solares e plataformas de pouso "H".
    // Docas: 1-4 P nos nos do anel, 5-6 M e 7 G no topo do eixo. A estacao gira devagar (StationSpin).
    public static class StationBuilder
    {
        const float RingRadius = 320f;
        const float RingHeight = 26f;
        const float RingDepth = 46f;
        const float HubRadius = 75f;
        const float HubHalfHeight = 80f;
        const float ApproachHeight = 150f;

        static Material white, blue, dark, orange, glass, garden;

        public static Station Build(StationDef def, GameConfig config, Reputation reputation, WorldPosition position, Material hull, Material light, Economy economy = null)
        {
            CreateMaterials(hull, light);
            var root = new GameObject("Station_" + def.id);
            var anchor = root.AddComponent<WorldAnchor>();
            var station = root.AddComponent<Station>();
            var t = root.transform;

            BuildRing(t);
            BuildHub(t);

            var docks = new Dock[7];
            // 1-4: P nos nos do anel (entre os bracos).
            for (int i = 0; i < 4; i++)
            {
                float a = 45f + i * 90f;
                var dir = Quaternion.Euler(0, a, 0) * Vector3.forward;
                var node = Part(t, PrimitiveType.Cube, "Node" + i, dir * RingRadius, new Vector3(74, 36, 74), new Vector3(0, a, 0), white);
                docks[i] = PadBuilder.Build(t, i, DockSize.P, dir * RingRadius + Vector3.up * 18f, a, 44f, hull);
                Part(node, PrimitiveType.Cube, "Trim", new Vector3(0, 0.36f, 0), new Vector3(1.02f, 0.08f, 1.02f), Vector3.zero, blue);
            }
            // 5-6: M e 7: G no topo do eixo.
            Part(t, PrimitiveType.Cube, "TopDeck", new Vector3(0, HubHalfHeight + 3f, 0), new Vector3(250, 6, 90), Vector3.zero, dark);
            docks[4] = PadBuilder.Build(t, 4, DockSize.M, new Vector3(-88, HubHalfHeight + 6f, 0), 0f, 58f, hull);
            docks[5] = PadBuilder.Build(t, 5, DockSize.M, new Vector3(88, HubHalfHeight + 6f, 0), 0f, 58f, hull);
            docks[6] = PadBuilder.Build(t, 6, DockSize.G, new Vector3(0, HubHalfHeight + 6f, 0), 0f, 78f, hull);

            for (int i = 0; i < 4; i++)
            {
                var dir = Quaternion.Euler(0, i * 90f, 0) * Vector3.forward;
                PadBuilder.AddLight(t, dir * RingRadius + Vector3.up * 70f, new Color(0.8f, 0.9f, 1f), 380f, 3f);
            }
            PadBuilder.AddLight(t, Vector3.up * (HubHalfHeight + 120f), new Color(1f, 0.9f, 0.75f), 500f, 3f);

            station.Initialize(def, config, docks, reputation, economy);
            anchor.SetWorld(position);
            root.AddComponent<StationSpin>();
            return station;
        }

        static void BuildRing(Transform t)
        {
            const int segments = 36;
            float segLength = 2f * Mathf.PI * RingRadius / segments * 1.03f;
            for (int i = 0; i < segments; i++)
            {
                float a = i * 360f / segments;
                var dir = Quaternion.Euler(0, a, 0) * Vector3.forward;
                var rot = new Vector3(0, a + 90f, 0);
                Part(t, PrimitiveType.Cube, "Ring" + i, dir * RingRadius, new Vector3(segLength, RingHeight, RingDepth), rot, white);
                Part(t, PrimitiveType.Cube, "Stripe" + i, dir * RingRadius + Vector3.up * (RingHeight / 2f + 0.5f), new Vector3(segLength, 1.2f, RingDepth * 0.45f), rot, blue);
                Part(t, PrimitiveType.Cube, "Windows" + i, dir * (RingRadius + RingDepth / 2f + 0.3f), new Vector3(segLength * 0.8f, 5f, 0.6f), rot, dark);
                if (i % 9 == 7) // 70, 160, 250 e 340 graus: longe das plataformas P (45 + 90k) e dos bracos
                {
                    var top = dir * RingRadius + Vector3.up * (RingHeight / 2f);
                    Part(t, PrimitiveType.Cylinder, "DomeBase" + i, top + Vector3.up * 1f, new Vector3(36, 2, 36), Vector3.zero, dark);
                    Part(t, PrimitiveType.Sphere, "Garden" + i, top + Vector3.up * 2f, new Vector3(30, 8, 30), Vector3.zero, garden);
                    Part(t, PrimitiveType.Sphere, "Dome" + i, top, Vector3.one * 34f, Vector3.zero, glass);
                }
                if (i % 6 == 2)
                    Part(t, PrimitiveType.Cube, "Solar" + i, dir * RingRadius + Vector3.up * (RingHeight / 2f + 3f), new Vector3(30, 1, 14), new Vector3(15, a + 90f, 0), blue);
                if (i % 7 == 3)
                    Part(t, PrimitiveType.Cylinder, "Antenna" + i, dir * RingRadius + Vector3.up * (RingHeight / 2f + 18f), new Vector3(1.5f, 18f, 1.5f), Vector3.zero, dark);
            }
            // Entrada laranja no anel.
            var gateDir = Quaternion.Euler(0, 200f, 0) * Vector3.forward;
            Part(t, PrimitiveType.Cylinder, "Entrance", gateDir * (RingRadius + RingDepth / 2f), new Vector3(30, 4, 30), new Vector3(90, 200f, 0), orange);
        }

        static void BuildHub(Transform t)
        {
            Part(t, PrimitiveType.Cylinder, "Hub", Vector3.zero, new Vector3(HubRadius * 2, HubHalfHeight, HubRadius * 2), Vector3.zero, white);
            Part(t, PrimitiveType.Cylinder, "HubBandTop", Vector3.up * (HubHalfHeight * 0.55f), new Vector3(HubRadius * 2.08f, 5, HubRadius * 2.08f), Vector3.zero, orange);
            Part(t, PrimitiveType.Cylinder, "HubBandLow", Vector3.down * (HubHalfHeight * 0.4f), new Vector3(HubRadius * 2.08f, 5, HubRadius * 2.08f), Vector3.zero, blue);
            Part(t, PrimitiveType.Cylinder, "HubBottom", Vector3.down * (HubHalfHeight + 20f), new Vector3(HubRadius * 1.3f, 20, HubRadius * 1.3f), Vector3.zero, dark);
            for (int i = 0; i < 4; i++)
            {
                float a = i * 90f;
                var dir = Quaternion.Euler(0, a, 0) * Vector3.forward;
                float len = RingRadius - HubRadius;
                var mid = dir * (HubRadius + len / 2f);
                Part(t, PrimitiveType.Cylinder, "Spoke" + i, mid, new Vector3(22, len / 2f, 22), new Vector3(90, a, 0), white);
                Part(t, PrimitiveType.Cylinder, "SpokeCollar" + i, dir * (HubRadius + 6f), new Vector3(30, 4, 30), new Vector3(90, a, 0), orange);
            }
        }

        static void CreateMaterials(Material hull, Material light)
        {
            if (white) return;
            white = Tint(hull, new Color(0.9f, 0.92f, 0.95f));
            blue = Tint(hull, new Color(0.55f, 0.72f, 0.9f));
            dark = Tint(hull, new Color(0.3f, 0.33f, 0.38f));
            orange = Tint(hull, new Color(1f, 0.5f, 0.12f));
            glass = Transparent(new Color(0.6f, 0.85f, 1f, 0.35f));
            garden = Tint(hull, new Color(0.25f, 0.55f, 0.25f)); // estufa
        }

        static Material Tint(Material baseMat, Color c)
        {
            var m = baseMat ? new Material(baseMat) : new Material(Shader.Find("Universal Render Pipeline/Lit"));
            m.SetColor("_BaseColor", c);
            return m;
        }

        static Material Transparent(Color c)
        {
            var m = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            m.SetColor("_BaseColor", c);
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 0f);
            m.SetFloat("_Cull", 0f);
            m.SetFloat("_ZWrite", 0f);
            m.SetOverrideTag("RenderType", "Transparent");
            m.renderQueue = (int)RenderQueue.Transparent;
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            m.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            return m;
        }

        static Transform Part(Transform parent, PrimitiveType type, string name, Vector3 pos, Vector3 scale, Vector3 rot, Material m)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            var t = go.transform;
            t.SetParent(parent, false);
            t.localPosition = pos;
            t.localEulerAngles = rot;
            t.localScale = scale;
            if (m) go.GetComponent<Renderer>().sharedMaterial = m;
            return t;
        }

        static Transform Point(Transform parent, string name, Vector3 pos)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            t.localPosition = pos;
            return t;
        }
    }
}
