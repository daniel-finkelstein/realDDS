using System;
using System.IO;
using System.Linq;
using Shin_Megami_Tensei_View;
using Shin_Megami_Tensei_Models;


namespace Shin_Megami_Tensei;

public class Game
{
    private readonly View _view;
    private readonly string _teamsFolder;

    public Game(View view, string teamsFolder)
    {
        _view = view ?? throw new ArgumentNullException(nameof(view));
        _teamsFolder = teamsFolder ?? throw new ArgumentNullException(nameof(teamsFolder));
    }

    public void Play()
    {
        var files  = LoadTeamFiles();
        ShowMenu(files);
        var index  = ReadSelection(files);
        var parsed = ReadTeams(files, index);
        if (!BothTeamsValid(parsed)) { PrintInvalidFile(); return; }
        StartCombat(parsed);
    }
    

    private string[] LoadTeamFiles() =>
        TeamFileReader.ListTeamFiles(_teamsFolder)
            .OrderBy(Path.GetFileName, StringComparer.Ordinal)
            .ToArray();

    private int ReadSelection(string[] files) =>
        ReadIndexFromView(files.Length);

    private TeamsFromFile ReadTeams(string[] files, int index) =>
        TeamFileReader.ReadTeams(files[index]);

    private bool BothTeamsValid(TeamsFromFile parsed) =>
        IsValidTeam(parsed.Player1) && IsValidTeam(parsed.Player2);

    private bool IsValidTeam(Team team) =>
        TeamValidator.Validate(team).IsValid;

    private void PrintInvalidFile() =>
        _view.WriteLine("Archivo de equipos inválido");

    private void StartCombat(TeamsFromFile parsed) =>
        CombatLogic.Run(_view, parsed.Player1, parsed.Player2);


    private void ShowMenu(string[] files)
    {
        _view.WriteLine("Elige un archivo para cargar los equipos");
        for (int i = 0; i < files.Length; i++)
            _view.WriteLine($"{i}: {Path.GetFileName(files[i])}");
    }

    private int ReadIndexFromView(int itemCount)
    {
        string? inputLine = _view.ReadLine();
        if (string.IsNullOrWhiteSpace(inputLine)) return 0;

        if (!int.TryParse(inputLine, out int selectedIndex)) return 0;
        if (selectedIndex < 0) return 0;
        if (selectedIndex >= itemCount) return itemCount - 1;

        return selectedIndex;
    }
}