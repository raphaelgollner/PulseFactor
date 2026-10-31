using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace PulseFactor
{
    [Serializable]
    public class SaveData
    {
        public const int CurrentVersion = 1;

        [Serializable] public class CargoEntry { public string resourceId; public int count; public int reserved; }
        [Serializable] public class RepEntry { public string corpId; public int value; }

        public int version = CurrentVersion;
        public long credits;
        public double clock;
        public string sectorId;
        public double px, py, pz;
        public float rx, ry, rz, rw = 1f;
        public string routeSectorId, routeTargetId;
        public string shipName;
        public List<CargoEntry> cargo = new();
        public List<RepEntry> reputation = new();
        public List<Contract> contracts = new();
        public List<Economy.StockEntry> stocks = new(); // vazio em saves antigos: estoques iniciais
    }

    // Save em JSON. Ausente, corrompido ou de versao futura: retorna null (o jogo comeca do zero).
    public static class SaveSystem
    {
        public static string DefaultPath => Path.Combine(Application.persistentDataPath, "save.json");

        public static void Save(SaveData data, string path)
        {
            var tmp = path + ".tmp";
            File.WriteAllText(tmp, JsonUtility.ToJson(data, true));
            if (File.Exists(path)) File.Delete(path);
            File.Move(tmp, path); // grava inteiro ou nada
        }

        public static SaveData Load(string path)
        {
            if (!File.Exists(path)) return null;
            try
            {
                var data = JsonUtility.FromJson<SaveData>(File.ReadAllText(path));
                if (data == null || data.version > SaveData.CurrentVersion || data.version <= 0) return null;
                return data;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[PulseFactor] Save ignorado ({e.Message})");
                return null;
            }
        }

        public static void Delete(string path)
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
