using System;
using System.IO;
using System.Collections.Generic;
using System.Text.Json;
using Shin_Megami_Tensei_Models;

namespace Shin_Megami_Tensei
{
    internal static class MonsterFileReader
    {
        private static Dictionary<string, Stats>? _statsByName;
        private static Dictionary<string, AffinityProfile>? _affinityByName;
        private static Dictionary<string, List<string>>? _skillsByName;
        

        public static void LoadMonsterDataFromFile(string referenceFilePath)
        {
            if (IsLoaded()) return;

            var jsonPath = ResolveMonstersJsonPath(referenceFilePath);
            _statsByName    = LoadStatsMapOrEmpty(jsonPath);
            _affinityByName = LoadAffinityMapOrEmpty(jsonPath);
            _skillsByName   = LoadSkillsMapOrEmpty(jsonPath);
        }

        public static bool GetStats(string name, out Stats stats)
        {
            if (_statsByName != null && _statsByName.TryGetValue(NormalizeName(name), out stats))
                return true;
            stats = default!;
            return false;
        }

        public static bool GetAffinities(string name, out AffinityProfile affinities)
        {
            if (_affinityByName != null && _affinityByName.TryGetValue(NormalizeName(name), out affinities))
                return true;
            affinities = AffinityProfile.NeutralAll;
            return false;
        }

        public static bool GetSkillNames(string name, out IReadOnlyList<string> skillNames)
        {
            skillNames = Array.Empty<string>();
            if (_skillsByName != null && _skillsByName.TryGetValue(NormalizeName(name), out var list))
            { skillNames = list; return true; }
            return false;
        }
        

        private static bool IsLoaded() =>
            _statsByName != null && _affinityByName != null && _skillsByName != null;

        private static string? ResolveMonstersJsonPath(string referenceFilePath) =>
            FindMonstersJsonNear(referenceFilePath) ??
            FindMonstersJsonNear(AppContext.BaseDirectory);

        private static string? FindMonstersJsonNear(string? anyPathInTree)
        {
            if (string.IsNullOrWhiteSpace(anyPathInTree)) return null;

            var dir = ResolveStartDirectory(anyPathInTree);
            foreach (var current in WalkUpDirectories(dir, 5))
            {
                var rootCandidate  = Path.Combine(current, "monsters.json");
                if (File.Exists(rootCandidate)) return rootCandidate;

                var dataCandidate = Path.Combine(Path.Combine(current, "data"), "monsters.json");
                if (File.Exists(dataCandidate)) return dataCandidate;
            }
            return null;
        }

        private static string ResolveStartDirectory(string path) =>
            File.Exists(path) ? Path.GetDirectoryName(path)! : path;

        private static IEnumerable<string> WalkUpDirectories(string startDir, int maxLevels)
        {
            var current = startDir;
            for (int i = 0; i < maxLevels && !string.IsNullOrEmpty(current); i++)
            {
                yield return current;
                current = Path.GetDirectoryName(current);
            }
        }

        private static string NormalizeName(string name)
        {
            var trimmed = name.Trim();
            return trimmed.EndsWith(".", StringComparison.Ordinal) ? trimmed[..^1].Trim() : trimmed;
        }

        // =============== Carga desde JSON ===============

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private static List<MonsterJson> DeserializeMonsterList(string json) =>
            JsonSerializer.Deserialize<List<MonsterJson>>(json, JsonOptions) ?? new List<MonsterJson>();

        private static Dictionary<string, Stats> LoadStatsMapOrEmpty(string? jsonPath)
        {
            if (jsonPath is null) return NewStatsMap();
            var json  = File.ReadAllText(jsonPath);
            var items = DeserializeMonsterList(json);
            return BuildStatsMap(items);
        }

        private static Dictionary<string, AffinityProfile> LoadAffinityMapOrEmpty(string? jsonPath)
        {
            if (jsonPath is null) return NewAffinityMap();
            var json  = File.ReadAllText(jsonPath);
            var items = DeserializeMonsterList(json);
            return BuildAffinitiesMap(items);
        }

