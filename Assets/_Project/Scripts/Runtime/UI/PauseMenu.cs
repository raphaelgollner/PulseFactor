using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace PulseFactor
{
    // Menu do Esc: CONTINUAR, CONTROLES (atalhos), CONFIGURACOES (idioma), MENU PRINCIPAL e SAIR
    // (os dois ultimos com confirmacao SIM/NAO; salvam antes). Pausa o jogo. Faixa de pre-alpha embaixo.
    [RequireComponent(typeof(UIDocument))]
    public class PauseMenu : MonoBehaviour
    {
        public Func<bool> ShouldLockCursorOnResume = () => true;
        public Action BeforeQuit;

        VisualElement root, main, controls, confirm, settings, page;
        Label title, controlsList, confirmText, confirmNote, languageLabel, banner;
        Button resume, controlsButton, settingsButton, menuButton, quit, back, settingsBack, yes, no;
        bool confirmMenu; // confirmacao de voltar ao menu (senao, de sair do jogo)

        public bool IsOpen { get; private set; }
        bool lockPending;

        static readonly string[] ControlKeys =
        {
            "controls.aim", "controls.throttle", "controls.strafe", "controls.vertical", "controls.roll",
            "controls.boost", "controls.cruise", "controls.fire", "controls.warp", "controls.target",
            "controls.clear", "controls.dock", "controls.interact", "controls.camera", "controls.freelook",
            "controls.map", "controls.tabs", "controls.markers", "controls.menu", "controls.debug", "controls.newgame",
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
            menuButton = Btn(main, () => { confirmMenu = true; Show(confirm); });
            quit = Btn(main, () => { confirmMenu = false; Show(confirm); });

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

            // Pergunta, aviso de que o progresso sera salvo (centralizado) e SIM / NAO.
            confirm = new VisualElement();
            box.Add(confirm);
            confirmText = Lbl(confirm, "pause-confirm");
            confirmNote = Lbl(confirm, "pause-note");
            var row = new VisualElement();
            row.AddToClassList("pause-row");
            confirm.Add(row);
            yes = Btn(row, () => { if (confirmMenu) ToMainMenu(); else Quit(); });
            yes.AddToClassList("btn-danger");
            no = Btn(row, () => Show(main));

            banner = DevBanner.Add(root);

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
            lockPending = false;
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
            lockPending = ShouldLockCursorOnResume();
            LockIfReady();
        }

        // Fechando com Esc: o editor do Unity solta o cursor ao apertar Esc; trava de novo quando a tecla for solta.
        void Update()
        {
            if (lockPending) LockIfReady();
        }

        void LockIfReady()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.isPressed) return;
            lockPending = false;
            if (IsOpen) return;
            UnityEngine.Cursor.lockState = CursorLockMode.Locked;
            UnityEngine.Cursor.visible = false;
        }

        void OnDisable()
        {
            Time.timeScale = 1f;
            Loc.LanguageChanged -= OnLanguageChanged;
        }

        void Show(VisualElement page)
        {
            this.page = page;
            title.text = Loc.T(page == controls ? "pause.controls_title"
                : page == confirm ? (confirmMenu ? "pause.menu_title" : "pause.quit_title")
                : page == settings ? "pause.settings" : "pause.title");
            resume.text = Loc.T("pause.resume");
            controlsButton.text = Loc.T("pause.controls");
            settingsButton.text = Loc.T("pause.settings");
            menuButton.text = Loc.T("pause.main_menu");
            quit.text = Loc.T("pause.quit");
            back.text = Loc.T("pause.back");
            settingsBack.text = Loc.T("pause.back");
            languageLabel.text = Loc.T("pause.language");
            yes.text = Loc.T("pause.yes");
            no.text = Loc.T("pause.no");
            confirmText.text = Loc.T(confirmMenu ? "pause.menu_confirm" : "pause.quit_confirm");
            confirmNote.text = Loc.T("pause.progress_saved");
            banner.text = Loc.T("menu.dev_banner");
            controlsList.text = string.Join("\n", Array.ConvertAll(ControlKeys, k => Loc.T(k)));
            main.style.display = page == main ? DisplayStyle.Flex : DisplayStyle.None;
            controls.style.display = page == controls ? DisplayStyle.Flex : DisplayStyle.None;
            confirm.style.display = page == confirm ? DisplayStyle.Flex : DisplayStyle.None;
            settings.style.display = page == settings ? DisplayStyle.Flex : DisplayStyle.None;
        }

        void ToMainMenu()
        {
            Time.timeScale = 1f;
            BeforeQuit?.Invoke(); // salva o progresso
            SceneManager.LoadScene(MainMenu.SceneName);
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

    // Faixa vermelha de fora a fora no rodape: "PRE-ALPHA - VERSAO DE DESENVOLVIMENTO".
    public static class DevBanner
    {
        public static Label Add(VisualElement root)
        {
            var bar = new VisualElement();
            bar.AddToClassList("dev-banner");
            root.Add(bar);
            var label = new Label(Loc.T("menu.dev_banner"));
            label.AddToClassList("dev-banner-text");
            bar.Add(label);
            return label;
        }
    }
}
