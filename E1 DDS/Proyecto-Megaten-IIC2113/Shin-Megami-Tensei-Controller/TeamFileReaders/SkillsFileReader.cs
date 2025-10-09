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
        private static readonly StringComparer CaseInsensitive = StringComparer.OrdinalIgnoreCase;
        private static Dictionary<string, Skill> _skillsByName = new(StringComparer.OrdinalIgnoreCase);
        private static bool _loaded;
        

        public static void LoadSkillDataFromFile(string anyPathInsideDataFolder)
        {
            if (_loaded) return;

            var file = ResolveSkillsJsonPath(anyPathInsideDataFolder)
                       ?? throw new FileNotFoundException("No se encontró skills.json cercano a " + anyPathInsideDataFolder);

            LoadFromFile(file);
            _loaded = true;
        }

        public static bool GetSkill(string skillName, out Skill skill) =>
            _skillsByName.TryGetValue(NormalizeName(skillName), out skill);
        

        private static string? ResolveSkillsJsonPath(string anyPathInsideDataFolder)
        {
            var dir = ResolveStartDirectory(anyPathInsideDataFolder);
            return BuildCandidatePaths(dir).FirstOrDefault(File.Exists);
        }

        private static string ResolveStartDirectory(string path) =>
            Path.GetDirectoryName(path) ?? ".";

        private static IEnumerable<string> BuildCandidatePaths(string dir)
        {
            yield return Path.Combine(dir, "skills.json");
            yield return Path.Combine(dir, "..", "skills.json");
            yield return Path.Combine(dir, "..", "..", "skills.json");
        }

        private static void LoadFromFile(string pathToSkillsJson)
        {
            var json = File.ReadAllText(pathToSkillsJson);
            var root = ParseRootArray(json);
            _skillsByName = BuildSkillsMap(root);
        }

        private static JsonArray ParseRootArray(string json) =>
            (JsonNode.Parse(json) as JsonArray) ?? new JsonArray();

        private static Dictionary<string, Skill> BuildSkillsMap(JsonArray root)
        {
            var dict = new Dictionary<string, Skill>(CaseInsensitive);
            foreach (var node in root)
                AddSkill(dict, node as JsonObject);
            return dict;
        }

        private static void AddSkill(Dictionary<string, Skill> dict, JsonObject? obj)
        {
            if (!CreateSkill(obj, out var skill)) return;
            dict[skill.Name] = skill;
        }

        private static bool CreateSkill(JsonObject? obj, out Skill skill)
        {
            skill = default!;
            if (obj is null) return false;

            var name = GetString(obj, "name");
            if (string.IsNullOrWhiteSpace(name)) return false;

            var type   = GetString(obj, "type");
            var cost   = GetInt(obj, "cost");
            var power  = GetInt(obj, "power");
            var target = GetString(obj, "target");
            var hits   = GetInt(obj, "hits");
            var effect = GetString(obj, "effect");

            skill = new Skill(name, type, cost, power, target, hits, effect);
            return true;
        }

        private static string GetString(JsonObject obj, string key)
        {
            var n = GetNode(obj, key);
            return n is null ? string.Empty : ReadToStringOrEmpty(n);
        }

        private static int GetInt(JsonObject obj, string key)
        {
            var node = GetNode(obj, key);
            if (node is null) return 0;

            if (TryGetValue<int>(node, out var intValue)) return intValue;
            if (TryGetValue<string>(node, out var stringValue) && int.TryParse(stringValue, out var parsed)) return parsed;
            return 0;
        }

        private static JsonNode? GetNode(JsonObject obj, string key) =>
            obj.TryGetPropertyValue(key, out var n) ? n : null;

        private static bool TryGetValue<T>(JsonNode node, out T value)
        {
            if (node is JsonValue jv && jv.TryGetValue<T>(out value))
                return true;

            value = default!;
            return false;
        }

        private static string ReadToStringOrEmpty(JsonNode node)
        {
            try { return node.GetValue<string>() ?? string.Empty; }
            catch { return (node.ToJsonString() ?? string.Empty).Trim('\"'); }
        }

        private static string NormalizeName(string name) =>
            (name ?? string.Empty).Trim();
    }
}

