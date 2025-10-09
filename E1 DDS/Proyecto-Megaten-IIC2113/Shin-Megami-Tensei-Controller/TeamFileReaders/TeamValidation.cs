using System;
using System.Linq;
using System.Collections.Generic;
using Shin_Megami_Tensei_Models;

namespace Shin_Megami_Tensei
{
    public static class TeamValidator
    {
        private const int MaxTeamSize = 8;
        private const int MaxSamuraiSkills = 8;
        
        private const string ErrExactlyOneSamurai = "Debe haber exactamente 1 Samurai en el equipo.";
        private static readonly string ErrTeamTooLarge =
            $"El equipo excede el tamaño máximo permitido ({MaxTeamSize}).";
        private const string ErrEmptyNames = "Unidades con nombre vacío.";

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
            if (!HasExactlyOneSamurai(team))
                errors.Add(ErrExactlyOneSamurai);
        }

        private static bool HasExactlyOneSamurai(Team team) => CountSamurai(team) == 1;

        private static int CountSamurai(Team team)
        {
            int count = 0;
            foreach (var unit in team.TeamUnits)
                if (IsUnitSamurai(unit)) count++;
            return count;
        }

        private static bool IsUnitSamurai(Unit unit) =>
            unit.Type.Equals("Samurai", StringComparison.OrdinalIgnoreCase);
        
        private static void CheckMaxPartySize(Team team, List<string> errors)
        {
            if (team.TeamUnits.Count > MaxTeamSize)
                errors.Add(ErrTeamTooLarge);
        }


        private static void CheckNoEmptyNames(Team team, List<string> errors)
        {
            bool anyEmpty = team.TeamUnits.Any(u => string.IsNullOrWhiteSpace(u.Name));
            if (anyEmpty) errors.Add(ErrEmptyNames);
        }

        private static void CheckNoDuplicateNames(Team team, List<string> errors)
        {
            var duplicate = team.TeamUnits
                .GroupBy(u => u.Name, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault(g => g.Count() > 1);

            if (duplicate != null)
                errors.Add($"Nombres duplicados: {duplicate.Key}");
        }
        

        private static void CheckSamuraiMaxSkills(Team team, List<string> errors)
        {
            foreach (var samurai in EnumerateSamurais(team))
            {
                if (CountSamuraiSkills(samurai) > MaxSamuraiSkills)
                    errors.Add($"Demasiadas habilidades en el Samurai {samurai.Name} (máximo {MaxSamuraiSkills}).");
            }
        }

        private static IEnumerable<Samurai> EnumerateSamurais(Team team)
        {
            foreach (var unit in team.TeamUnits)
                if (unit is Samurai s) yield return s;
        }

        private static int CountSamuraiSkills(Samurai samurai) =>
            samurai.Skills?.Count ?? 0;

        private static void CheckSamuraiNoDuplicateSkills(Team team, List<string> errors)
        {
            foreach (var samurai in EnumerateSamurais(team))
            {
                var duplicated = FindDuplicateSkills(samurai);
                if (duplicated.Count > 0)
                    errors.Add(BuildDuplicateSkillsMessage(samurai, duplicated));
            }
        }

        private static HashSet<string> FindDuplicateSkills(Samurai samurai)
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var dups = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var sk in samurai.Skills ?? Enumerable.Empty<Skill>())
            {
                var key = (sk?.Name ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(key)) continue;
                if (!seen.Add(key)) dups.Add(key);
            }

            return dups;
        }

        private static string BuildDuplicateSkillsMessage(Samurai samurai, HashSet<string> duplicated)
            => $"El Samurai {samurai.Name} tiene habilidades duplicadas: {string.Join(", ", duplicated)}.";
        

        private static TeamValidationResult BuildResult(List<string> errors)
            => new TeamValidationResult(errors.Count == 0, errors);
    }

    public sealed class TeamValidationResult
    {
        public bool IsValid { get; }
        public IReadOnlyList<string> Errors { get; }

        public TeamValidationResult(bool isValid, List<string> errors)
        {
            IsValid = isValid;
            Errors = errors;
        }

        public override string ToString() => IsValid ? "Válido" : "Archivo de equipos inválido";
    }
}

