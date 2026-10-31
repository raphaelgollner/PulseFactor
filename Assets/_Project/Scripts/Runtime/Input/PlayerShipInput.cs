using System;
using UnityEngine;

namespace PulseFactor
{
    // Le os controles do jogador e escreve no ShipController.
    // Mira estilo Everspace: o mouse move um reticulo virtual; a nave gira na direcao dele.
    public class PlayerShipInput : MonoBehaviour
    {
        [SerializeField] float mouseSensitivity = 0.0025f; // fracao da tela por pixel
        [SerializeField] float reticleRadius = 0.35f;      // fracao da altura da tela
        [SerializeField] float deadZone = 0.02f;
        [SerializeField] float stickSpeed = 1.5f;

        public ShipController Ship;
        public Camera AimCamera;
        public PulseControls Controls { get; private set; }

        // Deslocamento do reticulo em fracao da altura da tela (centro = 0).
        public Vector2 ReticleOffset { get; private set; }

        public event Action OnToggleCamera, OnWarp, OnRequestDock, OnInteract, OnSelectTarget, OnPause;
        public event Action<bool> OnFirePrimary, OnFireSecondary;
        public event Action<int> OnSkill;

        public bool FirePrimaryHeld => Controls.FirePrimary.IsPressed();
        public bool FireSecondaryHeld => Controls.FireSecondary.IsPressed();

        void Awake()
        {
            Controls = new PulseControls();
            Controls.ToggleCamera.performed += _ => OnToggleCamera?.Invoke();
            Controls.Warp.performed += _ => OnWarp?.Invoke();
            Controls.RequestDock.performed += _ => OnRequestDock?.Invoke();
            Controls.Interact.performed += _ => OnInteract?.Invoke();
            Controls.DockedInteract.performed += _ => OnInteract?.Invoke();
            Controls.SelectTarget.performed += _ => OnSelectTarget?.Invoke();
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

            Vector2 offset = ReticleOffset
                + Controls.AimDelta.ReadValue<Vector2>() * mouseSensitivity
                + Controls.AimStick.ReadValue<Vector2>() * (stickSpeed * Time.deltaTime);
            ReticleOffset = Vector2.ClampMagnitude(offset, reticleRadius);

            Vector3 aim = Ship.transform.forward;
            if (cam && ReticleOffset.magnitude > deadZone)
            {
                var screen = new Vector3(
                    Screen.width * 0.5f + ReticleOffset.x * Screen.height,
                    Screen.height * 0.5f + ReticleOffset.y * Screen.height, 0f);
                aim = cam.ScreenPointToRay(screen).direction;
            }

            var input = Ship.Input;
            input.throttle = Controls.Throttle.ReadValue<float>();
            input.strafe = new Vector2(Controls.Strafe.ReadValue<float>(), Controls.Vertical.ReadValue<float>());
            input.roll = Controls.Roll.ReadValue<float>();
            input.aimDirection = aim;
            if (cruisePressed) { input.cruiseToggle = true; cruisePressed = false; }
            Ship.Input = input;
        }

        // Posicao do reticulo em pixels, para o HUD.
        public Vector2 ReticleScreenPosition => new(
            Screen.width * 0.5f + ReticleOffset.x * Screen.height,
            Screen.height * 0.5f + ReticleOffset.y * Screen.height);
    }
}
