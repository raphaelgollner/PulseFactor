using System.Collections.Generic;
using System.IO;
using NUnit.Framework;

namespace PulseFactor.Tests
{
    public class SaveSystemTests
    {
        string path;

        [SetUp]
        public void SetUp() => path = Path.Combine(Path.GetTempPath(), "pulsefactor_test_save.json");

        [TearDown]
        public void TearDown() { if (File.Exists(path)) File.Delete(path); }

        [Test]
        public void Round_trip_preserves_everything()
        {
            var data = new SaveData
            {
                credits = 12345,
                clock = 3600.5,
                sectorId = "aster",
                px = 1.5e10, py = -2, pz = 3,
                rx = 0, ry = 0.7071f, rz = 0, rw = 0.7071f,
                cargo = new List<SaveData.CargoEntry> { new() { resourceId = "iron", count = 20, reserved = 5 } },
                reputation = new List<SaveData.RepEntry> { new() { corpId = "nova", value = 12 } },
                contracts = new List<Contract> { new() { id = "c1", type = ContractType.Requisition, resourceId = "copper", quantity = 80, delivered = 30, deadline = 7200, state = ContractState.Active } },
            };
            SaveSystem.Save(data, path);
            var back = SaveSystem.Load(path);
            Assert.AreEqual(12345, back.credits);
            Assert.AreEqual(3600.5, back.clock, 1e-9);
            Assert.AreEqual(1.5e10, back.px, 1e-3);
            Assert.AreEqual("aster", back.sectorId);
            Assert.AreEqual(5, back.cargo[0].reserved);
            Assert.AreEqual(12, back.reputation[0].value);
            Assert.AreEqual(30, back.contracts[0].delivered);
            Assert.AreEqual(ContractType.Requisition, back.contracts[0].type);
        }

        [Test]
        public void Missing_file_returns_null() => Assert.IsNull(SaveSystem.Load(path));

        [Test]
        public void Garbage_returns_null()
        {
            File.WriteAllText(path, "{{{ isto nao e json");
            Assert.IsNull(SaveSystem.Load(path));
        }

        [Test]
        public void Future_version_returns_null()
        {
            SaveSystem.Save(new SaveData { version = SaveData.CurrentVersion + 1 }, path);
            Assert.IsNull(SaveSystem.Load(path));
        }

        [Test]
        public void Delete_removes_save()
        {
            SaveSystem.Save(new SaveData(), path);
            SaveSystem.Delete(path);
            Assert.IsFalse(File.Exists(path));
        }
    }
}
