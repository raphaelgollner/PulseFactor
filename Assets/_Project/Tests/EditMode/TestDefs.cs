using UnityEngine;

namespace PulseFactor.Tests
{
    // Fabrica de definicoes para testes, sem depender dos assets gerados.
    public static class TestDefs
    {
        public static ShipClassDef Class(ShipClass c, float accel, float maxSpeed, float turn, int cargo)
        {
            var d = ScriptableObject.CreateInstance<ShipClassDef>();
            d.shipClass = c;
            d.acceleration = accel;
            d.maxSpeed = maxSpeed;
            d.turnRateDeg = turn;
            d.cargoCapacity = cargo;
            return d;
        }

        public static ShipClassDef Scout() => Class(ShipClass.Scout, 40, 220, 90, 50);
        public static ShipClassDef Assault() => Class(ShipClass.Assault, 30, 180, 75, 10);
        public static ShipClassDef Cargo() => Class(ShipClass.Cargo, 15, 130, 45, 500);

        public static BlockDef Block(BlockType type, ShipClass origin, bool multiclass, ShipClassDef classDef = null, string id = null)
        {
            var b = ScriptableObject.CreateInstance<BlockDef>();
            b.id = id ?? origin + "_" + type + (multiclass ? "_multi" : "_excl");
            b.type = type;
            b.originClass = origin;
            b.multiclass = multiclass;
            b.classDef = classDef;
            return b;
        }

        public static ResourceDef Resource(string id, int price)
        {
            var r = ScriptableObject.CreateInstance<ResourceDef>();
            r.id = id;
            r.basePrice = price;
            return r;
        }
    }
}
