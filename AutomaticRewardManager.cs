using System;
using System.Text.RegularExpressions;
using System.Linq;
using System.Collections.Generic;
using System.IO;

public class CPHInline
{
  private const string ScriptFile = @"C:\Users\Nixill\Documents\Streaming\Scripts\rewards.txt";
  private static Regex SplitString = new Regex(@"^\s+([^\s]+)\s+(.*)$", RegexOptions.Compiled);

  private static Dictionary<string, (bool Conditional, Predicate<string, List<string>, ScriptStatus> Callback)> Keywords;

  private void Init()
  {

  }

  private (string, string) Keyword(string input)
  {
    Match mtc = SplitString.Match(input);

    if (mtc.Success) return (mtc.Groups[1].Value.ToLower(), mtc.Groups[2].Value);
    else return (input.Trim().ToLower(), null);
  }

  // ARGUMENTS IN
  //   %status% - New stream title
  //   %gameName% - New stream game
  public bool RunScript()
  {
    List<string> lines = File.ReadAllLines(ScriptFile).ToList();
    ScriptStatus status = ScriptStatus.True;

    // Iterate over all the lines of the script. But we're using a list
    // and lines.Count because some keywords can also delete lines early.
    while (lines.Count > 0)
    {
      string line = lines[0];
      lines.Remove(0);

      // A blank line resets the status.
      if (line == "")
      {
        status = ScriptStatus.True;
        continue;
      }

      // An error status ignores the rest of the block.
      if (status == ScriptStatus.Error)
      {
        continue;
      }

      // Otherwise, parse a line.
      ParseLine(line, lines, ref status);
    }
  }

  public void ParseLine(string line, List<string> lines, ref ScriptStatus status)
  {
    // Get the first keyword of the line
    (string keyword, line) = Keyword(line);

    // See if it's an acceptable keyword
    // If not we'll ignore the line without affecting script status
    if (!Keywords.ContainsKey(keyword))
    {
      return;
    }

    // If it is, then we'll 
  }
}

internal enum ScriptStatus
{
  False = 0,
  True = 1,
  Error = -1
}
