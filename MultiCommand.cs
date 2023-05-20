// MultiCommand.cs
using System;
using System.Text.RegularExpressions;

public class CPHInline
{
  // public void Init()
  // {
  //
  // }

  // public void Dispose()
  // {
  //
  // }

  public static readonly Regex username = new Regex(@"@([A-Za-z0-9][A-Za-z0-9_]{0,24})");

  public bool Execute()
  {
    string title = (string)args["status"];

    string multiOutput = "";
    int count = 0;

    foreach (Match match in username.Matches(title))
    {
      multiOutput += $"/{match.Groups[1].Value}";
      count++;
    }

    if (count == 0)
    {
      CPH.SendMessage("No multistream tonight!");
    }
    else if (count == 1)
    {
      CPH.SendMessage($"https://multi.nixill.net{multiOutput}/layout4");
    }
    else
    {
      CPH.SendMessage($"https://multi.nixill.net{multiOutput}");
    }

    return true;
  }
}
