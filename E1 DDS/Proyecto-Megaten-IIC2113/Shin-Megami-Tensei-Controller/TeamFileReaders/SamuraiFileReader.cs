using System;
using System.IO;
using System.Collections.Generic;
using System.Text.Json;
using Shin_Megami_Tensei_Models;

namespace Shin_Megami_Tensei;

internal static class SamuraiFileReader
{
    private static Dictionary<string, Stats>?            statsByName;
    private static Dictionary<string, AffinityProfile>?  affinityByName;

    // =============== API pública ===============

    public static void LoadSamuraiDataFromFile(string referenceFilePath)
    {
        if (IsLoaded()) return;

        var jsonPath = ResolveSamuraiJsonPath(referenceFilePath);
        statsByName     = LoadStatsMapOrEmpty(jsonPath);
        affinityByName  = LoadAffinityMapOrEmpty(jsonPath);
    }

    public static bool GetSamuraiStats(string name, out Stats stats)
    {
        if (statsByName != null && statsByName.TryGetValue(NormalizeName(name), out stats))
            return true;
        stats = default!;
        return false;
    }

    public static bool GetSamuraiAffinities(string name, out AffinityProfile affinities)
    {
        if (affinityByName != null && affinityByName.TryGetValue(NormalizeName(name), out affinities))
            return true;
        affinities = AffinityProfile.NeutralAll;
        return false;
    }

    // =============== Infraestructura ===============

    private static bool IsLoaded() => statsByName != null && affinityByName != null;

    private static string? ResolveSamuraiJsonPath(string referenceFilePath) =>
        LocateSamuraiJson(referenceFilePath) ?? LocateSamuraiJson(AppContext.BaseDirectory);

    private static string? LocateSamuraiJson(string? anyPathInTree)
    {
        if (string.IsNullOrWhiteSpace(anyPathInTree)) return null;

        var dir = ResolveStartDirectory(anyPathInTree);
        foreach (var current in WalkUpDirectories(dir, 5))
        {
            var rootCandidate = Path.Combine(current, "samurai.json");
            if (File.Exists(rootCandidate)) return rootCandidate;

            var dataCandidate = Path.Combine(Path.Combine(current, "data"), "samurai.json");
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
        return StripTrailingDot(trimmed);
    }

    private static string StripTrailingDot(string line) =>
        line.EndsWith(".", StringComparison.Ordinal) ? line[..^1].Trim() : line;

    // =============== Carga desde JSON ===============

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static List<SamuraiJson> DeserializeSamuraiList(string json) =>
        JsonSerializer.Deserialize<List<SamuraiJson>>(json, JsonOptions) ?? new List<SamuraiJson>();

    private static Dictionary<string, Stats> LoadStatsMapOrEmpty(string? jsonPath)
    {
        if (jsonPath is null) return CreateNewStatsMap();

        var json  = File.ReadAllText(jsonPath);
        var items = DeserializeSamuraiList(json);
        return BuildStatsMap(items);
    }

    private static Dictionary<string, AffinityProfile> LoadAffinityMapOrEmpty(string? jsonPath)
    {
        if (jsonPath is null) return CreateNewAffinityMap();

        var json  = File.ReadAllText(jsonPath);
        var items = DeserializeSamuraiList(json);
        return BuildAffinitiesMap(items);
    }

    private static Dictionary<string, Stats> CreateNewStatsMap() =>
        new(StringComparer.OrdinalIgnoreCase);

    private static Dictionary<string, AffinityProfile> CreateNewAffinityMap() =>
        new(StringComparer.OrdinalIgnoreCase);

    private static Dictionary<string, Stats> BuildStatsMap(List<SamuraiJson> items)
    {
        var map = CreateNewStatsMap();
        foreach (var item in items)
            AddStatsEntry(map, item);
        return map;
    }

    private static void AddStatsEntry(Dictionary<string, Stats> map, SamuraiJson? item)
    {
        if (!CreateStatsEntry(item, out var key, out var stats)) return;
        map[key] = stats;
    }

    private static bool CreateStatsEntry(SamuraiJson? item, out string key, out Stats stats)
    {
        key = string.Empty;
        stats = default!;
        if (!HasValidName(item)) return false;
        if (item!.stats is null) return false;

        key   = NormalizeName(item.name!);
        stats = CreateStats(item.stats);
        return true;
    }

    private static bool HasValidName(SamuraiJson? item) =>
        !string.IsNullOrWhiteSpace(item?.name);

    private static Stats CreateStats(SamuraiStatsJson samuraiStats) =>
        new(hp: samuraiStats.HP, mp: samuraiStats.MP, str: samuraiStats.Str, skl: samuraiStats.Skl, mag: samuraiStats.Mag, spd: samuraiStats.Spd, lck: samuraiStats.Lck);

    // =============== Afinidades ===============

    private static Dictionary<string, AffinityProfile> BuildAffinitiesMap(List<SamuraiJson> items)
    {
        var map = CreateNewAffinityMap();
        foreach (var item in items)
        {
            if (!HasValidName(item)) continue;

            var key = NormalizeName(item!.name!);
            var ap  = CreateAffinityProfile(item.affinity);
            map[key] = ap;
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

    private static Affinity MapCodeToAffinity(string? code)
    {
        switch ((code ?? "-").Trim())
        {
            case "Wk": return Affinity.Weak;
            case "Rs": return Affinity.Resist;
            case "Nu": return Affinity.Null;
            case "Rp": return Affinity.Repel;
            case "Dr": return Affinity.Drain;
            case "-":
            default:   return Affinity.Neutral;
        }
    }
    

    private sealed class SamuraiJson
    {
        public string? name { get; set; }
        public Dictionary<string, string>? affinity { get; set; }
        public SamuraiStatsJson? stats { get; set; }
    }

    private sealed class SamuraiStatsJson
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
