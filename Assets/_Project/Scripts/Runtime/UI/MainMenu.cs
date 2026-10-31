using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace PulseFactor
{
    // Menu principal basico: CONTINUAR (se houver save), NOVO JOGO, OPCOES (idioma) e SAIR.
    // Nome do jogo no topo e a faixa de pre-alpha embaixo.
    [RequireComponent(typeof(UIDocument))]
    public class MainMenu : MonoBehaviour
    {
        public const string SceneName = "MainMenu";
        public const string GameScene = "Slice";

        VisualElement root, main, options, confirm, page;
        Label subtitle, languageLabel, confirmText, confirmNote, banner;
        Button continueButton, newGame, optionsButton, quit, optionsBack, yes, no;
        bool confirmNewGame; // confirmacao de novo jogo (senao, de sair)

        void OnEnable()
        {
            Time.timeScale = 1f;
            UnityEngine.Cursor.lockState = CursorLockMode.None;
            UnityEngine.Cursor.visible = true;

            root = GetComponent<UIDocument>().rootVisualElement;
            root.Clear();
            var sheet = Resources.Load<StyleSheet>("PulseUI");
            if (sheet) root.styleSheets.Add(sheet);

            var screen = new VisualElement();
            screen.AddToClassList("menu-screen");
            root.Add(screen);
            var logo = new Label("PULSEFACTOR");
            logo.AddToClassList("menu-logo");
            screen.Add(logo);
            subtitle = Lbl(screen, "menu-subtitle");

            var box = new VisualElement();
            box.AddToClassList("menu-box");
            screen.Add(box);

            main = new VisualElement();
            box.Add(main);
            continueButton = Btn(main, Continue);
            newGame = Btn(main, () =>
            {
                if (HasSave) { confirmNewGame = true; Show(confirm); }
                else StartNewGame();
            });
            optionsButton = Btn(main, () => Show(options));
            quit = Btn(main, () => { confirmNewGame = false; Show(confirm); });

            options = new VisualElement();
            box.Add(options);
            languageLabel = Lbl(options, "pause-confirm");
            var langs = Row(options);
            Btn(langs, () => Loc.SetLanguage("pt-BR")).text = "Português";
            Btn(langs, () => Loc.SetLanguage("en")).text = "English";
            optionsBack = Btn(options, () => Show(main));

            confirm = new VisualElement();
            box.Add(confirm);
            confirmText = Lbl(confirm, "pause-confirm");
            confirmNote = Lbl(confirm, "pause-note");
            var row = Row(confirm);
            yes = Btn(row, () => { if (confirmNewGame) StartNewGame(); else Quit(); });
            yes.AddToClassList("btn-danger");
            no = Btn(row, () => Show(main));

            banner = DevBanner.Add(root);
            Loc.LanguageChanged += Refresh;
            Show(main);
        }

        void OnDisable() => Loc.LanguageChanged -= Refresh;

        static bool HasSave => SaveSystem.Load(SaveSystem.DefaultPath) != null;

        void Refresh() => Show(page ?? main);

        void Show(VisualElement p)
        {
            page = p;
            subtitle.text = Loc.T("menu.subtitle");
            continueButton.text = Loc.T("menu.continue");
            continueButton.SetEnabled(HasSave);
            newGame.text = Loc.T("menu.new_game");
            optionsButton.text = Loc.T("menu.options");
            quit.text = Loc.T("pause.quit");
            optionsBack.text = Loc.T("pause.back");
            languageLabel.text = Loc.T("pause.language");
            confirmText.text = Loc.T(confirmNewGame ? "menu.new_game_confirm" : "menu.quit_confirm");
            confirmNote.text = Loc.T(confirmNewGame ? "menu.new_game_note" : "");
            confirmNote.style.display = confirmNewGame ? DisplayStyle.Flex : DisplayStyle.None;
            yes.text = Loc.T("pause.yes");
            no.text = Loc.T("pause.no");
            banner.text = Loc.T("menu.dev_banner");
            main.style.display = p == main ? DisplayStyle.Flex : DisplayStyle.None;
            options.style.display = p == options ? DisplayStyle.Flex : DisplayStyle.None;
            confirm.style.display = p == confirm ? DisplayStyle.Flex : DisplayStyle.None;
        }

        void Continue()
        {
            if (HasSave) SceneManager.LoadScene(GameScene);
        }

        void StartNewGame()
        {
            SaveSystem.Delete(SaveSystem.DefaultPath);
            SceneManager.LoadScene(GameScene);
        }

        static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
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