        private static Dictionary<string, List<string>> LoadSkillsMapOrEmpty(string? jsonPath)
        {
            var map = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            if (jsonPath is null) return map;

            var json  = File.ReadAllText(jsonPath);
            var items = DeserializeMonsterList(json);
            foreach (var it in items)
            {
                if (!HasValidName(it)) continue;
                var key   = NormalizeName(it!.name!);
                var names = it.skills ?? new List<string>();
                var list  = new List<string>();
                foreach (var s in names)
                {
                    var t = (s ?? string.Empty).Trim();
                    if (t.Length > 0) list.Add(t);
                }
                map[key] = list;
            }
            return map;
        }

        private static Dictionary<string, Stats> NewStatsMap() =>
            new(StringComparer.OrdinalIgnoreCase);

        private static Dictionary<string, AffinityProfile> NewAffinityMap() =>
            new(StringComparer.OrdinalIgnoreCase);

        private static Dictionary<string, Stats> BuildStatsMap(List<MonsterJson> items)
        {
            var map = NewStatsMap();
            foreach (var item in items)
                AddStatsEntry(map, item);
            return map;
        }

        private static void AddStatsEntry(Dictionary<string, Stats> map, MonsterJson? item)
        {
            if (!CreateStatsEntry(item, out var key, out var stats)) return;
            map[key] = stats;
        }

        private static bool CreateStatsEntry(MonsterJson? item, out string key, out Stats stats)
        {
            key = string.Empty;
            stats = default!;
            if (!HasValidName(item)) return false;
            if (item!.stats is null) return false;

            key   = NormalizeName(item.name!);
            stats = CreateStats(item.stats);
            return true;
        }

        private static bool HasValidName(MonsterJson? item) =>
            !string.IsNullOrWhiteSpace(item?.name);

        private static Stats CreateStats(MonsterStatsJson stats) =>
            new(hp: stats.HP, mp: stats.MP, str: stats.Str, skl: stats.Skl, mag: stats.Mag, spd: stats.Spd, lck: stats.Lck);

        // =============== Afinidades ===============

        private static Dictionary<string, AffinityProfile> BuildAffinitiesMap(List<MonsterJson> items)
        {
            var map = NewAffinityMap();
            foreach (var item in items)
            {
                if (!HasValidName(item)) continue;
                var key  = NormalizeName(item!.name!);
                var prof = CreateAffinityProfile(item.affinity);
                map[key] = prof;
            }
            return map;
        }

        private static AffinityProfile CreateAffinityProfile(Dictionary<string, string>? src)
        {
            if (src == null) return AffinityProfile.NeutralAll;

            var dict = new Dictionary<Element, Affinity>();

            void add(string jsonKey, Element element)
            {
                if (!src.TryGetValue(jsonKey, out var code)) return;
                dict[element] = MapCodeToAffinity(code);
            }

            add("Phys",  Element.Phys);
            add("Gun",   Element.Gun);
            add("Fire",  Element.Fire);
            add("Ice",   Element.Ice);
            add("Elec",  Element.Elec);
            add("Force", Element.Force);
            add("Light", Element.Light);
            add("Dark",  Element.Dark);
            add("Bind",  Element.Bind);
            add("Sleep", Element.Sleep);
            add("Sick",  Element.Sick);
            add("Panic", Element.Panic);
            add("Poison",Element.Poison);

            return new AffinityProfile(dict);
        }

        private static Affinity MapCodeToAffinity(string? code) =>
            (code ?? "-").Trim() switch
            {
                "Wk" => Affinity.Weak,
                "Rs" => Affinity.Resist,
                "Nu" => Affinity.Null,
                "Rp" => Affinity.Repel,
                "Dr" => Affinity.Drain,
                _    => Affinity.Neutral
            };

        // =============== Tipos JSON ===============

        private sealed class MonsterJson
        {
            public string? name { get; set; }
            public Dictionary<string, string>? affinity { get; set; }
            public MonsterStatsJson? stats { get; set; }
            public List<string>? skills { get; set; } // <-- NUEVO
        }

        private sealed class MonsterStatsJson
        {
            public int HP  { get; set; }
            public int MP  { get; set; }
            public int Str { get; set; }
            public int Skl { get; set; }
            public int Mag { get; set; }
            public int Spd { get; set; }
            public int Lck { get; set; }
        }
    }
}
