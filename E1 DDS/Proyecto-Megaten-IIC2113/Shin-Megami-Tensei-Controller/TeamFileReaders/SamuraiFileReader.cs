using System;
using System.IO;
using System.Collections.Generic;
using System.Text.Json;
using Shin_Megami_Tensei_Models;

namespace Shin_Megami_Tensei;

internal static class SamuraiFileReader

//hecho solo pero con harta ayuda IA (marcadas las funciones en particular)
{
    private static Dictionary<string, Stats>? statsByName;
    
    public static void EnsureLoadedFor(string referenceFilePath)
    {
        if (IsLoaded()) return;
        var jsonPath = ResolveSamuraiJsonPath(referenceFilePath);
        statsByName = LoadStatsMapOrEmpty(jsonPath);
    }

    public static bool TryGetStats(string name, out Stats stats)
    {
        //ayuda IA
        if (HasMap() && statsByName!.TryGetValue(NormalizeName(name), out stats))
            return true;
        stats = default!;
        return false;
    }
    
    private static bool IsLoaded() => statsByName != null;
    private static bool HasMap() => statsByName != null;

    private static string? ResolveSamuraiJsonPath(string referenceFilePath) =>
        TryFindSamuraiJsonNear(referenceFilePath) ?? TryFindSamuraiJsonNear(AppContext.BaseDirectory);

    private static string? TryFindSamuraiJsonNear(string? anyPathInTree)
    {
        //ayuda IA
        if (string.IsNullOrWhiteSpace(anyPathInTree)) return null;
        var dir = ResolveStartDirectory(anyPathInTree);
        foreach (var current in WalkUpDirectories(dir, 5))
        {
            var rootCandidate = CombineFile(current, "samurai.json");
            if (File.Exists(rootCandidate)) return rootCandidate;

            var dataCandidate = CombineFile(Path.Combine(current, "data"), "samurai.json");
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
        var items = DeserializeSamuraiList(json);
        return BuildStatsMap(items);
    }

    private static string ReadAllText(string path) => File.ReadAllText(path);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static List<SamuraiJson> DeserializeSamuraiList(string json) =>
        JsonSerializer.Deserialize<List<SamuraiJson>>(json, JsonOptions) ?? new List<SamuraiJson>();

    private static Dictionary<string, Stats> BuildStatsMap(List<SamuraiJson> items)
    {
        var map = NewStatsMap();
        foreach (var item in items)
            TryAddStatsEntry(map, item);
        return map;
    }

    private static void TryAddStatsEntry(Dictionary<string, Stats> map, SamuraiJson? item)
    {
        if (!TryCreateStatsEntry(item, out var key, out var stats)) return;
        map[key] = stats;
    }

    private static bool TryCreateStatsEntry(SamuraiJson? item, out string key, out Stats stats)
    {
        key = string.Empty;
        stats = default!;
        if (!HasValidName(item)) return false;
        if (item!.stats is null) return false;

        key = NormalizeName(item.name!);
        stats = CreateStats(item.stats);
        return true;
    }

    private static bool HasValidName(SamuraiJson? item) =>
        !string.IsNullOrWhiteSpace(item?.name);

    private static Stats CreateStats(SamuraiStatsJson samuraiStats) =>
        new(hp: samuraiStats.HP, mp: samuraiStats.MP, str: samuraiStats.Str, skl: samuraiStats.Skl, mag: samuraiStats.Mag, spd: samuraiStats.Spd, lck: samuraiStats.Lck);
    
    private static string NormalizeName(string name)
    {
        var trimmed = SafeTrim(name);
        return StripTrailingDot(trimmed);
    }

    private static string SafeTrim(string? line) => (line ?? string.Empty).Trim();

    private static string StripTrailingDot(string line) =>
        line.EndsWith(".", StringComparison.Ordinal) ? line[..^1].Trim() : line;
    
    private sealed class SamuraiJson
    {
        public string? name { get; set; }
        public SamuraiStatsJson? stats { get; set; }
    }

    private sealed class SamuraiStatsJson
    {
        public int HP { get; set; }
        public int MP { get; set; }
        public int Str { get; set; }
        public int Skl { get; set; }
        public int Mag { get; set; }
        public int Spd { get; set; }
        public int Lck { get; set; }
    }
}
