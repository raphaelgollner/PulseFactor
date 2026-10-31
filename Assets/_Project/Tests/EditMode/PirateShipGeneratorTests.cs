using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using static PulseFactor.Tests.TestDefs;

namespace PulseFactor.Tests
{
    public class PirateShipGeneratorTests
    {
        List<BlockDef> catalog;

        // Catalogo no formato real: 6 modelos, multiclasse = cruise, typec, horder.
        [SetUp]
        public void SetUp()
        {
            catalog = new List<BlockDef>();
            Model("crusader", ShipClass.Scout, false, Scout());
            Model("cruise", ShipClass.Scout, true, Scout());
            Model("rapidfire", ShipClass.Assault, false, Assault());
            Model("typea", ShipClass.Assault, false, Assault());
            Model("typec", ShipClass.Cargo, true, Cargo());
            Model("horder", ShipClass.Cargo, true, Cargo());
        }

        void Model(string id, ShipClass c, bool multi, ShipClassDef def)
        {
            catalog.Add(Block(BlockType.Cockpit, c, multi, null, id + "_cockpit"));
            catalog.Add(Block(BlockType.Body, c, multi, def, id + "_body"));
            catalog.Add(Block(BlockType.Tail, c, multi, null, id + "_tail"));
            catalog.Add(Block(BlockType.Wing, c, multi, null, id + "_wing"));
        }

        [Test]
        public void Body_is_always_assault()
        {
            var gen = new PirateShipGenerator(catalog);
            for (int seed = 0; seed < 200; seed++)
                Assert.AreEqual(ShipClass.Assault, gen.Generate(new System.Random(seed)).ShipClass);
        }

        [Test]
        public void All_parts_mountable_on_assault()
        {
            var gen = new PirateShipGenerator(catalog);
            for (int seed = 0; seed < 200; seed++)
            {
                var bp = gen.Generate(new System.Random(seed));
                Assert.IsTrue(BlockRules.IsValid(bp, out var error), error);
                foreach (var b in new[] { bp.cockpit, bp.tail, bp.wing })
                    StringAssert.DoesNotStartWith("crusader", b.id);
            }
        }

        [Test]
        public void Uses_multiclass_parts_sometimes()
        {
            var gen = new PirateShipGenerator(catalog);
            bool found = false;
            for (int seed = 0; seed < 200 && !found; seed++)
            {
                var bp = gen.Generate(new System.Random(seed));
                found = bp.cockpit.multiclass || bp.tail.multiclass || bp.wing.multiclass;
            }
            Assert.IsTrue(found);
        }

        [Test]
        public void Missing_assault_body_throws()
        {
            catalog.RemoveAll(b => b.type == BlockType.Body && b.originClass == ShipClass.Assault);
            var gen = new PirateShipGenerator(catalog);
            Assert.Throws<InvalidOperationException>(() => gen.Generate(new System.Random(1)));
        }

        [Test]
        public void Palette_only_red_black_white()
        {
            var allowed = new[] { PiratePalette.Red, PiratePalette.Black, PiratePalette.White };
            for (int seed = 0; seed < 200; seed++)
            {
                var p = PiratePalette.Generate(new System.Random(seed));
                foreach (var bp in new[] { p.cockpit, p.body, p.tail, p.wing })
                    foreach (var c in new[] { bp.primary, bp.secondary, bp.accent })
                        CollectionAssert.Contains(allowed, c);
            }
        }

        [Test]
        public void Palette_produces_single_and_mixed()
        {
            bool single = false, mixed = false;
            for (int seed = 0; seed < 200; seed++)
            {
                var p = PiratePalette.Generate(new System.Random(seed));
                var set = new HashSet<Color> { p.cockpit.primary, p.body.primary, p.tail.primary, p.wing.primary,
                    p.cockpit.secondary, p.body.secondary, p.tail.secondary, p.wing.secondary };
                if (set.Count == 1) single = true; else mixed = true;
            }
            Assert.IsTrue(single && mixed);
        }

        [Test]
        public void Single_mode_accent_differs_from_base()
        {
            for (int seed = 0; seed < 200; seed++)
            {
                var p = PiratePalette.Generate(new System.Random(seed));
                var bases = new HashSet<Color> { p.cockpit.primary, p.body.primary, p.tail.primary, p.wing.primary,
                    p.cockpit.secondary, p.body.secondary, p.tail.secondary, p.wing.secondary };
                var accents = new HashSet<Color> { p.cockpit.accent, p.body.accent, p.tail.accent, p.wing.accent };
                if (bases.Count == 1 && accents.Count == 1) // modo "uma cor so"
                    Assert.AreNotEqual(p.body.primary, p.body.accent);
            }
        }

        [Test]
        public void Same_seed_same_ship()
        {
            var gen = new PirateShipGenerator(catalog);
            var a = gen.Generate(new System.Random(42));
            var b = gen.Generate(new System.Random(42));
            Assert.AreSame(a.cockpit, b.cockpit);
            Assert.AreSame(a.body, b.body);
            Assert.AreSame(a.tail, b.tail);
            Assert.AreSame(a.wing, b.wing);
            Assert.AreEqual(a.paint.body.primary, b.paint.body.primary);
        }
    }
}
