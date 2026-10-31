using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UIElements;

namespace PulseFactor
{
    // Monta a fatia jogavel: origem flutuante, camera, nave do jogador, setor inicial e interface.
    public class GameBootstrap : MonoBehaviour
    {
        [Header("Dados")]
        public GalaxyDef galaxy;
        public GameConfig config;
        public ShipModelDef startingShip;
        public List<BlockDef> catalog = new();
        public List<ResourceDef> resources = new();

        [Header("Materiais")]
        public Material planetMaterial, sunMaterial, hullMaterial, lightMaterial, asteroidMaterial;
        public Material gateMaterial, portalMaterial, beamMaterial, starMaterial, dustMaterial;

        [Header("Interface")]
        public PanelSettings screenPanel, terminalPanel, cockpitPanelBase;

        public PlayerContext Player { get; private set; }

        void Start()
        {
            var origin = new GameObject("FloatingOrigin").AddComponent<FloatingOrigin>();
            var cam = SetupCamera();
            new GameObject("StarField").AddComponent<StarField>().Material = starMaterial;

            Player = BuildPlayer(cam);
            origin.Focus = Player.Ship.transform;

            var dust = new GameObject("SpaceDust").AddComponent<SpaceDust>();
            dust.Ship = Player.Ship;
            dust.Warp = Player.Warp;
            dust.Material = dustMaterial;

            var sectors = new GameObject("Sectors").AddComponent<SectorManager>();
            sectors.Player = Player.Ship.transform;
            sectors.Context = new SectorBuildContext
            {
                Config = config, Catalog = catalog, Camera = cam,
                PlanetMaterial = planetMaterial, SunMaterial = sunMaterial, HullMaterial = hullMaterial,
                LightMaterial = lightMaterial, AsteroidMaterial = asteroidMaterial,
                GateMaterial = gateMaterial, PortalMaterial = portalMaterial,
            };
            Player.Sectors = sectors;
            sectors.Load(galaxy.start, null);
            PlaceNearFirstStation(sectors, origin);

            BuildUi();
        }

        Camera SetupCamera()
        {
            var cam = Camera.main;
            if (!cam)
            {
                cam = new GameObject("Main Camera").AddComponent<Camera>();
                cam.tag = "MainCamera";
                cam.gameObject.AddComponent<AudioListener>();
            }
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.005f, 0.008f, 0.015f);
            cam.gameObject.AddComponent<CameraRig>();
            var data = cam.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = true;

            var volume = new GameObject("PostProcess").AddComponent<Volume>();
            volume.isGlobal = true;
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            var bloom = profile.Add<Bloom>(true);
            bloom.intensity.Override(1.2f);
            bloom.threshold.Override(0.9f);
            var tonemap = profile.Add<Tonemapping>(true);
            tonemap.mode.Override(TonemappingMode.ACES);
            volume.profile = profile;
            return cam;
        }

        PlayerContext BuildPlayer(Camera cam)
        {
            var bp = startingShip.ToBlueprint();
            var go = ShipAssembler.Build(bp, null);
            go.name = "PlayerShip";
            var visual = go.GetComponent<AssembledShip>();

            go.AddComponent<Rigidbody>();
            var box = go.AddComponent<BoxCollider>();
            var bounds = visual.LocalBounds();
            box.center = bounds.center;
            box.size = bounds.size;

            var ship = go.AddComponent<ShipController>();
            ship.Initialize(bp.body.classDef);
            go.AddComponent<WorldAnchor>().dynamic = true;

            var input = go.AddComponent<PlayerShipInput>();
            input.Ship = ship;
            input.AimCamera = cam;
            var targets = go.AddComponent<TargetSelector>();
            targets.Input = input;
            var warp = go.AddComponent<WarpController>();
            warp.Input = input;
            warp.Targets = targets;
            var docking = go.AddComponent<DockingAutopilot>();
            docking.Input = input;
            docking.Warp = warp;
            var laser = go.AddComponent<MiningLaser>();
            laser.Input = input;
            laser.Warp = warp;
            laser.BeamMaterial = beamMaterial;
            laser.Loadout = new WeaponLoadout(bp.body.classDef.weaponSlots);
            laser.Loadout.slots[0].kind = WeaponKind.MiningLaser;
            laser.Loadout.slots[0].group = FireGroup.Primary;

            var rig = cam.GetComponent<CameraRig>();
            rig.Attach(go);
            rig.SetMode(CameraRig.Mode.ThirdPerson);

            return new PlayerContext
            {
                Ship = ship, Visual = visual, Input = input, Warp = warp, Targets = targets,
                Docking = docking, Laser = laser, CameraRig = rig, Config = config,
                Wallet = new Wallet(config.startingCredits),
            };
        }

        void PlaceNearFirstStation(SectorManager sectors, FloatingOrigin origin)
        {
            if (sectors.Stations.Count == 0) return;
            var station = sectors.Stations[0];
            var pos = station.Position + new double3(0, 300, -4000);
            var anchor = Player.Ship.GetComponent<WorldAnchor>();
            anchor.SetWorld(pos);
            origin.Rebase(pos);
            Player.Ship.transform.rotation = Quaternion.LookRotation(station.transform.position - Player.Ship.transform.position);
            Player.Targets.Select(station);
        }

        void BuildUi()
        {
            var hud = Doc("HUD", screenPanel, 0).AddComponent<ScreenHud>();
            hud.Player = Player;
            hud.gameObject.SetActive(true);

            var termGo = Doc("Terminal", terminalPanel, 10);
            var terminal = termGo.AddComponent<StationTerminal>();
            terminal.Player = Player;
            terminal.Resources = resources;
            termGo.SetActive(true);

            var panels = new GameObject("CockpitPanels").AddComponent<CockpitPanels>();
            panels.Player = Player;
            panels.BaseSettings = cockpitPanelBase;
            panels.Build(Player.Visual);

            var input = Player.Input;
            var docking = Player.Docking;
            var rig = Player.CameraRig;
            input.OnToggleCamera += () => { if (docking.Current == DockingAutopilot.Phase.Free || docking.Current == DockingAutopilot.Phase.Authorized) rig.Toggle(); };
            docking.Docked += () =>
            {
                input.SetDocked(true);
                rig.SetMode(CameraRig.Mode.Cockpit); // menus da estacao sempre em primeira pessoa
                terminal.Open();
            };
            docking.Undocked += () => input.SetDocked(false);
            input.OnInteract += () => { if (docking.Current == DockingAutopilot.Phase.Docked && !terminal.IsOpen) terminal.Open(); };
            input.OnPause += () =>
            {
                if (docking.Current == DockingAutopilot.Phase.Docked) return;
                bool locked = UnityEngine.Cursor.lockState == CursorLockMode.Locked;
                UnityEngine.Cursor.lockState = locked ? CursorLockMode.None : CursorLockMode.Locked;
                UnityEngine.Cursor.visible = locked;
            };
            Player.Wallet.Changed += terminal.Refresh;
            Player.Ship.Cargo.Changed += terminal.Refresh;
        }

        static GameObject Doc(string name, PanelSettings settings, int sortingOrder)
        {
            var go = new GameObject(name);
            go.SetActive(false); // o UIDocument precisa do PanelSettings antes do OnEnable
            var doc = go.AddComponent<UIDocument>();
            doc.panelSettings = settings;
            doc.sortingOrder = sortingOrder;
            return go;
        }
    }
}
