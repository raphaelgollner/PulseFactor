using UnityEngine;

namespace PulseFactor
{
    public static class GateTransit
    {
        // Posicoes no espaco local do gate (anel no plano XY, eixo Z atravessa o anel).
        public static bool Crossed(Vector3 prevLocal, Vector3 currLocal, float innerRadius)
        {
            if (Mathf.Sign(prevLocal.z) == Mathf.Sign(currLocal.z) || prevLocal.z == currLocal.z) return false;
            float t = prevLocal.z / (prevLocal.z - currLocal.z);
            Vector3 p = Vector3.Lerp(prevLocal, currLocal, t);
            return new Vector2(p.x, p.y).magnitude < innerRadius;
        }
    }

    // Jump gate (ref. Docs/Design/refs/portal-de-salto.webp): anel segmentado enorme com faixas amarelas,
    // anel interno azul brilhante e um vortice girando no meio. Grande o bastante para naves capitais no futuro.
    // Atravessar o anel (pelos dois lados) leva ao gate pareado em outro setor.
    [RequireComponent(typeof(WorldAnchor))]
    public class JumpGate : MonoBehaviour, ITargetable
    {
        public const float OuterRadius = 2000f;
        public const float FrameRadius = 1850f;   // centro da estrutura do anel
        public const float InnerRadius = 1700f;   // abertura (atravessar aqui faz o salto)

        public GatePlacement Placement { get; private set; }
        public SectorManager Manager;

        WorldAnchor anchor;
        Vector3 prevLocal;
        bool hasPrev;

        public WorldPosition Position => anchor.World;
        public string DisplayId => Placement != null ? Placement.id : name;
        public TargetKind Kind => TargetKind.Gate;

        void Awake() => anchor = GetComponent<WorldAnchor>();
        void OnEnable() => TargetRegistry.Register(this);
        void OnDisable() => TargetRegistry.Unregister(this);

        public void Initialize(GatePlacement placement, SectorManager manager, Material ringMaterial, Material portalMaterial)
        {
            Placement = placement;
            Manager = manager;
            name = "Gate_" + placement.id;

            var frame = PadBuilder.Tint(ringMaterial, new Color(0.32f, 0.34f, 0.38f));
            var dark = PadBuilder.Tint(ringMaterial, new Color(0.18f, 0.19f, 0.22f));
            var yellow = PadBuilder.Tint(ringMaterial, new Color(0.95f, 0.72f, 0.15f));
            var glow = PadBuilder.Emissive(new Color(0.25f, 0.75f, 1f), 4f);
            var t = transform;

            // Colisor: so a estrutura do anel (o meio fica livre pelos dois lados).
            var ring = new GameObject("Ring");
            ring.transform.SetParent(t, false);
            var collider = TorusMesh.Build(FrameRadius, 150f, 64, 12);
            ring.AddComponent<MeshCollider>().sharedMesh = collider;

            // Segmentos da estrutura, faixas amarelas e blocos de no.
            const int segments = 28;
            float segLength = 2f * Mathf.PI * FrameRadius / segments * 0.94f;
            for (int i = 0; i < segments; i++)
            {
                float a = i * 360f / segments;
                var dir = Quaternion.Euler(0, 0, a) * Vector3.up;
                var rot = Quaternion.Euler(0, 0, a);
                Part(t, PrimitiveType.Cube, "Frame" + i, dir * FrameRadius, new Vector3(segLength, 260f, 300f), rot, frame);
                Part(t, PrimitiveType.Cube, "Truss" + i, dir * (FrameRadius - 150f), new Vector3(segLength * 0.9f, 40f, 260f), rot, dark);
                Part(t, PrimitiveType.Cube, "StripeF" + i, dir * (FrameRadius + 60f) + Vector3.forward * 152f, new Vector3(segLength * 0.8f, 30f, 6f), rot, yellow);
                Part(t, PrimitiveType.Cube, "StripeB" + i, dir * (FrameRadius + 60f) - Vector3.forward * 152f, new Vector3(segLength * 0.8f, 30f, 6f), rot, yellow);
                if (i % 4 == 0)
                    Part(t, PrimitiveType.Cube, "Node" + i, dir * (FrameRadius + 40f), new Vector3(260f, 360f, 380f), rot, dark);
            }

            // Anel interno brilhante (azul).
            var inner = new GameObject("InnerGlow");
            inner.transform.SetParent(t, false);
            inner.AddComponent<MeshFilter>().sharedMesh = TorusMesh.Build(InnerRadius + 40f, 28f, 96, 8);
            inner.AddComponent<MeshRenderer>().sharedMaterial = glow;

            // Espetos diagonais e modulos laterais.
            for (int i = 0; i < 4; i++)
            {
                float a = 45f + i * 90f;
                var dir = Quaternion.Euler(0, 0, a) * Vector3.up;
                Part(t, PrimitiveType.Cube, "Spike" + i, dir * (OuterRadius + 380f), new Vector3(90f, 900f, 90f), Quaternion.Euler(0, 0, a), frame);
            }
            foreach (float a in new[] { 0f, 180f })
            {
                var dir = Quaternion.Euler(0, 0, a) * Vector3.right;
                Part(t, PrimitiveType.Cylinder, "Pod", dir * (OuterRadius + 200f), new Vector3(220f, 260f, 220f), Quaternion.Euler(0, 0, 90), dark);
                Part(t, PrimitiveType.Cylinder, "PodBand", dir * (OuterRadius + 200f), new Vector3(230f, 40f, 230f), Quaternion.Euler(0, 0, 90), yellow);
                PadBuilder.AddLight(t, dir * (OuterRadius + 420f), new Color(0.6f, 0.85f, 1f), 1200f, 3f);
            }

            // Vortice: veu com espiral, visivel dos dois lados, girando devagar.
            var veilMat = portalMaterial ? new Material(portalMaterial) : PadBuilder.Transparent(new Color(0.3f, 0.7f, 1f, 0.5f));
            veilMat.SetTexture("_BaseMap", SwirlTexture());
            var veil = new GameObject("Vortex");
            veil.transform.SetParent(t, false);
            Veil(veil.transform, veilMat, Quaternion.identity);
            Veil(veil.transform, veilMat, Quaternion.Euler(0, 180, 0));
            veil.AddComponent<Spin>().degreesPerSecond = 6f;
        }

