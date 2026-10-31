using UnityEngine;
using UnityEngine.UIElements;

namespace PulseFactor
{
    // Anel laranja em volta da mira: carga das armas. Atirar esvazia, recarrega com o tempo
    // (as armas chegam na fase de combate; ate la fica cheio).
    public class WeaponChargeRing : VisualElement
    {
        static readonly Color Track = new(1f, 0.55f, 0.1f, 0.18f);
        static readonly Color Fill = new(1f, 0.55f, 0.1f, 0.9f);
        const float Width = 3f;

        float fraction = 1f;

        public float Fraction
        {
            get => fraction;
            set
            {
                value = Mathf.Clamp01(value);
                if (Mathf.Approximately(value, fraction)) return;
                fraction = value;
                MarkDirtyRepaint();
            }
        }

        public WeaponChargeRing()
        {
            pickingMode = PickingMode.Ignore;
            AddToClassList("weapon-ring");
            generateVisualContent += Draw;
        }

        void Draw(MeshGenerationContext ctx)
        {
            var r = contentRect;
            var center = r.center;
            float radius = Mathf.Min(r.width, r.height) / 2f - Width;
            var p = ctx.painter2D;
            p.lineWidth = Width;
            p.lineCap = LineCap.Butt;

            p.strokeColor = Track;
            p.BeginPath();
            p.Arc(center, radius, 0f, 360f);
            p.Stroke();

            if (fraction <= 0f) return;
            // Comeca no topo e esvazia no sentido anti-horario.
            p.strokeColor = Fill;
            p.BeginPath();
            p.Arc(center, radius, -90f, -90f + 360f * fraction);
            p.Stroke();
        }
    }
}
