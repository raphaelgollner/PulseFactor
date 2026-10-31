using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace PulseFactor
{
    public static class ShipAssembler
    {
        static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        public static GameObject Build(ShipBlueprint bp, Transform parent)
        {
            if (!BlockRules.IsValid(bp, out var error)) throw new ArgumentException(error);

            var root = new GameObject("Ship");
            root.transform.SetParent(parent, false);
            var ship = root.AddComponent<AssembledShip>();
            BuildParts(ship, bp);
            return root;
        }

        // Troca as pecas de uma nave ja montada (mesmo GameObject: fisica, controles e camera continuam).
        public static void Rebuild(AssembledShip ship, ShipBlueprint bp)
        {
            if (!BlockRules.IsValid(bp, out var error)) throw new ArgumentException(error);
            foreach (var part in new[] { ship.Cockpit, ship.Body, ship.Tail, ship.WingL, ship.WingR })
            {
                if (!part) continue;
                part.SetParent(null, false); // sai da hierarquia ja (Destroy so acontece no fim do quadro)
                Object.Destroy(part.gameObject);
            }
            ship.WingL = ship.WingR = null;
            ship.Thrusters.Clear();
            ship.ScreenMounts.Clear();
            ship.EyePoint = null;
            var rotation = ship.transform.rotation;
            var position = ship.transform.position;
            ship.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity); // monta no espaco local
            BuildParts(ship, bp);
            ship.transform.SetPositionAndRotation(position, rotation);
        }

        static void BuildParts(AssembledShip ship, ShipBlueprint bp)
        {
            var root = ship.gameObject;
            ship.Blueprint = bp;

            ship.Body = Spawn(bp.body, "Body", root.transform);
            ship.Cockpit = Attach(bp.cockpit, "Cockpit", root.transform, Socket(ship.Body, "Socket_Front"), false);
            ship.Tail = Attach(bp.tail, "Tail", root.transform, Socket(ship.Body, "Socket_Back"), false);
            if (bp.wing) // asas sao opcionais (ex.: Hulker)
            {
                ship.WingL = Attach(bp.wing, "WingL", root.transform, Socket(ship.Body, "Socket_WingL"), false);
                ship.WingR = Attach(bp.wing, "WingR", root.transform, Socket(ship.Body, "Socket_WingR"), true);
            }

            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.name.StartsWith("Thruster_")) ship.Thrusters.Add(t);
                else if (t.name.StartsWith("ScreenMount_")) ship.ScreenMounts.Add(t);
                else if (t.name == "EyePoint") ship.EyePoint = t;
            }

            ApplyPaint(root, bp.paint ?? PaintScheme.FromManufacturers(bp));
        }

        static Transform Spawn(BlockDef def, string name, Transform parent)
        {
            var go = Object.Instantiate(def.prefab, parent, false);
            go.name = name;
            go.SetActive(true);
            return go.transform;
        }

        static Transform Socket(Transform body, string name) =>
            body.Find(name) ?? throw new ArgumentException($"Corpo sem {name}");

        // Encaixa o Socket_Attach do bloco no socket do corpo.
        static Transform Attach(BlockDef def, string name, Transform root, Transform socket, bool mirror)
        {
            var part = Spawn(def, name, root);
            part.rotation = socket.rotation;
            if (mirror) part.localScale = new Vector3(-1f, 1f, 1f);
            var attach = part.Find("Socket_Attach");
            Vector3 offset = attach ? attach.position - part.position : Vector3.zero;
            part.position = socket.position - offset;
            return part;
        }

        public static void ApplyPaint(GameObject ship, PaintScheme paint)
        {
            Paint(ship.transform.Find("Cockpit"), paint.cockpit);
            Paint(ship.transform.Find("Body"), paint.body);
            Paint(ship.transform.Find("Tail"), paint.tail);
            Paint(ship.transform.Find("WingL"), paint.wing);
            Paint(ship.transform.Find("WingR"), paint.wing);
        }

        static void Paint(Transform part, BlockPaint paint)
        {
            if (!part) return;
            var mpb = new MaterialPropertyBlock();
            foreach (var region in part.GetComponentsInChildren<PaintableRegion>(true))
            {
                var r = region.GetComponent<Renderer>();
                if (!r) continue;
                r.GetPropertyBlock(mpb);
                mpb.SetColor(BaseColor, paint.Channel(region.channel));
                r.SetPropertyBlock(mpb);
            }
        }
    }
}
