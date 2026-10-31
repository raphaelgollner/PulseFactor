using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Localization;

namespace PulseFactor
{
    // Posicao relativa a um corpo celeste do setor.
    [Serializable]
    public struct Placement
    {
        public string parentId;
        public double distance;
        public float angleDeg;
        public float elevationDeg;
    }

    [Serializable]
    public class StationPlacement
    {
        public StationDef def;
        public Placement at;
        [Tooltip("Opcional: o topo da estacao aponta para esta outra estacao do setor (ex.: Apollo -> Gollner).")]
        public string faceTowardStationId;
    }

    [Serializable]
    public class FieldPlacement
    {
        public AsteroidFieldDef def;
        public Placement at;
        public bool pirates = true;
    }

    [Serializable]
    public class GatePlacement
    {
        public string id;
        public Placement at;
        public SectorDef target;
        public string targetGateId;
    }

    // Um setor: a vizinhanca de um planeta (luas, estacoes, campos, gates).
    [CreateAssetMenu(menuName = "PulseFactor/Sector")]
    public class SectorDef : ScriptableObject
    {
        public string id;
        public LocalizedString displayName;
        public string dominantCorporationId; // ex.: aster -> nova

        [Header("Sol ao fundo")]
        public Vector3 sunDirection = new(0.3f, 0.2f, 1f);
        public double sunDistance = 1.5e11;
        public double sunRadius = 7e8;

        public List<SolarSystemDef.Body> bodies = new();
        public List<StationPlacement> stations = new();
        public List<FieldPlacement> fields = new();
        public List<GatePlacement> gates = new();

        public Dictionary<string, double3> BodyPositions() => Orbits.Resolve(bodies);

        public WorldPosition Resolve(Placement p) => Resolve(p, BodyPositions());

        public static WorldPosition Resolve(Placement p, Dictionary<string, double3> bodyPositions)
        {
            if (!bodyPositions.TryGetValue(p.parentId ?? "", out var parent))
                throw new ArgumentException($"Corpo '{p.parentId}' nao existe no setor");
            return new WorldPosition(parent + Orbits.Direction(p.angleDeg, p.elevationDeg) * p.distance);
        }

        public GatePlacement Gate(string gateId) => gates.Find(g => g.id == gateId);
    }
}