        static Transform Part(Transform parent, PrimitiveType type, string name, Vector3 pos, Vector3 scale, Quaternion rot, Material m)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            DestroyImmediate(go.GetComponent<Collider>()); // o colisor e o toro "Ring"
            var tr = go.transform;
            tr.SetParent(parent, false);
            tr.localPosition = pos;
            tr.localRotation = rot;
            tr.localScale = scale;
            if (m) go.GetComponent<Renderer>().sharedMaterial = m;
            return tr;
        }

        static void Veil(Transform parent, Material m, Quaternion rotation)
        {
            var veil = GameObject.CreatePrimitive(PrimitiveType.Quad);
            DestroyImmediate(veil.GetComponent<Collider>());
            veil.name = "Portal";
            veil.transform.SetParent(parent, false);
            veil.transform.localRotation = rotation;
            veil.transform.localScale = Vector3.one * (InnerRadius * 2.05f);
            veil.GetComponent<Renderer>().sharedMaterial = m;
        }

        static Texture2D swirl;

        // Espiral procedural (alpha mais forte nos bracos, some na borda).
        static Texture2D SwirlTexture()
        {
            if (swirl) return swirl;
            const int n = 256;
            swirl = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "GateSwirl" };
            var px = new Color[n * n];
            for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float u = x / (n - 1f) * 2f - 1f, v = y / (n - 1f) * 2f - 1f;
                float r = Mathf.Sqrt(u * u + v * v);
                float ang = Mathf.Atan2(v, u);
                float arms = 0.5f + 0.5f * Mathf.Sin(ang * 3f + r * 9f);
                float edge = Mathf.Clamp01(1f - r);
                float a = edge * (0.25f + 0.6f * arms) * Mathf.SmoothStep(0f, 0.15f, edge);
                px[y * n + x] = new Color(0.6f + 0.4f * arms, 0.9f, 1f, a);
            }
            swirl.SetPixels(px);
            swirl.Apply();
            return swirl;
        }

        void Update()
        {
            if (!Manager || !Manager.Player || Placement == null || !Placement.target) return;
            var local = transform.InverseTransformPoint(Manager.Player.position);
            if (hasPrev && GateTransit.Crossed(prevLocal, local, InnerRadius))
            {
                hasPrev = false;
                Manager.Jump(Placement.target, Placement.targetGateId);
                return;
            }
            prevLocal = local;
            hasPrev = true;
        }
    }
}
