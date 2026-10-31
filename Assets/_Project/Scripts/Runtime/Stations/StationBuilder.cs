using UnityEngine;

namespace PulseFactor
{
    // Estacao provisoria em primitivas: eixo central, anel e 4 baias de doca abertas.
    public static class StationBuilder
    {
        public static Station Build(StationDef def, GameConfig config, WorldPosition position, Material hull, Material light)
        {
            var root = new GameObject("Station_" + def.id);
            var anchor = root.AddComponent<WorldAnchor>();
            var station = root.AddComponent<Station>();

            Part(root.transform, PrimitiveType.Cylinder, "Spine", Vector3.zero, new Vector3(80, 160, 80), Vector3.zero, hull);
            Part(root.transform, PrimitiveType.Sphere, "Core", Vector3.zero, Vector3.one * 160, Vector3.zero, hull);
            Part(root.transform, PrimitiveType.Cylinder, "Antenna", new Vector3(0, 260, 0), new Vector3(6, 100, 6), Vector3.zero, hull);

            const float ringRadius = 340f;
            const int segments = 16;
            for (int i = 0; i < segments; i++)
            {
                float a = i * 360f / segments;
                var dir = Quaternion.Euler(0, a, 0) * Vector3.forward;
                bool isBay = i % 4 == 0;
                if (!isBay)
                    Part(root.transform, PrimitiveType.Cube, "Ring" + i, dir * ringRadius, new Vector3(140, 50, 60), new Vector3(0, a + 90, 0), hull);
                Part(root.transform, PrimitiveType.Cube, "Strut" + i, dir * (ringRadius * 0.5f), new Vector3(12, 12, ringRadius), new Vector3(0, a, 0), hull);
            }

            var docks = new Dock[4];
            for (int d = 0; d < 4; d++)
            {
                float a = d * 90f;
                var dir = Quaternion.Euler(0, a, 0) * Vector3.forward;
                var bay = new GameObject("Dock" + d).transform;
                bay.SetParent(root.transform, false);
                bay.localPosition = dir * ringRadius;
                bay.localRotation = Quaternion.LookRotation(-dir, Vector3.up);

                // Baia: piso e paredes, aberta para fora e para dentro.
                Part(bay, PrimitiveType.Cube, "Floor", new Vector3(0, -25, 0), new Vector3(120, 4, 60), Vector3.zero, hull);
                Part(bay, PrimitiveType.Cube, "Roof", new Vector3(0, 25, 0), new Vector3(120, 4, 60), Vector3.zero, hull);
                Part(bay, PrimitiveType.Cube, "WallL", new Vector3(-60, 0, 0), new Vector3(4, 50, 60), Vector3.zero, hull);
                Part(bay, PrimitiveType.Cube, "WallR", new Vector3(60, 0, 0), new Vector3(4, 50, 60), Vector3.zero, hull);

                var dock = bay.gameObject.AddComponent<Dock>();
                dock.index = d;
                dock.LandingPoint = Point(bay, "LandingPoint", new Vector3(0, -18, 0));
                dock.ApproachPoint = Point(bay, "ApproachPoint", new Vector3(0, -10, -150));
                var marker = Part(bay, PrimitiveType.Cylinder, "Marker", new Vector3(0, -22.5f, 0), new Vector3(20, 0.5f, 20), Vector3.zero, light).gameObject;
                Object.Destroy(marker.GetComponent<Collider>());
                dock.Marker = marker;
                marker.SetActive(false);
                docks[d] = dock;
            }

            station.Initialize(def, config, docks);
            anchor.SetWorld(position);
            return station;
        }

        static Transform Part(Transform parent, PrimitiveType type, string name, Vector3 pos, Vector3 scale, Vector3 rot, Material m)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            var t = go.transform;
            t.SetParent(parent, false);
            t.localPosition = pos;
            t.localEulerAngles = rot;
            t.localScale = scale;
            if (m) go.GetComponent<Renderer>().sharedMaterial = m;
            return t;
        }

        static Transform Point(Transform parent, string name, Vector3 pos)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            t.localPosition = pos;
            return t;
        }
    }
}
