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

    // Jump gate: rosquinha gigante. Atravessar o anel leva ao gate pareado em outro setor.
    [RequireComponent(typeof(WorldAnchor))]
    public class JumpGate : MonoBehaviour, ITargetable
    {
        public const float OuterRadius = 1000f;
        public const float TubeRadius = 150f;
        public const float InnerRadius = OuterRadius - TubeRadius;

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

            var ring = new GameObject("Ring");
            ring.transform.SetParent(transform, false);
            var mesh = TorusMesh.Build(OuterRadius - TubeRadius * 0.5f, TubeRadius, 96, 24);
            ring.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = ring.AddComponent<MeshRenderer>();
            if (ringMaterial) mr.sharedMaterial = ringMaterial;
            ring.AddComponent<MeshCollider>().sharedMesh = mesh;

            // Veu luminoso no centro do anel.
            var portal = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Destroy(portal.GetComponent<Collider>());
            portal.name = "Portal";
            portal.transform.SetParent(transform, false);
            portal.transform.localScale = Vector3.one * (InnerRadius * 1.9f);
            if (portalMaterial) portal.GetComponent<Renderer>().sharedMaterial = portalMaterial;
            var back = Instantiate(portal, transform);
            back.transform.localRotation = Quaternion.Euler(0, 180, 0);
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
