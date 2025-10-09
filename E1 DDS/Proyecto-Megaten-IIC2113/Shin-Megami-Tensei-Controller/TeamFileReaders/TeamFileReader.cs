using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Shin_Megami_Tensei_Models;

namespace Shin_Megami_Tensei
{
    public static class TeamFileReader
    {
        private const string Player1Header = "Player 1 Team";
        private const string Player2Header = "Player 2 Team";
        private const string SamuraiToken  = "[Samurai]";

        public static string[] ListTeamFiles(string folder) =>
            Directory.GetFiles(folder, "*.txt", SearchOption.TopDirectoryOnly)
                     .OrderBy(Path.GetFileName)
                     .ToArray();

        public static TeamsFromFile ReadTeams(string path)
        {
            LoadAllDataFromFileIfNotLoadedForEachTypeOfUnit(path);

            var lines = LoadTrimmedNonEmptyLines(path);
            var (player1Line, player2Line) = FindSections(lines);
            var (team1, team2) = ReadTeamsFromSections(lines, player1Line, player2Line);
            return new TeamsFromFile(team1, team2, path);
        }

        private static void LoadAllDataFromFileIfNotLoadedForEachTypeOfUnit(string path)
        {
            SkillsFileReader.LoadSkillDataFromFile(path);
            SamuraiFileReader.LoadSamuraiDataFromFile(path);
            MonsterFileReader.LoadMonsterDataFromFile(path);
        }

        private static (Team team1, Team team2) ReadTeamsFromSections(string[] lines, int player1Line, int player2Line)
            => (ReadTeam(lines, player1Line + 1, player2Line),
                ReadTeam(lines, player2Line + 1, lines.Length));

        private static string[] LoadTrimmedNonEmptyLines(string path)
        {
            var raw = File.ReadAllLines(path);
            return TextParsing.TrimAndFilter(raw).ToArray();
        }

        private static (int player1Header, int player2Header) FindSections(string[] lines)
        {
            int p1 = FindHeader(lines, Player1Header);
            int p2 = FindHeader(lines, Player2Header);
            if (p1 < 0 || p2 < 0 || p2 <= p1)
                throw new InvalidOperationException(
                    $"Formato inválido en archivo de equipos: se esperaban encabezados '{Player1Header}' y '{Player2Header}' en orden."
                );
            return (p1, p2);
        }

        private static int FindHeader(string[] lines, string title) =>
            Array.FindIndex(lines, s => s.Equals(title, StringComparison.OrdinalIgnoreCase));

        private static Team ReadTeam(string[] lines, int start, int endExclusive)
        {
            var team = new Team();
            for (int i = start; i < endExclusive; i++)
                team.AddUnit(ParseUnitLine(lines[i]));
            return team;
        }

        private static Unit ParseUnitLine(string line) =>
            IsSamurai(line) ? CreateSamuraiFromLine(line) : CreateMonsterFromLine(line);

        private static bool IsSamurai(string line) =>
            line.StartsWith(SamuraiToken, StringComparison.OrdinalIgnoreCase);

        private static Unit CreateSamuraiFromLine(string line)
        {
            var name = NormalizeName(ExtractNameAfter(line, SamuraiToken));
            var skills = TakeSkillsFromLine(line);

            if (!SamuraiFileReader.GetSamuraiStats(name, out var stats))
                throw new InvalidOperationException($"Samurái no encontrado: '{name}'.");

            SamuraiFileReader.GetSamuraiAffinities(name, out var affinities);
            return new Samurai(name, stats, skills, affinities);
        }

        private static Unit CreateMonsterFromLine(string line)
        {
            var name = NormalizeName(ExtractNameBeforeParen(line));

            if (!MonsterFileReader.GetStats(name, out var stats))
                throw new InvalidOperationException($"Monstruo no encontrado: '{name}'.");

            MonsterFileReader.GetAffinities(name, out var affinities);

            var skills = new List<Skill>();
            if (MonsterFileReader.GetSkillNames(name, out var skillNames))
                foreach (var sn in skillNames)
                    if (SkillsFileReader.GetSkill(sn, out var sk)) skills.Add(sk);

            return new Monster(name, stats, skills, affinities);
        }

        private static string ExtractNameAfter(string wholeLine, string token)
        {
            var after = wholeLine.Substring(token.Length).Trim();
            int i = after.IndexOf('(');
            return i >= 0 ? after[..i].Trim() : after;
        }

        private static string ExtractNameBeforeParen(string line)
        {
            int i = line.IndexOf('(');
            return i >= 0 ? line[..i].Trim() : line.Trim();
        }

        private static string NormalizeName(string name)
        {
            var trimmed = (name ?? string.Empty).Trim();
            return trimmed.EndsWith(".", StringComparison.Ordinal) ? trimmed[..^1].Trim() : trimmed;
        }

        private static List<Skill> TakeSkillsFromLine(string line)
        {
            var inside = ExtractParenthesisContent(line);
            if (string.IsNullOrEmpty(inside)) return new List<Skill>();

            var names = TextParsing.SplitTrimmed(inside, ',');
            var list  = new List<Skill>();
            foreach (var raw in names)
            {
                if (SkillsFileReader.GetSkill(raw, out var s)) list.Add(s);
                else list.Add(new Skill(raw, "Unknown", 0, 0, "Single", 1, ""));
            }
            return list;
        }

        private static string ExtractParenthesisContent(string line)
        {
            var (openIndex, closeIndex) = FindParenthesisIndices(line);
            if (openIndex < 0 || closeIndex <= openIndex + 1) return "";
            return line.Substring(openIndex + 1, closeIndex - openIndex - 1);
        }

        private static (int openIndex, int closeIndex) FindParenthesisIndices(string line)
        {
            int open = line.IndexOf('(');
            int close = line.IndexOf(')', open + 1);
            return (open, close);
        }
    }

    internal static class TextParsing
    {
        public static IEnumerable<string> TrimAndFilter(IEnumerable<string> items) =>
            items.Select(line => line.Trim())
                 .Where(line => !string.IsNullOrWhiteSpace(line));

        public static IEnumerable<string> SplitTrimmed(string text, char separator) =>
            text.Split(separator, StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.Trim());
    }

    public class TeamsFromFile
    {
        public Team Player1 { get; }
        public Team Player2 { get; }
        public string SourcePath { get; }

        public TeamsFromFile(Team player1Team, Team player2Team, string source)
        {
            Player1 = player1Team;
            Player2 = player2Team;
            SourcePath = source;
        }
    }
}
