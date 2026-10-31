using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace PulseFactor
{
    // F9: menu de debug para testes (adicionar/remover creditos).
    [RequireComponent(typeof(UIDocument))]
    public class DebugMenu : MonoBehaviour
    {
        public PlayerContext Player;
        public Func<bool> ShouldLockCursorOnClose = () => true;

        VisualElement root;
        Label title, credits, perf;
        float fps, perfTimer;
        int frames;
        Button zero, close;

        public bool IsOpen { get; private set; }

        static readonly long[] Amounts = { 1000, 10000, 100000, 1000000 };

        void OnEnable()
        {
            root = GetComponent<UIDocument>().rootVisualElement;
            root.Clear();
            var sheet = Resources.Load<StyleSheet>("PulseUI");
            if (sheet) root.styleSheets.Add(sheet);

            var box = new VisualElement();
            box.AddToClassList("debug-box");
            root.Add(box);
            title = Lbl(box, "pause-title");
            credits = Lbl(box, "label");
            perf = Lbl(box, "label");

            var add = Row(box);
            foreach (var a in Amounts) Btn(add, "+" + a.ToString("N0"), () => Player.Wallet.Add(a));
            var remove = Row(box);
            foreach (var a in Amounts) Btn(remove, "−" + a.ToString("N0"), () => Player.Wallet.TrySpend(Math.Min(a, Player.Wallet.Credits)));
            var bottom = Row(box);
            zero = Btn(bottom, "", () => Player.Wallet.TrySpend(Player.Wallet.Credits));
            zero.AddToClassList("btn-danger");
            close = Btn(bottom, "", Close);

            root.style.display = DisplayStyle.None;
        }

        void Update()
        {
            if (Keyboard.current != null && Keyboard.current.f9Key.wasPressedThisFrame)
            {
                if (IsOpen) Close();
                else Open();
            }
            frames++;
            perfTimer += Time.unscaledDeltaTime;
            if (perfTimer >= 0.5f) { fps = frames / perfTimer; frames = 0; perfTimer = 0f; }
            if (IsOpen) Refresh();
        }

        public void Open()
        {
            if (Player == null) return;
            IsOpen = true;
            root.style.display = DisplayStyle.Flex;
            UnityEngine.Cursor.lockState = CursorLockMode.None;
            UnityEngine.Cursor.visible = true;
        }

        public void Close()
        {
            IsOpen = false;
            root.style.display = DisplayStyle.None;
            if (ShouldLockCursorOnClose())
            {
                UnityEngine.Cursor.lockState = CursorLockMode.Locked;
                UnityEngine.Cursor.visible = false;
            }
        }

        void Refresh()
        {
            title.text = Loc.T("debug.title");
            credits.text = Loc.T("hud.credits", Player.Wallet.Credits.ToString("N0"));
            zero.text = Loc.T("debug.zero");
            close.text = Loc.T("debug.close");
            perf.text = Loc.T("debug.perf", Mathf.RoundToInt(fps), ShipController.All.Count - 1, Mathf.RoundToInt(1f / Time.fixedDeltaTime));
        }

        static VisualElement Row(VisualElement parent)
        {
            var e = new VisualElement();
            e.AddToClassList("pause-row");
            parent.Add(e);
            return e;
        }

        static Label Lbl(VisualElement parent, string cls)
        {
            var l = new Label();
            l.AddToClassList(cls);
            parent.Add(l);
            return l;
        }

        static Button Btn(VisualElement parent, string text, Action onClick)
        {
            var b = new Button(onClick) { text = text };
            b.AddToClassList("btn");
            b.AddToClassList("pause-button");
            parent.Add(b);
            return b;
        }
    }
}
