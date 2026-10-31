using System.Collections.Generic;
using UnityEngine;

namespace PulseFactor
{
    // Palco fora da cena (camada propria, sem WorldAnchor: a origem flutuante nao mexe nele) que mostra a nave
    // do estaleiro girando numa RenderTexture e gera miniaturas das pecas.
    public class ShipPreview : MonoBehaviour
    {
        public const int Layer = 30; // a camera principal ignora esta camada (CameraRig)
        static readonly Vector3 StagePosition = new(0f, -100000f, 0f);

        static ShipPreview instance;
        public static ShipPreview Instance => instance ? instance : instance = Create();

        public RenderTexture Texture { get; private set; }

        Camera cam;
        Transform stage, pivot;
        GameObject model;
        readonly Dictionary<BlockDef, Texture2D> thumbs = new();
        RenderTexture thumbTarget;
        float spin;

        static ShipPreview Create()
        {
            var go = new GameObject("ShipPreview");
            DontDestroyOnLoad(go);
            return go.AddComponent<ShipPreview>();
        }

        void Awake()
        {
            stage = new GameObject("Stage").transform;
            stage.SetParent(transform, false);
            stage.position = StagePosition;
            pivot = new GameObject("Pivot").transform;
            pivot.SetParent(stage, false);

            Texture = new RenderTexture(900, 600, 24) { name = "ShipPreview", antiAliasing = 4 };
            thumbTarget = new RenderTexture(192, 128, 24) { name = "ShipThumb", antiAliasing = 4 };

            cam = new GameObject("PreviewCamera").AddComponent<Camera>();
            cam.transform.SetParent(stage, false);
            cam.cullingMask = 1 << Layer;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.03f, 0.07f, 0.1f, 1f);
            cam.fieldOfView = 30f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 2000f;
            cam.targetTexture = Texture;
            cam.enabled = false;

            Light(new Vector3(40, -30, 0), 1.3f, new Color(1f, 0.96f, 0.9f));
            Light(new Vector3(-20, 150, 0), 0.6f, new Color(0.6f, 0.75f, 1f));
        }

        void Light(Vector3 euler, float intensity, Color color)
        {
            var l = new GameObject("PreviewLight").AddComponent<Light>();
            l.transform.SetParent(stage, false);
            l.transform.localRotation = Quaternion.Euler(euler);
            l.type = LightType.Directional;
            l.intensity = intensity;
            l.color = color;
            l.cullingMask = 1 << Layer;
            l.shadows = LightShadows.None;
        }

        // Mostra a nave (a RenderTexture fica girando enquanto Showing).
        public void Show(ShipBlueprint bp)
        {
            if (model) Destroy(model);
            model = null;
            if (bp == null || !BlockRules.IsValid(bp, out _)) return;
            model = ShipAssembler.Build(bp, pivot);
            Prepare(model);
            Frame(model, Texture, 1.15f);
            cam.enabled = true;
        }

        public void Hide()
        {
            cam.enabled = false;
            if (model) Destroy(model);
            model = null;
        }

        void Update()
        {
            if (!cam.enabled) return;
            spin += Time.unscaledDeltaTime * 20f;
            pivot.localRotation = Quaternion.Euler(0, spin, 0);
        }

        // Miniatura de uma peca com as cores do fabricante (cache).
        public Texture2D Thumbnail(BlockDef block)
        {
            if (!block || !block.prefab) return null;
            if (thumbs.TryGetValue(block, out var tex) && tex) return tex;

            var go = Instantiate(block.prefab, pivot, false);
            go.SetActive(true);
            go.name = block.type.ToString(); // ApplyPaint procura por nome
            if (block.type == BlockType.Wing) go.name = "WingL";
            var holder = new GameObject("Holder");
            holder.transform.SetParent(pivot, false);
            go.transform.SetParent(holder.transform, false);
            ShipAssembler.ApplyPaint(holder, PaintScheme.FromManufacturers(new ShipBlueprint
            {
                cockpit = block, body = block, tail = block, wing = block,
            }));
            Prepare(holder);

            var savedRotation = pivot.localRotation;
            pivot.localRotation = Quaternion.identity;
            bool wasEnabled = cam.enabled;
            Frame(holder, thumbTarget, 1.1f);
            cam.targetTexture = thumbTarget;
            if (model) model.SetActive(false);
            cam.Render();
            if (model) model.SetActive(true);

            var prev = RenderTexture.active;
            RenderTexture.active = thumbTarget;
            tex = new Texture2D(thumbTarget.width, thumbTarget.height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, thumbTarget.width, thumbTarget.height), 0, 0);
            tex.Apply();
            RenderTexture.active = prev;

            holder.SetActive(false);
            Destroy(holder);
            cam.targetTexture = Texture;
            pivot.localRotation = savedRotation;
            if (model) Frame(model, Texture, 1.15f);
            cam.enabled = wasEnabled;
            thumbs[block] = tex;
            return tex;
        }

        static void Prepare(GameObject go)
        {
            foreach (var t in go.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = Layer;
            foreach (var c in go.GetComponentsInChildren<Collider>(true)) c.enabled = false;
        }

        // Camera em 3/4 de frente, afastada o bastante para caber a nave inteira.
        void Frame(GameObject go, RenderTexture target, float margin)
        {
            var bounds = new Bounds(go.transform.position, Vector3.zero);
            foreach (var r in go.GetComponentsInChildren<Renderer>()) bounds.Encapsulate(r.bounds);
            float radius = Mathf.Max(bounds.extents.magnitude, 0.5f) * margin;
            float aspect = (float)target.width / target.height;
            float fov = cam.fieldOfView * Mathf.Deg2Rad * 0.5f;
            float dist = radius / Mathf.Sin(Mathf.Min(fov, Mathf.Atan(Mathf.Tan(fov) * aspect)));
            var dir = Quaternion.Euler(22f, -140f, 0) * Vector3.forward;
            cam.transform.position = bounds.center - dir * dist;
            cam.transform.rotation = Quaternion.LookRotation(dir);
        }

        void OnDestroy()
        {
            if (Texture) Texture.Release();
            if (thumbTarget) thumbTarget.Release();
            foreach (var t in thumbs.Values) if (t) Destroy(t);
        }
    }
}
