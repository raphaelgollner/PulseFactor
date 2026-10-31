using System;
using UnityEngine;

namespace PulseFactor
{
    public enum WeaponKind { None, MiningLaser }
    public enum FireGroup { Primary, Secondary }

    [Serializable]
    public class WeaponSlot
    {
        public WeaponKind kind;
        public FireGroup group;
    }

    [Serializable]
    public class WeaponLoadout
    {
        public WeaponSlot[] slots;

        public WeaponLoadout(int slotCount)
        {
            slots = new WeaponSlot[slotCount];
            for (int i = 0; i < slotCount; i++) slots[i] = new WeaponSlot();
        }

        public bool HasInGroup(WeaponKind kind, FireGroup group) =>
            Array.Exists(slots, s => s.kind == kind && s.group == group);
    }

    // Laser de mineracao: com o grupo de fogo pressionado, raycast pela mira ate o alcance.
    [RequireComponent(typeof(ShipController))]
    public class MiningLaser : MonoBehaviour
    {
        [SerializeField] float range = 800f;
        [SerializeField] float beamWidth = 0.6f;

        public PlayerShipInput Input;
        public WarpController Warp;
        public WeaponLoadout Loadout;
        public Material BeamMaterial;

        public bool Firing { get; private set; }
        public Asteroid CurrentAsteroid { get; private set; }

        ShipController ship;
        readonly MiningProgress progress = new();
        LineRenderer beam;
        bool cargoFullWarned;

        void Awake() => ship = GetComponent<ShipController>();

        void Start()
        {
            beam = new GameObject("MiningBeam").AddComponent<LineRenderer>();
            beam.transform.SetParent(transform, false);
            beam.positionCount = 2;
            beam.startWidth = beamWidth;
            beam.endWidth = beamWidth * 0.4f;
            beam.useWorldSpace = true;
            if (BeamMaterial) beam.sharedMaterial = BeamMaterial;
            beam.enabled = false;
        }

        bool TriggerHeld()
        {
            if (!Input || Loadout == null) return false;
            return (Loadout.HasInGroup(WeaponKind.MiningLaser, FireGroup.Primary) && Input.FirePrimaryHeld)
                || (Loadout.HasInGroup(WeaponKind.MiningLaser, FireGroup.Secondary) && Input.FireSecondaryHeld);
        }

        void Update()
        {
            bool inWarp = Warp && Warp.Drive.State != WarpState.Idle;
            Firing = TriggerHeld() && !inWarp && !ship.ControlsLocked;
            CurrentAsteroid = null;
            if (!Firing) { beam.enabled = false; cargoFullWarned = false; return; }

            var cam = Camera.main;
            Vector3 origin = transform.position + transform.forward * 4f - transform.up * 0.8f;
            Ray ray = cam && Input ? cam.ScreenPointToRay(Input.ReticleScreenPosition) : new Ray(origin, transform.forward);
            Vector3 end = ray.origin + ray.direction * range;

            if (RaycastIgnoringSelf(ray, out var hit))
            {
                end = hit.point;
                CurrentAsteroid = hit.collider.GetComponentInParent<Asteroid>();
                if (CurrentAsteroid)
                {
                    if (ship.Cargo.Free <= 0)
                    {
                        if (!cargoFullWarned) GameMessages.Post("msg.cargo_full");
                        cargoFullWarned = true;
                    }
                    else if (progress.Tick(Time.deltaTime, CurrentAsteroid.Deposit, ship.Cargo) > 0)
                    {
                        CurrentAsteroid.OnMined();
                    }
                }
            }

            beam.enabled = true;
            beam.SetPosition(0, origin);
            beam.SetPosition(1, end);
        }

        readonly RaycastHit[] hits = new RaycastHit[16];

        bool RaycastIgnoringSelf(Ray ray, out RaycastHit best)
        {
            best = default;
            int n = Physics.RaycastNonAlloc(ray, hits, range);
            float bestDist = float.MaxValue;
            for (int i = 0; i < n; i++)
            {
                if (hits[i].collider.transform.IsChildOf(transform)) continue;
                if (hits[i].distance < bestDist) { bestDist = hits[i].distance; best = hits[i]; }
            }
            return bestDist < float.MaxValue;
        }
    }
}
