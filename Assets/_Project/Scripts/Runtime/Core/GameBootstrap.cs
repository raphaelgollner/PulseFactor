using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
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
        public List<ShipModelDef> smallTraders = new();
        public ShipModelDef mediumTrader;
        public List<CorporationDef> corporations = new();

        [Header("Materiais")]
        public Material planetMaterial, sunMaterial, hullMaterial, lightMaterial, asteroidMaterial;
        public Material gateMaterial, portalMaterial, beamMaterial, starMaterial, dustMaterial;

        [Header("Interface")]
        public PanelSettings screenPanel, terminalPanel, cockpitPanelBase;

        public PlayerContext Player { get; private set; }

        // Testes automaticos nao leem nem gravam o save do jogador.
        public static bool SkipSave;

        FloatingOrigin origin;
        bool quitting;

        void Start()
        {
            var save = SkipSave ? null : SaveSystem.Load(SaveSystem.DefaultPath);
            origin = new GameObject("FloatingOrigin").AddComponent<FloatingOrigin>();
            var cam = SetupCamera();
            new GameObject("StarField").AddComponent<StarField>().Material = starMaterial;

            Player = BuildPlayer(cam, save);
            origin.Focus = Player.Ship.transform;

            var contracts = new GameObject("Contracts").AddComponent<ContractService>();
            contracts.Galaxy = galaxy;
            contracts.Resources = resources;
            contracts.Player = Player;
            Player.Contracts = contracts;
            contracts.RegisterStations();
            if (save != null) RestoreProgress(save, contracts);

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
                Reputation = contracts.Reputation, Economy = contracts.Economy,
                SmallTraders = smallTraders, MediumTrader = mediumTrader, Corporations = corporations,
            };
            Player.Sectors = sectors;

            var route = new GameObject("Route").AddComponent<RouteService>();
            route.Galaxy = galaxy;
            route.Sectors = sectors;
            Player.Route = route;
            Player.Warp.Route = route;
            if (save != null && !string.IsNullOrEmpty(save.routeTargetId))
                route.SetDestination(save.routeSectorId, save.routeTargetId);

            var savedSector = save != null ? galaxy.sectors.Find(s => s.id == save.sectorId) : null;
            sectors.Load(savedSector ? savedSector : galaxy.start, null);
            if (savedSector) PlaceAtSaved(save);
            else PlaceNearFirstStation(sectors, origin);

            BuildUi();

            Player.Docking.Docked += Save;
            Player.Docking.Undocked += Save;
            sectors.Loaded += _ => Save();
        }

        void Update()
        {
            // Atalho de desenvolvimento: F10 = novo jogo.
            if (Keyboard.current != null && Keyboard.current.f10Key.wasPressedThisFrame)
            {
                SaveSystem.Delete(SaveSystem.DefaultPath);
                quitting = true; // nao regravar ao descarregar a cena
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }
        }

        void OnApplicationQuit()
        {
            Save();
            quitting = true;
        }

        public void Save()
        {
            if (SkipSave || quitting || Player == null || !Player.Ship || !Player.Sectors || !Player.Sectors.Current) return;
            var ship = Player.Ship;
            var pos = ship.transform.position;
            // Docado: grava o ponto de aproximacao da doca (fora da plataforma).
            if (Player.Docking.Current == DockingAutopilot.Phase.Docked && Player.Docking.Dock)
                pos = Player.Docking.Dock.ApproachPoint.position;
            var world = origin.ToWorld(pos);
            var rot = ship.transform.rotation;
            var c = Player.Contracts;
            var data = new SaveData
            {
                credits = Player.Wallet.Credits,
                clock = c.Clock.Seconds,
                sectorId = Player.Sectors.Current.id,
                px = world.value.x, py = world.value.y, pz = world.value.z,
                rx = rot.x, ry = rot.y, rz = rot.z, rw = rot.w,
                routeSectorId = Player.Route ? Player.Route.DestinationSectorId : null,
                routeTargetId = Player.Route ? Player.Route.DestinationId : null,
                shipName = Player.ShipName,
            };
            foreach (var kv in ship.Cargo.Contents)
                data.cargo.Add(new SaveData.CargoEntry { resourceId = kv.Key.id, count = kv.Value, reserved = ship.Cargo.Reserved(kv.Key) });
            foreach (var (corp, value) in c.Reputation.All)
                data.reputation.Add(new SaveData.RepEntry { corpId = corp, value = value });
            data.contracts.AddRange(c.Log.Active);
            data.stocks.AddRange(c.Economy.Snapshot());
            try { SaveSystem.Save(data, SaveSystem.DefaultPath); }
            catch (System.Exception e) { Debug.LogWarning("[PulseFactor] Falha ao salvar: " + e.Message); }
        }

        void RestoreProgress(SaveData save, ContractService contracts)
        {
            contracts.Clock.Seconds = save.clock;
            foreach (var r in save.reputation) contracts.Reputation.Set(r.corpId, r.value);
            foreach (var e in save.cargo)
            {
                var res = contracts.Res(e.resourceId);
                if (!res) { Debug.LogWarning("[PulseFactor] Recurso do save desconhecido: " + e.resourceId); continue; }
                Player.Ship.Cargo.Add(res, e.count);
                Player.Ship.Cargo.Reserve(res, e.reserved);
            }
            contracts.Log.Restore(save.contracts);
            contracts.Economy.Restore(save.stocks);
        }

        void PlaceAtSaved(SaveData save)
        {
            var pos = new WorldPosition(new double3(save.px, save.py, save.pz));
            Player.Ship.GetComponent<WorldAnchor>().SetWorld(pos);
            origin.Rebase(pos);
            Player.Ship.transform.rotation = new Quaternion(save.rx, save.ry, save.rz, save.rw);
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

        PlayerContext BuildPlayer(Camera cam, SaveData save)
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
            var proximity = go.AddComponent<ProximityGuard>();
            proximity.Warp = warp;
            var docking = go.AddComponent<DockingAutopilot>();
            docking.Input = input;
            docking.Warp = warp;
            docking.Targets = targets;
            var boost = go.AddComponent<BoostController>();
            boost.Input = input;
            boost.Warp = warp;
            boost.Docking = docking;
            var laser = go.AddComponent<MiningLaser>();
            laser.Input = input;
            laser.Warp = warp;
            laser.BeamMaterial = beamMaterial;
            laser.Loadout = new WeaponLoadout(bp.body.classDef.weaponSlots);
            laser.Loadout.slots[0].kind = WeaponKind.MiningLaser;
            laser.Loadout.slots[0].group = FireGroup.Primary;

            var rig = cam.GetComponent<CameraRig>();
            rig.Attach(go);
            rig.LookSource = input;
            rig.SetMode(CameraRig.Mode.Cockpit); // primeira pessoa e o padrao ao iniciar

            return new PlayerContext
            {
                Ship = ship, Visual = visual, Input = input, Warp = warp, Proximity = proximity, Targets = targets,
                Docking = docking, Laser = laser, Boost = boost, CameraRig = rig, Config = config,
                Wallet = new Wallet(save != null ? save.credits : config.startingCredits),
                ShipName = save != null && !string.IsNullOrEmpty(save.shipName) ? PlayerContext.CleanShipName(save.shipName) : "PULSE-01",
            };
        }

        void PlaceNearFirstStation(SectorManager sectors, FloatingOrigin origin)
        {
            if (sectors.Stations.Count == 0) return;
            var station = sectors.Stations[0];
            var pos = station.Position + new double3(0, 200, -1500); // dentro do alcance de docagem (2 km)
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

            var mapGo = Doc("SystemMap", terminalPanel, 20);
            var map = mapGo.AddComponent<SystemMapView>();
            map.Galaxy = galaxy;
            map.Player = Player;
            mapGo.SetActive(true);

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
                terminal.OpenWithWelcome();
            };
            docking.Undocked += () => input.SetDocked(false);
            // F: docado abre o terminal; em voo (cockpit) liga/desliga o modo mouse das telas.
            input.OnInteract += () =>
            {
                if (docking.Current == DockingAutopilot.Phase.Docked) { if (!terminal.IsOpen) terminal.Open(); return; }
                if (map.IsOpen) return;
                if (rig.CurrentMode == CameraRig.Mode.Cockpit) panels.SetMouseMode(!panels.MouseMode);
            };
            input.OnPanelNext += panels.NextTab;
            input.OnPanelPrev += panels.PrevTab;
            input.OnMap += map.Toggle;
            input.OnClearTarget += () =>
            {
                Player.Targets.Select(null);
                Player.Route.Clear();
                GameMessages.Post("msg.target_cleared");
            };
            docking.Docked += () => Player.Route.ArrivedAt(docking.Station ? docking.Station.DisplayId : null);
            // Esc: fecha mapa / modo mouse; senao abre/fecha o menu (Continuar, Controles, Sair).
            var pauseGo = Doc("PauseMenu", terminalPanel, 40);
            var pause = pauseGo.AddComponent<PauseMenu>();
            pause.ShouldLockCursorOnResume = () => docking.Current != DockingAutopilot.Phase.Docked && !map.IsOpen;
            pause.BeforeQuit = Save;
            pauseGo.SetActive(true);
            input.OnPause += () =>
            {
                if (pause.IsOpen) { pause.Close(); return; }
                if (map.IsOpen) { map.Close(); return; }
                if (panels.MouseMode) { panels.SetMouseMode(false); return; }
                pause.Open();
            };
            Player.Wallet.Changed += terminal.Refresh;
            Player.Ship.Cargo.Changed += terminal.Refresh;
            Player.Contracts.Log.Completed += _ => terminal.Refresh();
            Player.Contracts.Log.Failed += _ => terminal.Refresh();
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
