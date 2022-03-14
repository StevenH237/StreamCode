using System;
using System.Text.RegularExpressions;
using System.Linq;
using System.Collections.Generic;
using System.IO;

public class CPHInline
{
  private const string ScriptFile = @"C:\Users\Nixill\Documents\Streaming\Scripts\rewards.txt";
  private static Regex SplitString = new Regex(@"^\s+([^\s]+)\s+(.*)$", RegexOptions.Compiled);

  private (string, string) Split(string input)
  {
    Match mtc = SplitString.Match(input);

    if (mtc.Success) return (mtc.Groups[1].Value, mtc.Groups[2].Value);
    else return (input.Trim(), null);
  }

  // ARGUMENTS IN
  //   %status% - New stream title
  //   %gameName% - New stream game
  public bool RunScript()
  {
    List<string> lines = File.ReadAllLines(ScriptFile).ToList();
    ScriptStatus status = ScriptStatus.True;

    // Iterate over all the lines of the script. But we're using a list
    // because some keywords can also delete lines early.
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
    // 
  }

  public bool ParseCondition(string line, List<string> lines, ref ScriptStatus status)
  {

  }
}

internal enum ScriptStatus
{
  False = 0,
  True = 1,
  Error = -1
}
