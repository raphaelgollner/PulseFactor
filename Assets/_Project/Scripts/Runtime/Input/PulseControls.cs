using UnityEngine;
using UnityEngine.InputSystem;

namespace PulseFactor
{
    // Acoes de input definidas em codigo. Remapeamento: SaveBindingOverrides / LoadBindingOverrides.
    public class PulseControls
    {
        const string OverridesKey = "pulse.bindings";

        public readonly InputActionAsset Asset;
        public readonly InputActionMap Flight;
        public readonly InputActionMap Docked;

        public readonly InputAction Throttle, Strafe, Vertical, Roll, AimDelta, AimStick;
        public readonly InputAction FirePrimary, FireSecondary, Skill1, Skill2, Skill3;
        public readonly InputAction Cruise, Warp, RequestDock, ToggleCamera, Interact, SelectTarget, Pause, FreeLook, Map, ClearTarget, Boost, PanelNext, PanelPrev, ToggleMarkers;
        public readonly InputAction DockedInteract, DockedBack;

        public PulseControls()
        {
            Asset = ScriptableObject.CreateInstance<InputActionAsset>();
            Flight = Asset.AddActionMap("Flight");
            Docked = Asset.AddActionMap("Docked");

            Throttle = Axis(Flight, "Throttle", "<Keyboard>/s", "<Keyboard>/w");
            Throttle.AddBinding("<Gamepad>/leftStick/y");
            Strafe = Axis(Flight, "Strafe", "<Keyboard>/a", "<Keyboard>/d");
            Strafe.AddBinding("<Gamepad>/leftStick/x");
            Vertical = Axis(Flight, "Vertical", "<Keyboard>/c", "<Keyboard>/space");
            Vertical.AddCompositeBinding("1DAxis").With("Negative", "<Gamepad>/dpad/down").With("Positive", "<Gamepad>/dpad/up");
            Roll = Axis(Flight, "Roll", "<Keyboard>/q", "<Keyboard>/e");
            Roll.AddCompositeBinding("1DAxis").With("Negative", "<Gamepad>/leftShoulder").With("Positive", "<Gamepad>/rightShoulder");

            AimDelta = Flight.AddAction("AimDelta", InputActionType.Value, "<Mouse>/delta");
            AimStick = Flight.AddAction("AimStick", InputActionType.Value, "<Gamepad>/rightStick");

            FirePrimary = Button(Flight, "FirePrimary", "<Mouse>/leftButton", "<Gamepad>/rightTrigger");
            FireSecondary = Button(Flight, "FireSecondary", "<Mouse>/rightButton", "<Gamepad>/leftTrigger");
            Skill1 = Button(Flight, "Skill1", "<Keyboard>/1", "<Gamepad>/dpad/left");
            Skill2 = Button(Flight, "Skill2", "<Keyboard>/2", "<Gamepad>/dpad/right");
            Skill3 = Button(Flight, "Skill3", "<Keyboard>/3", null);

            Cruise = Button(Flight, "Cruise", "<Keyboard>/x", "<Gamepad>/buttonEast");
            Warp = Button(Flight, "Warp", "<Keyboard>/j", "<Gamepad>/buttonSouth");
            RequestDock = Button(Flight, "RequestDock", "<Keyboard>/l", "<Gamepad>/select");
            ToggleCamera = Button(Flight, "ToggleCamera", "<Keyboard>/v", "<Gamepad>/buttonNorth");
            Interact = Button(Flight, "Interact", "<Keyboard>/f", "<Gamepad>/buttonWest");
            SelectTarget = Button(Flight, "SelectTarget", "<Keyboard>/t", "<Gamepad>/rightStickPress");
            Pause = Button(Flight, "Pause", "<Keyboard>/escape", "<Gamepad>/start");
            FreeLook = Button(Flight, "FreeLook", "<Keyboard>/z", "<Gamepad>/leftStickPress");
            Map = Button(Flight, "Map", "<Keyboard>/m", "<Gamepad>/dpad/up");
            Boost = Button(Flight, "Boost", "<Keyboard>/leftShift", null); // controle: definir quando houver mapa de botoes final
            PanelPrev = Button(Flight, "PanelPrev", "<Keyboard>/6", null);
            PanelNext = Button(Flight, "PanelNext", "<Keyboard>/7", null);
            ToggleMarkers = Button(Flight, "ToggleMarkers", "<Keyboard>/equals", null);
            ClearTarget = Flight.AddAction("ClearTarget", InputActionType.Button);
            ClearTarget.AddCompositeBinding("OneModifier").With("Modifier", "<Keyboard>/ctrl").With("Binding", "<Keyboard>/t");

            DockedInteract = Button(Docked, "Interact", "<Keyboard>/f", "<Gamepad>/buttonWest");
            DockedBack = Button(Docked, "Back", "<Keyboard>/escape", "<Gamepad>/buttonEast");

            LoadBindingOverrides();
        }

        static InputAction Axis(InputActionMap map, string name, string negative, string positive)
        {
            var a = map.AddAction(name, InputActionType.Value);
            a.expectedControlType = "Axis";
            a.AddCompositeBinding("1DAxis").With("Negative", negative).With("Positive", positive);
            return a;
        }

        static InputAction Button(InputActionMap map, string name, string kb, string pad)
        {
            var a = map.AddAction(name, InputActionType.Button, kb);
            if (pad != null) a.AddBinding(pad);
            return a;
        }

        public void SaveBindingOverrides()
        {
            PlayerPrefs.SetString(OverridesKey, Asset.SaveBindingOverridesAsJson());
            PlayerPrefs.Save();
        }

        public void LoadBindingOverrides()
        {
            var json = PlayerPrefs.GetString(OverridesKey, "");
            if (!string.IsNullOrEmpty(json)) Asset.LoadBindingOverridesFromJson(json);
        }
    }
}
