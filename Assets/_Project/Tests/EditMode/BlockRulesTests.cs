using NUnit.Framework;
using static PulseFactor.Tests.TestDefs;

namespace PulseFactor.Tests
{
    public class BlockRulesTests
    {
        [Test]
        public void Exclusive_block_mounts_only_on_origin_class()
        {
            var b = Block(BlockType.Cockpit, ShipClass.Assault, multiclass: false);
            Assert.IsTrue(BlockRules.CanMount(b, ShipClass.Assault));
            Assert.IsFalse(BlockRules.CanMount(b, ShipClass.Scout));
            Assert.IsFalse(BlockRules.CanMount(b, ShipClass.Cargo));
        }

        [Test]
        public void Multiclass_block_mounts_on_any_class()
        {
            var b = Block(BlockType.Wing, ShipClass.Scout, multiclass: true);
            Assert.IsTrue(BlockRules.CanMount(b, ShipClass.Scout));
            Assert.IsTrue(BlockRules.CanMount(b, ShipClass.Cargo));
            Assert.IsTrue(BlockRules.CanMount(b, ShipClass.Assault));
        }

        [Test]
        public void Blueprint_without_body_is_invalid()
        {
            var bp = new ShipBlueprint
            {
                cockpit = Block(BlockType.Cockpit, ShipClass.Scout, true),
                tail = Block(BlockType.Tail, ShipClass.Scout, true),
                wing = Block(BlockType.Wing, ShipClass.Scout, true),
            };
            Assert.IsFalse(BlockRules.IsValid(bp, out var error));
            Assert.IsNotEmpty(error);
        }

        [Test]
        public void Blueprint_class_comes_from_body()
        {
            var bp = new ShipBlueprint
            {
                cockpit = Block(BlockType.Cockpit, ShipClass.Scout, true),
                body = Block(BlockType.Body, ShipClass.Cargo, false, Cargo()),
                tail = Block(BlockType.Tail, ShipClass.Cargo, false),
                wing = Block(BlockType.Wing, ShipClass.Cargo, false),
            };
            Assert.IsTrue(BlockRules.IsValid(bp, out var error), error);
            Assert.AreEqual(ShipClass.Cargo, bp.ShipClass);
        }

        [Test]
        public void Wing_in_body_slot_is_invalid()
        {
            var bp = new ShipBlueprint
            {
                cockpit = Block(BlockType.Cockpit, ShipClass.Scout, true),
                body = Block(BlockType.Wing, ShipClass.Scout, false, Scout()),
                tail = Block(BlockType.Tail, ShipClass.Scout, true),
                wing = Block(BlockType.Wing, ShipClass.Scout, true),
            };
            Assert.IsFalse(BlockRules.IsValid(bp, out _));
        }

        [Test]
        public void Exclusive_part_of_other_class_is_invalid()
        {
            var bp = new ShipBlueprint
            {
                cockpit = Block(BlockType.Cockpit, ShipClass.Assault, false),
                body = Block(BlockType.Body, ShipClass.Scout, false, Scout()),
                tail = Block(BlockType.Tail, ShipClass.Scout, false),
                wing = Block(BlockType.Wing, ShipClass.Scout, false),
            };
            Assert.IsFalse(BlockRules.IsValid(bp, out _));
        }
    }
}
