using UnityEngine;
using UnityEngine.Rendering;

namespace PulseFactor
{
    // Plataforma de pouso estilo Elite: deck escuro, faixas laranja nas laterais, cantoneiras,
    // linhas-guia e o numero pintado na borda ("01"). Holograma do numero + seta azul so na doca concedida.
    public static class PadBuilder
    {
        static Material deck, stripe, guide, paint, holo, arrow;

        public static Dock Build(Transform parent, int index, DockSize size, Vector3 localPos, float yaw, float padSize, Material baseMat)
        {
            CreateMaterials(baseMat);
            var go = new GameObject($"Dock{index + 1:00}_{size}");
            var t = go.transform;
            t.SetParent(parent, false);
            t.localPosition = localPos;
            t.localRotation = Quaternion.Euler(0, yaw, 0);

            float b = padSize / 2f;
            Part(t, PrimitiveType.Cube, "Deck", Vector3.zero, new Vector3(padSize, 2f, padSize), deck);

            // Faixas laranja nas duas laterais (como no Elite).
            Part(t, PrimitiveType.Cube, "StripeL", new Vector3(-b + 2.5f, 1.1f, 0), new Vector3(2.2f, 0.2f, padSize * 0.92f), stripe);
            Part(t, PrimitiveType.Cube, "StripeR", new Vector3(b - 2.5f, 1.1f, 0), new Vector3(2.2f, 0.2f, padSize * 0.92f), stripe);

            // Cantoneiras em L.
            float c = padSize * 0.16f;
            for (int i = 0; i < 4; i++)
            {
                float sx = i % 2 == 0 ? -1 : 1, sz = i < 2 ? -1 : 1;
                var corner = new Vector3(sx * (b - 1.2f), 1.12f, sz * (b - 1.2f));
                Part(t, PrimitiveType.Cube, "CornerA" + i, corner + new Vector3(-sx * c / 2f, 0, 0), new Vector3(c, 0.2f, 1.2f), stripe);
                Part(t, PrimitiveType.Cube, "CornerB" + i, corner + new Vector3(0, 0, -sz * c / 2f), new Vector3(1.2f, 0.2f, c), stripe);
            }

            // Linhas-guia: retangulo interno e eixo central.
            float g = b * 0.62f;
            Part(t, PrimitiveType.Cube, "GuideN", new Vector3(0, 1.1f, g), new Vector3(g * 2f, 0.15f, 0.5f), guide);
            Part(t, PrimitiveType.Cube, "GuideS", new Vector3(0, 1.1f, -g), new Vector3(g * 2f, 0.15f, 0.5f), guide);
            Part(t, PrimitiveType.Cube, "GuideE", new Vector3(g, 1.1f, 0), new Vector3(0.5f, 0.15f, g * 2f), guide);
            Part(t, PrimitiveType.Cube, "GuideW", new Vector3(-g, 1.1f, 0), new Vector3(0.5f, 0.15f, g * 2f), guide);
            Part(t, PrimitiveType.Cube, "GuideAxis", new Vector3(0, 1.1f, 0), new Vector3(0.4f, 0.15f, g * 1.4f), guide);

            AddLight(t, new Vector3(0, padSize * 0.6f, 0), new Color(1f, 0.85f, 0.65f), padSize * 2.2f, 3f);

            var dock = go.AddComponent<Dock>();
            dock.index = index;
            dock.size = size;
            dock.padSize = padSize;
            dock.LandingPoint = Point(t, "LandingPoint", new Vector3(0, 4f, 0));
            dock.ApproachPoint = Point(t, "ApproachPoint", new Vector3(0, 150f, 0));

            // Numero pintado deitado na borda, sempre visivel.
            var painted = Text(t, "PaintedNumber", (index + 1).ToString("00"), new Vector3(0, 1.25f, -b + padSize * 0.13f), padSize * 0.022f, paint.color);
            painted.transform.localRotation = Quaternion.Euler(-90, 180, 0); // deitado, legivel para quem vem de cima

            // Seta azul + holograma do numero: so quando a doca e concedida.
            var marker = new GameObject("Marker");
            marker.transform.SetParent(t, false);
            BuildArrow(marker.transform, padSize);
            var holoText = Text(marker.transform, "Hologram", (index + 1).ToString("00"), new Vector3(0, padSize + 70f, 0), padSize * 0.035f, holo.color);
            holoText.gameObject.AddComponent<Billboard>();
            marker.SetActive(false);
            dock.Marker = marker;
            return dock;
        }

