using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using Shin_Megami_Tensei_Models;

namespace Shin_Megami_Tensei
{
    public static class SkillsFileReader
    {
        private static readonly StringComparer CaseCompared = StringComparer.OrdinalIgnoreCase;
        private static Dictionary<string, Skill> _skillsByName = new(StringComparer.OrdinalIgnoreCase);
        private static bool _loaded;
        
        public static void EnsureLoadedFor(string anyPathInsideDataFolder)
        {
            if (IsLoaded()) return;

            var file = ResolveSkillsJsonPath(anyPathInsideDataFolder)
                       ?? throw new FileNotFoundException("No se encontró skills.json cercano a " + anyPathInsideDataFolder);

            LoadFromFile(file);
            MarkLoaded();
        }

        public static bool TryGetSkill(string skillName, out Skill skill) =>
            _skillsByName.TryGetValue(NormalizeName(skillName), out skill);
        
        private static bool IsLoaded() => _loaded;
        private static void MarkLoaded() => _loaded = true;
        
        private static string? ResolveSkillsJsonPath(string anyPathInsideDataFolder)
        {
            var dir = ResolveStartDirectory(anyPathInsideDataFolder);
            var candidates = BuildCandidatePaths(dir);
            return FirstExisting(candidates);
        }

        private static string ResolveStartDirectory(string path) =>
            Path.GetDirectoryName(path) ?? ".";

        private static IEnumerable<string> BuildCandidatePaths(string dir)
        {
            yield return Path.Combine(dir, "skills.json");
            yield return Path.Combine(dir, "..", "skills.json");
            yield return Path.Combine(dir, "..", "..", "skills.json");
        }

        private static string? FirstExisting(IEnumerable<string> paths) =>
            paths.FirstOrDefault(File.Exists);
        
        private static void LoadFromFile(string pathToSkillsJson)
        {
            var json = ReadAllText(pathToSkillsJson);
            var root = ParseRootArray(json);
            _skillsByName = BuildSkillsMap(root);
        }

        private static string ReadAllText(string path) =>
            File.ReadAllText(path);

        private static JsonArray ParseRootArray(string json) =>
            (JsonNode.Parse(json) as JsonArray) ?? new JsonArray();

        private static Dictionary<string, Skill> BuildSkillsMap(JsonArray root)
        {
            var skillsDictionary = NewSkillsMap();
            foreach (var item in root)
                TryAddSkill(skillsDictionary, item as JsonObject);
            return skillsDictionary;
        }

        private static Dictionary<string, Skill> NewSkillsMap() =>
            new(CaseCompared);

        private static void TryAddSkill(Dictionary<string, Skill> skillsDictionary, JsonObject? objeto)
        {
            if (!TryCreateSkill(objeto, out var skill)) return;
            skillsDictionary[skill.Name] = skill;
        }

        private static bool TryCreateSkill(JsonObject? objeto, out Skill skill)
        {
            skill = default!;
            if (objeto is null) return false;

            var name   = GetString(objeto, "name");
            if (string.IsNullOrWhiteSpace(name)) return false;

            var type   = GetString(objeto, "type");
            var cost   = GetInt(objeto, "cost");
            var power  = GetInt(objeto, "power");
            var target = GetString(objeto, "target");
            var hits   = GetInt(objeto, "hits");
            var effect = GetString(objeto, "effect");

            skill = new Skill(name, type, cost, power, target, hits, effect);
            return true;
        }
        
        private static string GetString(JsonObject objeto, string key)
        {
            var n = GetNode(objeto, key);
            return n is null ? string.Empty : ToStringOrEmpty(n);
        }

        private static int GetInt(JsonObject objeto, string key)
        {
            var node = GetNode(objeto, key);
            if (node is null) return 0;

            if (TryGetValue<int>(node, out var intValue)) return intValue;
            if (TryGetValue<string>(node, out var stringValue) && int.TryParse(stringValue, out var parsedInt)) return parsedInt;
            return 0;
        }

        private static JsonNode? GetNode(JsonObject objeto, string key) =>
            objeto.TryGetPropertyValue(key, out var n) ? n : null;

        private static bool TryGetValue<T>(JsonNode node, out T value)
        //ayuda IA
        {
            if (node is JsonValue jasonValue && jasonValue.TryGetValue<T>(out value))
                return true;

            value = default!;
            return false;
        }


        private static string ToStringOrEmpty(JsonNode node)
        {
            try { return node.GetValue<string>() ?? string.Empty; }
            catch { return (node.ToJsonString() ?? string.Empty).Trim('\"'); }
        }
        
        private static string NormalizeName(string name) =>
            (name ?? string.Empty).Trim();
    }
}
