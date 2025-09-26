using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Shin_Megami_Tensei_Models;

namespace Shin_Megami_Tensei;

public static class TeamFileReader
{
    public static string[] ListTeamFiles(string folder)
    {
        return Directory.GetFiles(folder, "*.txt", SearchOption.TopDirectoryOnly)
            .OrderBy(Path.GetFileName).ToArray();
    }

    public static TeamsFromFile ReadTeams(string path)
    {
        EnsureReadersLoaded(path);
        var lines = LoadLinesFor(path);
        var (player1Line, player2Line) = FindSections(lines);
        var (team1, team2) = ReadTeamsFromSections(lines, player1Line, player2Line);
        return CreateResult(team1, team2, path);
    }

    private static void EnsureReadersLoaded(string path)
    {
        SamuraiFileReader.EnsureLoadedFor(path);
        MonsterFileReader.EnsureLoadedFor(path);
        SkillsFileReader.EnsureLoadedFor(path);
    }

    private static string[] LoadLinesFor(string path) => LoadLines(path);

    private static (Team team1, Team team2) ReadTeamsFromSections(string[] lines, int player1Line, int player2Line)
        => (ReadTeam(lines, player1Line + 1, player2Line), ReadTeam(lines, player2Line + 1, lines.Length));

    private static TeamsFromFile CreateResult(Team player1Team, Team player2Team, string path)
        => new TeamsFromFile(player1Team, player2Team, path);



    private static string[] LoadLines(string path)
    {
        var raw = File.ReadAllLines(path);
        return TextParsing.TrimAndFilter(raw).ToArray();
    }

    private static (int player1Header, int player2Header) FindSections(string[] lines)
    {
        int player1Header = FindHeader(lines, "Player 1 Team");
        int player2Header = FindHeader(lines, "Player 2 Team");
        if (player1Header < 0 || player2Header < 0 || player2Header <= player1Header)
            throw new InvalidOperationException("Formato inválido.");
        return (player1Header, player2Header);
    }

    private static int FindHeader(string[] lines, string title)
    {
        return Array.FindIndex(lines, s => s.Equals(title, StringComparison.OrdinalIgnoreCase));
    }

    private static Team ReadTeam(string[] lines, int firstTeamLine, int lastTeamLine)
    {
        var team = new Team();
        foreach (var unit in Slice(lines, firstTeamLine, lastTeamLine).Select(SingleLine))
            team.AddUnit(unit);
        return team;
    }

    private static string[] Slice(string[] src, int firstLine, int lastLine)
    {
        int subArraySize = lastLine - firstLine;
        var subArray = new string[subArraySize];
        if (subArraySize > 0)
            Array.Copy(src, firstLine, subArray, 0, subArraySize);
        return subArray;
    }

    private static Unit SingleLine(string line)
    {
        if (IsSamurai(line))
            return MakeSamurai(line);
        else
            return MakeMonster(line);
    }

    private static bool IsSamurai(string line)
    {
        return line.StartsWith("[Samurai]", StringComparison.OrdinalIgnoreCase);
    }

    private static Unit MakeSamurai(string line)
    {
        var name = NormalizeName(ExtractNameAfter(line, "[Samurai]"));
        var skills = TakeSkillsFromLine(line);
        
        if (SamuraiFileReader.TryGetStats(name, out var realStats))
            return new Samurai(name, realStats, skills);

        return null;
    }

    private static Unit MakeMonster(string line)
    {
        var name   = NormalizeName(ExtractNameBeforeParen(line));
        var skills = TakeSkillsFromLine(line);

        if (MonsterFileReader.TryGetStats(name, out var realStats))
            return new Monster(name, realStats, skills);
        return null;
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
        if (trimmed.EndsWith(".", StringComparison.Ordinal)) trimmed = trimmed[..^1].Trim();
        return trimmed;
    }

    private static List<Skill> TakeSkillsFromLine(string line)
    {
        var inside = ExtractParenthesisContent(line);
        if (string.IsNullOrEmpty(inside)) return new List<Skill>();

        var names = TextParsing.SplitTrimmed(inside, ',');
        var list  = new List<Skill>();
        foreach (var raw in names)
        {
            if (SkillsFileReader.TryGetSkill(raw, out var s)) list.Add(s);
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
    public static IEnumerable<string> TrimAndFilter(IEnumerable<string> items)
    {
        return items.Select(line => line.Trim()).Where(line => !string.IsNullOrWhiteSpace(line));
    }

    public static IEnumerable<string> SplitTrimmed(string text, char separator)
    {
        return text.Split(separator, StringSplitOptions.RemoveEmptyEntries)
                   .Select(t => t.Trim());
    }
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
