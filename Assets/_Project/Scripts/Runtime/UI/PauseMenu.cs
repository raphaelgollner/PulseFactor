using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace PulseFactor
{
    // Menu do Esc: CONTINUAR, CONTROLES (atalhos), CONFIGURACOES (idioma) e SAIR (com confirmacao; salva antes). Pausa o jogo.
    [RequireComponent(typeof(UIDocument))]
    public class PauseMenu : MonoBehaviour
    {
        public Func<bool> ShouldLockCursorOnResume = () => true;
        public Action BeforeQuit;

        VisualElement root, main, controls, confirm, settings, page;
        Label title, controlsList, confirmText, languageLabel;
        Button resume, controlsButton, settingsButton, quit, back, settingsBack, yes, no;

        public bool IsOpen { get; private set; }

        static readonly string[] ControlKeys =
        {
            "controls.aim", "controls.throttle", "controls.strafe", "controls.vertical", "controls.roll",
            "controls.boost", "controls.cruise", "controls.fire", "controls.warp", "controls.target",
            "controls.clear", "controls.dock", "controls.interact", "controls.camera", "controls.freelook",
            "controls.map", "controls.tabs", "controls.menu", "controls.debug", "controls.newgame",
        };

        void OnEnable()
        {
            root = GetComponent<UIDocument>().rootVisualElement;
            root.Clear();
            var sheet = Resources.Load<StyleSheet>("PulseUI");
            if (sheet) root.styleSheets.Add(sheet);

            var overlay = new VisualElement();
            overlay.AddToClassList("pause-overlay");
            root.Add(overlay);
            var box = new VisualElement();
            box.AddToClassList("pause-box");
            overlay.Add(box);
            title = Lbl(box, "pause-title");

            main = new VisualElement();
            box.Add(main);
            resume = Btn(main, Close);
            controlsButton = Btn(main, () => Show(controls));
            settingsButton = Btn(main, () => Show(settings));
            quit = Btn(main, () => Show(confirm));

            controls = new VisualElement();
            box.Add(controls);
            var scroll = new ScrollView();
            scroll.AddToClassList("pause-controls");
            controls.Add(scroll);
            controlsList = Lbl(scroll, "pause-controls-text");
            back = Btn(controls, () => Show(main));

            settings = new VisualElement();
            box.Add(settings);
            languageLabel = Lbl(settings, "pause-confirm");
            var langs = new VisualElement();
            langs.AddToClassList("pause-row");
            settings.Add(langs);
            Btn(langs, () => Loc.SetLanguage("pt-BR")).text = "Português";
            Btn(langs, () => Loc.SetLanguage("en")).text = "English";
            settingsBack = Btn(settings, () => Show(main));

            confirm = new VisualElement();
            box.Add(confirm);
            confirmText = Lbl(confirm, "pause-confirm");
            var row = new VisualElement();
            row.AddToClassList("pause-row");
            confirm.Add(row);
            yes = Btn(row, Quit);
            yes.AddToClassList("btn-danger");
            no = Btn(row, () => Show(main));

            root.style.display = DisplayStyle.None;
            Loc.LanguageChanged += OnLanguageChanged;
        }

        void OnLanguageChanged()
        {
            if (IsOpen && page != null) Show(page);
        }

        public void Toggle() { if (IsOpen) Close(); else Open(); }

        public void Open()
        {
            IsOpen = true;
            Time.timeScale = 0f;
            root.style.display = DisplayStyle.Flex;
            UnityEngine.Cursor.lockState = CursorLockMode.None;
            UnityEngine.Cursor.visible = true;
            Show(main);
        }

        public void Close()
        {
            IsOpen = false;
            Time.timeScale = 1f;
            root.style.display = DisplayStyle.None;
            if (ShouldLockCursorOnResume())
            {
                UnityEngine.Cursor.lockState = CursorLockMode.Locked;
                UnityEngine.Cursor.visible = false;
            }
        }

        void OnDisable()
        {
            Time.timeScale = 1f;
            Loc.LanguageChanged -= OnLanguageChanged;
        }

        void Show(VisualElement page)
        {
            this.page = page;
            title.text = Loc.T(page == controls ? "pause.controls_title" : page == confirm ? "pause.quit_title"
                : page == settings ? "pause.settings" : "pause.title");
            resume.text = Loc.T("pause.resume");
            controlsButton.text = Loc.T("pause.controls");
            settingsButton.text = Loc.T("pause.settings");
            settingsBack.text = Loc.T("pause.back");
            languageLabel.text = Loc.T("pause.language");
            quit.text = Loc.T("pause.quit");
            back.text = Loc.T("pause.back");
            yes.text = Loc.T("pause.yes");
            no.text = Loc.T("pause.no");
            confirmText.text = Loc.T("pause.quit_confirm");
            controlsList.text = string.Join("\n", Array.ConvertAll(ControlKeys, k => Loc.T(k)));
            main.style.display = page == main ? DisplayStyle.Flex : DisplayStyle.None;
            controls.style.display = page == controls ? DisplayStyle.Flex : DisplayStyle.None;
            confirm.style.display = page == confirm ? DisplayStyle.Flex : DisplayStyle.None;
            settings.style.display = page == settings ? DisplayStyle.Flex : DisplayStyle.None;
        }

        void Quit()
        {
            Time.timeScale = 1f;
            BeforeQuit?.Invoke(); // salva o progresso
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        static Label Lbl(VisualElement parent, string cls)
        {
            var l = new Label();
            l.AddToClassList(cls);
            parent.Add(l);
            return l;
        }

        static Button Btn(VisualElement parent, Action onClick)
        {
            var b = new Button(onClick);
            b.AddToClassList("btn");
            b.AddToClassList("pause-button");
            parent.Add(b);
            return b;
        }
    }
}
