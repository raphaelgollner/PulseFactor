#if UNITY_EDITOR
using System.Collections;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace PulseFactor.Tests
{
    // Cena do menu principal gerada por "PulseFactor/Generate Slice Content".
    public class MainMenuTests
    {
        const string ScenePath = "Assets/_Project/Scenes/MainMenu.unity";

        [UnityTest]
        public IEnumerator Menu_has_panel_settings_and_buttons()
        {
            if (!System.IO.File.Exists(ScenePath)) Assert.Ignore("Cena MainMenu ainda nao gerada");
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            var menu = Object.FindAnyObjectByType<MainMenu>();
            Assert.IsNotNull(menu, "MainMenu na cena");
            var doc = menu.GetComponent<UIDocument>();
            Assert.IsNotNull(doc.panelSettings, "sem PanelSettings o menu fica preto");
            Assert.GreaterOrEqual(doc.rootVisualElement.Query<Button>().ToList().Count, 4);
            Assert.IsNotNull(doc.rootVisualElement.Q(className: "dev-banner"));
        }
    }
}
#endif
