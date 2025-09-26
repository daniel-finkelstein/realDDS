using System;
using System.IO;
using System.Collections.Generic;
using System.Text.Json;
using Shin_Megami_Tensei_Models;

namespace Shin_Megami_Tensei
{
    //sin IA pero basado en samurai
    internal static class MonsterFileReader
    {
        private static Dictionary<string, Stats>? _statsByName;
        
        public static void EnsureLoadedFor(string referenceFilePath)
        {
            if (IsLoaded()) return;
            var jsonPath = ResolveMonstersJsonPath(referenceFilePath);
            _statsByName = LoadStatsMapOrEmpty(jsonPath);
        }

        public static bool TryGetStats(string name, out Stats stats)
        {
            if (HasMap() && _statsByName!.TryGetValue(NormalizeName(name), out stats))
                return true;

            stats = default!;
            return false;
        }

        public static bool TryCreate(string name, out Monster monster)
        {
            if (TryGetStats(name, out var stats))
            {
                monster = new Monster(NormalizeName(name), stats);
                return true;
            }
            monster = default!;
            return false;
        }
        
        private static bool IsLoaded() => _statsByName != null;
        private static bool HasMap() => _statsByName != null;
        
        private static string? ResolveMonstersJsonPath(string referenceFilePath) =>
            TryFindMonstersJsonNear(referenceFilePath) ??
            TryFindMonstersJsonNear(AppContext.BaseDirectory);

        private static string? TryFindMonstersJsonNear(string? anyPathInTree)
        {
            if (string.IsNullOrWhiteSpace(anyPathInTree)) return null;

            var dir = ResolveStartDirectory(anyPathInTree);
            foreach (var current in WalkUpDirectories(dir, 5))
            {
                var rootCandidate = CombineFile(current, "monsters.json");
                if (File.Exists(rootCandidate)) return rootCandidate;

                var dataCandidate = CombineFile(Path.Combine(current, "data"), "monsters.json");
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
                current = ParentDirectoryOf(current);
            }
        }

        private static string? ParentDirectoryOf(string directory) => Path.GetDirectoryName(directory);
        private static string CombineFile(string directory, string file) => Path.Combine(directory, file);
        
        private static Dictionary<string, Stats> LoadStatsMapOrEmpty(string? jsonPath) =>
            jsonPath is null ? NewStatsMap() : LoadFromJson(jsonPath);

        private static Dictionary<string, Stats> NewStatsMap() =>
            new(StringComparer.OrdinalIgnoreCase);

        private static Dictionary<string, Stats> LoadFromJson(string path)
        {
            var json = ReadAllText(path);
            var items = DeserializeMonsterList(json);
            return BuildStatsMap(items);
        }

        private static string ReadAllText(string path) => File.ReadAllText(path);

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private static List<MonsterJson> DeserializeMonsterList(string json) =>
            JsonSerializer.Deserialize<List<MonsterJson>>(json, JsonOptions) ?? new List<MonsterJson>();

        private static Dictionary<string, Stats> BuildStatsMap(List<MonsterJson> items)
        {
            var map = NewStatsMap();
            foreach (var item in items)
                TryAddStatsEntry(map, item);
            return map;
        }

        private static void TryAddStatsEntry(Dictionary<string, Stats> map, MonsterJson? item)
        {
            if (!TryCreateStatsEntry(item, out var key, out var stats)) return;
            map[key] = stats;
        }

        private static bool TryCreateStatsEntry(MonsterJson? item, out string key, out Stats stats)
        {
            key = string.Empty;
            stats = default!;
            if (!HasValidName(item)) return false;
            if (item!.stats is null) return false;

            key = NormalizeName(item.name!);
            stats = CreateStats(item.stats);
            return true;
        }

        private static bool HasValidName(MonsterJson? item) =>
            !string.IsNullOrWhiteSpace(item?.name);

        private static Stats CreateStats(MonsterStatsJson stats) =>
            new(hp: stats.HP, mp: stats.MP, str: stats.Str, skl: stats.Skl, mag: stats.Mag, spd: stats.Spd, lck: stats.Lck);
        
        private static string NormalizeName(string name)
        {
            var trimmed = SafeTrim(name);
            return StripTrailingDot(trimmed);
        }

        private static string SafeTrim(string? line) => (line ?? string.Empty).Trim();

        private static string StripTrailingDot(string line) =>
            line.EndsWith(".", StringComparison.Ordinal) ? line[..^1].Trim() : line;
        
        private sealed class MonsterJson
        {
            public string? name { get; set; }
            public MonsterStatsJson? stats { get; set; }
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
