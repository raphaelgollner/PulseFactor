using UnityEngine;

namespace PulseFactor
{
    [CreateAssetMenu(menuName = "PulseFactor/Game Config")]
    public class GameConfig : ScriptableObject
    {
        // Modo de teste: comprar nao desconta creditos e a reputacao fica zerada.
        public bool testMode = true;
        public int startingCredits = 1000;
    }
}
