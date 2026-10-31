using UnityEngine;

namespace PulseFactor.Tests
{
    // Blocos de teste montados em codigo (templates inativos na cena), sem depender de assets.
    public static class PlayModeFixtures
    {
        public static ShipClassDef Class(ShipClass c, float accel = 40, float maxSpeed = 220, float turn = 90, int cargo = 50)
        {
            var d = ScriptableObject.CreateInstance<ShipClassDef>();
            d.shipClass = c; d.acceleration = accel; d.maxSpeed = maxSpeed; d.turnRateDeg = turn; d.cargoCapacity = cargo;
            return d;
        }

        public static BlockDef Block(BlockType type, ShipClass origin, bool multiclass, ShipClassDef classDef = null)
        {
            var b = ScriptableObject.CreateInstance<BlockDef>();
            b.id = $"{origin}_{type}";
            b.type = type;
            b.originClass = origin;
            b.multiclass = multiclass;
            b.classDef = classDef;
            b.prefab = Template(type, origin);
            return b;
        }

        static GameObject Template(BlockType type, ShipClass origin)
        {
            var root = new GameObject($"tpl_{origin}_{type}");
            root.SetActive(false);
            var mesh = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mesh.transform.SetParent(root.transform, false);
            mesh.AddComponent<PaintableRegion>().channel = 0;
            if (type == BlockType.Body)
            {
                float half = origin == ShipClass.Cargo ? 5f : 4f;
                Socket(root, "Socket_Front", new Vector3(0, 0, half));
                Socket(root, "Socket_Back", new Vector3(0, 0, -half));
                Socket(root, "Socket_WingL", new Vector3(-half / 2f, 0, -1));
                Socket(root, "Socket_WingR", new Vector3(half / 2f, 0, -1));
            }
            else
            {
                Socket(root, "Socket_Attach", Vector3.zero);
                Socket(root, "Thruster_0", new Vector3(0, 0, -1));
            }
            if (type == BlockType.Cockpit)
            {
                var interior = Socket(root, "Interior", Vector3.zero);
                Socket(interior, "EyePoint", new Vector3(0, 0.2f, 1));
                var ext = Socket(root, "Exterior", Vector3.zero);
                ext.layer = Mathf.Max(0, LayerMask.NameToLayer("ShipExteriorCockpit"));
            }
            return root;
        }

        static GameObject Socket(GameObject parent, string name, Vector3 pos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            go.transform.localPosition = pos;
            return go;
        }

        public static ShipBlueprint Blueprint(ShipClass bodyClass, bool multiclassParts = true, ShipClass partsOrigin = ShipClass.Scout)
        {
            var cls = Class(bodyClass);
            return new ShipBlueprint
            {
                cockpit = Block(BlockType.Cockpit, partsOrigin, multiclassParts),
                body = Block(BlockType.Body, bodyClass, false, cls),
                tail = Block(BlockType.Tail, partsOrigin, multiclassParts),
                wing = Block(BlockType.Wing, partsOrigin, multiclassParts),
            };
        }
    }
}
