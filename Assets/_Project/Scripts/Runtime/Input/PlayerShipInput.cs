using System;
using UnityEngine;

namespace PulseFactor
{
    // Le os controles do jogador e escreve no ShipController.
    // Mira estilo Everspace: o mouse move um reticulo virtual; a nave gira na direcao dele.
    public class PlayerShipInput : MonoBehaviour
    {
        [SerializeField] float mouseSensitivity = 0.0012f; // fracao da tela por pixel
        [SerializeField] float maxMouseDelta = 150f;       // pixels por frame; acima disso e salto de foco
        [SerializeField] float reticleRadius = 0.35f;      // fracao da altura da tela
        [SerializeField] float deadZone = 0.02f;
        [SerializeField] float stickSpeed = 1.5f;
        [SerializeField] float lookSensitivity = 0.15f;   // graus por pixel no olhar livre

        public ShipController Ship;
        public Camera AimCamera;
        public PulseControls Controls { get; private set; }

        // Deslocamento do reticulo em fracao da altura da tela (centro = 0).
        public Vector2 ReticleOffset { get; private set; }

        public event Action OnToggleCamera, OnWarp, OnRequestDock, OnInteract, OnSelectTarget, OnPause, OnMap, OnClearTarget, OnPanelNext, OnPanelPrev;
        public event Action<bool> OnFirePrimary, OnFireSecondary;
        public event Action<int> OnSkill;

        public bool FirePrimaryHeld => Controls.FirePrimary.IsPressed() && UnityEngine.Cursor.lockState == CursorLockMode.Locked;
        public bool BoostHeld => Controls.Flight.enabled && Controls.Boost.IsPressed();
        public bool FireSecondaryHeld => Controls.FireSecondary.IsPressed() && UnityEngine.Cursor.lockState == CursorLockMode.Locked;

        void Awake()
        {
            Controls = new PulseControls();
            Controls.ToggleCamera.performed += _ => OnToggleCamera?.Invoke();
            Controls.Warp.performed += _ => OnWarp?.Invoke();
            Controls.RequestDock.performed += _ => OnRequestDock?.Invoke();
            Controls.Interact.performed += _ => OnInteract?.Invoke();
            Controls.DockedInteract.performed += _ => OnInteract?.Invoke();
            // T sozinho seleciona; Ctrl+T limpa alvo e rota.
            Controls.SelectTarget.performed += _ =>
            {
                var kb = UnityEngine.InputSystem.Keyboard.current;
                if (kb != null && kb.ctrlKey.isPressed) return;
                OnSelectTarget?.Invoke();
            };
            Controls.ClearTarget.performed += _ => OnClearTarget?.Invoke();
            Controls.Map.performed += _ => OnMap?.Invoke();
            Controls.PanelNext.performed += _ => OnPanelNext?.Invoke();
            Controls.PanelPrev.performed += _ => OnPanelPrev?.Invoke();
            Controls.Pause.performed += _ => OnPause?.Invoke();
            Controls.DockedBack.performed += _ => OnPause?.Invoke();
            Controls.FirePrimary.started += _ => OnFirePrimary?.Invoke(true);
            Controls.FirePrimary.canceled += _ => OnFirePrimary?.Invoke(false);
            Controls.FireSecondary.started += _ => OnFireSecondary?.Invoke(true);
            Controls.FireSecondary.canceled += _ => OnFireSecondary?.Invoke(false);
            Controls.Skill1.performed += _ => OnSkill?.Invoke(0);
            Controls.Skill2.performed += _ => OnSkill?.Invoke(1);
            Controls.Skill3.performed += _ => OnSkill?.Invoke(2);
            Controls.Cruise.performed += _ => cruisePressed = true;
        }

        bool cruisePressed;

        void OnEnable() => SetDocked(false);
        void OnDisable() { Controls.Flight.Disable(); Controls.Docked.Disable(); }

        public void SetDocked(bool docked)
        {
            if (docked) { Controls.Flight.Disable(); Controls.Docked.Enable(); }
            else { Controls.Docked.Disable(); Controls.Flight.Enable(); }
            Cursor.lockState = docked ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = docked;
            ReticleOffset = Vector2.zero;
        }

        void Update()
        {
            if (!Ship) return;
            var cam = AimCamera ? AimCamera : Camera.main;

            // Ignora o "salto" do mouse ao focar a janela / travar o cursor.
            Vector2 mouse = Controls.AimDelta.ReadValue<Vector2>();
            bool mouseValid = Application.isFocused && UnityEngine.Cursor.lockState == CursorLockMode.Locked
                              && mouse.sqrMagnitude < maxMouseDelta * maxMouseDelta;
            if (!mouseValid) mouse = Vector2.zero;

            // Z segurado: o mouse gira a camera (olhar ao redor); a mira da nave fica parada.
            FreeLooking = Controls.FreeLook.IsPressed();
            Vector2 stick = Controls.AimStick.ReadValue<Vector2>();
            if (FreeLooking)
            {
                var look = LookAngles + new Vector2(mouse.x, -mouse.y) * lookSensitivity + new Vector2(stick.x, -stick.y) * (120f * Time.deltaTime);
                LookAngles = new Vector2(Mathf.Clamp(look.x, -160f, 160f), Mathf.Clamp(look.y, -80f, 80f));
            }
            else
            {
                LookAngles = Vector2.Lerp(LookAngles, Vector2.zero, 1f - Mathf.Exp(-8f * Time.deltaTime));
                Vector2 offset = ReticleOffset + mouse * mouseSensitivity + stick * (stickSpeed * Time.deltaTime);
                ReticleOffset = Vector2.ClampMagnitude(offset, reticleRadius);
            }

            // Mira no referencial da nave (independe da camera): deslocamento do reticulo * campo de visao.
            float fov = cam ? cam.fieldOfView : 60f;
            AimDirection = Ship.transform.rotation * Quaternion.Euler(-ReticleOffset.y * fov, ReticleOffset.x * fov, 0f) * Vector3.forward;
            Vector3 aim = ReticleOffset.magnitude > deadZone ? AimDirection : Ship.transform.forward;

            var input = Ship.Input;
            input.throttle = Controls.Throttle.ReadValue<float>();
            input.strafe = new Vector2(Controls.Strafe.ReadValue<float>(), Controls.Vertical.ReadValue<float>());
            input.roll = Controls.Roll.ReadValue<float>();
            input.aimDirection = aim;
            if (cruisePressed) { input.cruiseToggle = true; cruisePressed = false; }
            Ship.Input = input;
        }

        public bool FreeLooking { get; private set; }
        public Vector2 LookAngles { get; private set; } // graus: x = yaw, y = pitch
        public Vector3 AimDirection { get; private set; } = Vector3.forward;

        // Posicao do reticulo em pixels (projecao da mira), para o HUD; fora da tela quando atras da camera.
        public Vector2 ReticleScreenPosition
        {
            get
            {
                var cam = AimCamera ? AimCamera : Camera.main;
                if (!cam) return new Vector2(Screen.width, Screen.height) * 0.5f;
                var sp = cam.WorldToScreenPoint(cam.transform.position + AimDirection * 1000f);
                return sp.z > 0 ? new Vector2(sp.x, sp.y) : new Vector2(-10000f, -10000f);
            }
        }
    }
}
