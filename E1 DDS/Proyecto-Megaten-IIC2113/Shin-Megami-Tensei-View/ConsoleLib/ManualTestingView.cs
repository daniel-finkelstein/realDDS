namespace Shin_Megami_Tensei_View.ConsoleLib;

public class ManualTestingView : TestingView
{
    private const string EndOfFileString = "[EndOfFile]";
    private readonly string[] _expectedScript;
    private int _currentLine;
    private bool _isOutputCorrectSoFar = true;
    
    public ManualTestingView(string pathTestScript) : base(pathTestScript)
    {
        Console.ForegroundColor = ConsoleColor.Blue;
        _expectedScript = File.ReadAllLines(pathTestScript);
        _currentLine = 0;
    }

    protected override void Write(object text)
    {
        if(_isOutputCorrectSoFar)
            CheckIfCurrentOutputIsAsExpected(text);
        base.Write(text);
        Console.Write(text);
    }

    private void CheckIfCurrentOutputIsAsExpected(object text)
    {
        string normalizedText = GetNormalizedTest(text.ToString());
        string[] lines = normalizedText.Split("\n");
        CheckThatLinesMatchTheExpectedOutput(lines);
    }

    private string GetNormalizedTest(string text)
        => text[^1] == '\n' ? text.Remove(text.Length-1) : text;
    
    private void CheckThatLinesMatchTheExpectedOutput(string[] lines)
    {
        for(int i = 0; i < lines.Length; i++)
        {
            if(IsThisLineDifferentFromTheExpectedValue(lines[i]))
            {
                IndicateThatThereIsAnErrorInThisLineAndChangeTheColorOfTheConsole();
                break;
            }
            _currentLine++;
        }
    }
    
    private bool IsThisLineDifferentFromTheExpectedValue(string line)
        => GetExpectedLine() != line;

    private string GetExpectedLine()
    {
        if(IsTheEndOfTheExpectedScript())
            return EndOfFileString;
        return _expectedScript[_currentLine];
    }
    
    private bool IsTheEndOfTheExpectedScript()
        => _currentLine == _expectedScript.Length;

    private void IndicateThatThereIsAnErrorInThisLineAndChangeTheColorOfTheConsole()
    {
        _isOutputCorrectSoFar = false;
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Write($"[ERROR] el valor esperado acá era: \"{GetExpectedLine()}\"");
    }

    protected override string GetNextInput()
    {
        try
        {
            return TryToGetInputFromTest();
        }
        catch (InvalidInputRequestException)
        {
            return GetNextInputFromUser();
        }
    }
    
    private string TryToGetInputFromTest()
    {
        string nextInput = base.GetNextInput();
        CheckIfCurrentOutputIsAsExpected($"INPUT: {nextInput}");
        if (!_isOutputCorrectSoFar)
            throw new InvalidInputRequestException("No se debía pedir un input en este momento");
        Console.Write($"[INPUT TEST]: {nextInput}");
        Console.ReadLine();
        return nextInput;
    }
    
    private string GetNextInputFromUser()
    {
        Console.Write($"[INPUT MANUAL]: ");
        return Console.ReadLine();
    }
}