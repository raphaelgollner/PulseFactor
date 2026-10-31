using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace PulseFactor.Tests
{
    // Playtest fase 2: as abas do terminal (canto superior esquerdo) nao recebiam clique porque os
    // paineis do cockpit, desenhados em textura, capturavam o input como se ocupassem a tela.
    public class CockpitPanelInputTests
    {
        [UnityTest]
        public IEnumerator Cockpit_panels_do_not_capture_screen_clicks()
        {
            var ship = ShipAssembler.Build(PlayModeFixtures.Blueprint(ShipClass.Scout), null);
            var assembled = ship.GetComponent<AssembledShip>();
            var mount = new GameObject("ScreenMount_Center").transform;
            mount.SetParent(ship.transform, false);
            assembled.ScreenMounts.Add(mount);

            var panels = new GameObject("panels").AddComponent<CockpitPanels>();
#if UNITY_EDITOR
            // As configuracoes reais (com tema): as vazias nao ganham layout e o teste passava sem reproduzir o bug.
            var real = UnityEditor.AssetDatabase.LoadAssetAtPath<PanelSettings>("Assets/_Project/UI/Panel_Cockpit.asset");
            if (!real) Assert.Ignore("Panel_Cockpit.asset ainda nao gerado");
            panels.BaseSettings = real;
#endif
            panels.Build(assembled);
            yield return null;
            yield return null;

            var doc = panels.GetComponentInChildren<UIDocument>();
            Assert.IsNotNull(doc, "painel criado");
            var panel = doc.rootVisualElement.panel;
            // Um ponto no canto superior esquerdo da tela (onde ficam as abas do terminal).
            var screenPoint = new Vector2(150, 150); // ScreenToPanel usa origem no topo, como no jogo
            var panelPoint = RuntimePanelUtils.ScreenToPanel(panel, screenPoint);
            Assert.IsNull(panel.Pick(panelPoint), "o painel do cockpit nao pode pegar cliques da tela");

            Object.Destroy(panels.gameObject);
            Object.Destroy(ship);
        }
    }
}