        // Luz pontual leve para destacar estruturas (precisa do URP em Forward+ para muitas luzes).
        public static Light AddLight(Transform parent, Vector3 localPos, Color color, float range, float intensity)
        {
            var go = new GameObject("Light");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            var l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = color;
            l.range = range;
            l.intensity = intensity;
            l.shadows = LightShadows.None;
            return l;
        }

        static TextMesh Text(Transform parent, string name, string value, Vector3 pos, float charSize, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            var text = go.AddComponent<TextMesh>();
            text.text = value;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.fontSize = 120;
            text.characterSize = charSize;
            text.color = color;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            go.GetComponent<MeshRenderer>().sharedMaterial = text.font.material;
            return text;
        }

        static void BuildArrow(Transform parent, float padSize)
        {
            var root = new GameObject("Arrow");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = new Vector3(0, padSize + 30f, 0);
            var shaft = Part(root.transform, PrimitiveType.Cube, "Shaft", new Vector3(0, 14f, 0), new Vector3(6f, 22f, 6f), arrow);
            Object.Destroy(shaft.GetComponent<Collider>());
            var head = new GameObject("Head");
            head.transform.SetParent(root.transform, false);
            head.AddComponent<MeshFilter>().sharedMesh = PyramidDown(16f, 14f);
            head.AddComponent<MeshRenderer>().sharedMaterial = arrow;
            root.AddComponent<Bob>();
        }

        static Mesh PyramidDown(float width, float height)
        {
            float w = width / 2f;
            var v = new[]
            {
                new Vector3(-w, height, -w), new Vector3(w, height, -w), new Vector3(w, height, w), new Vector3(-w, height, w),
                Vector3.zero,
            };
            var tris = new[] { 0, 1, 2, 0, 2, 3, 4, 1, 0, 4, 2, 1, 4, 3, 2, 4, 0, 3 };
            var m = new Mesh { name = "ArrowHead", vertices = v, triangles = tris };
            m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }

        static void CreateMaterials(Material baseMat)
        {
            if (deck) return;
            deck = Tint(baseMat, new Color(0.17f, 0.18f, 0.2f));
            stripe = Emissive(new Color(1f, 0.55f, 0.1f), 2.2f);
            guide = Emissive(new Color(1f, 0.6f, 0.15f), 1.2f);
            paint = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { color = new Color(1f, 0.75f, 0.35f) };
            holo = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { color = new Color(0.35f, 0.85f, 1f, 0.6f) };
            arrow = Transparent(new Color(0.25f, 0.65f, 1f, 0.45f));
        }

        public static Material Tint(Material baseMat, Color c)
        {
            var m = baseMat ? new Material(baseMat) : new Material(Shader.Find("Universal Render Pipeline/Lit"));
            m.SetColor("_BaseColor", c);
            return m;
        }

        public static Material Emissive(Color c, float intensity)
        {
            var m = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            m.SetColor("_BaseColor", c * intensity);
            return m;
        }

        public static Material Transparent(Color c)
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

        public static Transform Part(Transform parent, PrimitiveType type, string name, Vector3 pos, Vector3 scale, Material m, Vector3 rot = default)
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

        public static Transform Point(Transform parent, string name, Vector3 pos)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            t.localPosition = pos;
            return t;
        }
    }
}
