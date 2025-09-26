using System;
using System.Linq;
using System.Collections.Generic;
using Shin_Megami_Tensei_Models;

namespace Shin_Megami_Tensei;

    public static class TeamValidator
    {
        public static TeamValidationResult Validate(Team team)
        {
            var errors = new List<string>();
            CheckExactlyOneSamurai(team, errors);
            CheckMaxPartySize(team, errors);
            CheckNoEmptyNames(team, errors);
            CheckNoDuplicateNames(team, errors);
            CheckSamuraiMaxSkills(team, errors);
            CheckSamuraiNoDuplicateSkills(team, errors);
            return BuildResult(errors);
        }
        
        private static void CheckExactlyOneSamurai(Team team, List<string> errors)
        {
            if (HasExactlyOneSamurai(team)) return;
            AddExactlyOneSamuraiError(errors);
        }

        private static bool HasExactlyOneSamurai(Team team) =>
            CountSamurai(team) == 1;

        private static int CountSamurai(Team team)
        {
            int samuraiCount = 0;
            foreach (var unit in team.TeamUnits)
                if (IsUnitSamurai(unit)) samuraiCount++;
            return samuraiCount;
        }

        private static void AddExactlyOneSamuraiError(List<string> errors) =>
            errors.Add("Hay mas o menos de 1 samurai");


        private static void CheckMaxPartySize(Team team, List<string> errors)
        {
            int maxTeamSize = 8;
            if (team.TeamUnits.Count > maxTeamSize)
                errors.Add($"Muchos en un equipo");
        }

        private static void CheckNoEmptyNames(Team team, List<string> errors)
        {
            bool emptyNameCheck = team.TeamUnits.Any(unit => string.IsNullOrWhiteSpace(unit.Name));
            if (emptyNameCheck) errors.Add("Unidades con nombre vacío.");
        }

        private static void CheckNoDuplicateNames(Team team, List<string> errors)
        {
            //ayuda de IA
            var duplicateName = team.TeamUnits
                .GroupBy(unit => unit.Name, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault(group => group.Count() > 1);
            if (duplicateName != null) errors.Add($"Nombres duplicados {duplicateName.Key}");
        }
        

        private static void CheckSamuraiMaxSkills(Team team, List<string> errors)
        {
            foreach (var samurai in EnumerateSamurais(team))
                ValidateSamuraiMaxSkills(samurai, errors);
        }

        private static IEnumerable<Samurai> EnumerateSamurais(Team team)
        {
            foreach (var unit in team.TeamUnits)
                if (unit is Samurai samurai) yield return samurai;
        }

        private static void ValidateSamuraiMaxSkills(Samurai samurai, List<string> errors)
        {
            if (SamuraiSkillCount(samurai) > MaxSamuraiSkillsAllowed())
                AddTooManySkillsError(errors);
        }

        private static int SamuraiSkillCount(Samurai samurai) =>
            samurai.Skills?.Count ?? 0;

        private static int MaxSamuraiSkillsAllowed() => 8;

        private static void AddTooManySkillsError(List<string> errors) =>
            errors.Add("Demasiadas habilidades en un samurai");


        private static void CheckSamuraiNoDuplicateSkills(Team team, List<string> errors)
        {
            foreach (var unit in team.TeamUnits)
            {
                if (unit is Samurai samurai)
                    CheckSamuraiSkills(samurai, errors);
            }
        }

        private static void CheckSamuraiSkills(Samurai samurai, List<string> errors)
        {
            var duplicated = FindDuplicateSkills(samurai);
            if (duplicated.Count > 0)
                errors.Add(BuildDuplicateSkillsMessage(samurai, duplicated));
        }

        private static HashSet<string> FindDuplicateSkills(Samurai samurai)
        //ayuda de IA
        {
            var (seenSkill, duplicatedSkillSet) = InitSkillSets();
            foreach (var key in SkillKeys(samurai)) TrackSkillKey(key, seenSkill, duplicatedSkillSet);
            return duplicatedSkillSet;
        }

        private static (HashSet<string> seen, HashSet<string> dup) InitSkillSets() =>
            (NewCaseInsensitiveSet(), NewCaseInsensitiveSet());

        private static HashSet<string> NewCaseInsensitiveSet() =>
            new(StringComparer.OrdinalIgnoreCase);

        private static IEnumerable<string> SkillKeys(Samurai samurai)
        {
            foreach (var skill in EnumerateSkills(samurai))
            {
                var key = NormalizeSkillName(skill);
                if (!IsSkillNameInvalid(key))
                    yield return key;
            }
        }

        private static IEnumerable<Skill> EnumerateSkills(Samurai samurai) =>
            samurai.Skills ?? Enumerable.Empty<Skill>();

        private static void TrackSkillKey(string key, HashSet<string> seenSkills, HashSet<string> duplicatedSkillsSet)
        {
            if (IsSkillDuplicated(key, seenSkills)) duplicatedSkillsSet.Add(key);
        }

        private static bool IsSkillNameInvalid(string key)
        {
            return string.IsNullOrWhiteSpace(key);
        }

        private static bool IsSkillDuplicated(string key, HashSet<string> seenSkillsSet)
        {
            return !seenSkillsSet.Add(key);
        }


        private static string NormalizeSkillName(Skill? skill)
            => (skill?.Name ?? string.Empty).Trim();

        private static string BuildDuplicateSkillsMessage(Samurai samurai, HashSet<string> duplicated)
            => $"El Samurai {samurai.Name} tiene habilidades duplicadas: {string.Join(", ", duplicated)}.";


        private static bool IsUnitSamurai(Unit unit) =>
            unit.Type.Equals("Samurai", StringComparison.OrdinalIgnoreCase);

        private static TeamValidationResult BuildResult(List<string> errors)
            => new TeamValidationResult(errors.Count == 0, errors);
    }

    public class TeamValidationResult
    {
        public bool IsValid { get; }
        public IReadOnlyList<string> Errors { get; }
        public TeamValidationResult(bool isValid, List<string> errors)
        { IsValid = isValid; Errors = errors; }
        public override string ToString() => IsValid ? "Válido" : "Archivo de equipos inválido";
    }


