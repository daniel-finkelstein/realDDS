namespace Shin_Megami_Tensei_View.ConsoleLib;

public class ConsoleView : AbstractView
{
    protected override void Write(object text)
    {
        base.Write(text);
        Console.Write(text);
    }

    protected override string GetNextInput()
    {
        Console.Write("INPUT: ");
        return Console.ReadLine();
    }
}