using System;
using System.Text.RegularExpressions;
using System.Linq;
using System.Collections.Generic;
using System.IO;

public class CPHInline
{
  private const string ScriptFile = @"C:\Users\Nixill\Documents\Streaming\Scripts\rewards.txt";
  private static Regex SplitString = new Regex(@"^([^\s]+?) +(.*)$", RegexOptions.Compiled);

  private static Dictionary<string, (bool Conditional, KeywordCallback Callback)> Keywords = new();

  private const bool DEBUG = false;
  private const bool LogErrors = true;

  private int TotalLines = 0;

  public void Init()
  {
    Keywords["both"] = (true, BothKeyword);
    Keywords["either"] = (true, EitherKeyword);
    Keywords["else"] = (false, ElseKeyword);
    Keywords["fail"] = (true, FailKeyword);
    Keywords["log"] = (false, LogKeyword);
    Keywords["not"] = (true, NotKeyword);
    Keywords["pass"] = (true, PassKeyword);
    Keywords["say"] = (false, SayKeyword);
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
    CPH.LogInfo("Running rewards management script");

    List<string> lines = File.ReadAllLines(ScriptFile).Select(x => x.Trim()).ToList();
    TotalLines = lines.Count;
    ScriptStatus status = ScriptStatus.True;

    // Iterate over all the lines of the script. But we're using a list
    // and lines.Count because some keywords can also delete lines early.
    while (lines.Count > 0)
    {
      string line = lines[0];
      lines.RemoveAt(0);

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
      ParseLine(null, line, lines, ref status);
    }

    // why do these have to be bools anyway
    return true;
  }

  public void ParseLine(bool? conditional, string line, List<string> lines, ref ScriptStatus status)
  {
    // Get the first keyword of the line
    string keyword;
    (keyword, line) = Keyword(line);

    // Since we're having problems, we'll print out how this line was
    // read.
    if (DEBUG)
    {
      if (line == null) CPH.LogInfo($"Keyword: {keyword}");
      else CPH.LogInfo($"Keyword: {keyword} / Line: {line}");
    }

    // See if it's an acceptable keyword
    // If not we'll ignore the line without affecting script status
    if (!Keywords.ContainsKey(keyword))
    {
      if (DEBUG) CPH.LogInfo($"Keyword '{keyword}' was not found.");
      return;
    }

    // If it is, then we'll have to parse that keyword!
    bool isConditional;
    KeywordCallback callback;
    (isConditional, callback) = Keywords[keyword];

    // Oh, but first, we should see if it's the correct conditional or
    // not.
    if (conditional.HasValue)
    {
      if (conditional.Value != isConditional)
      {
        status = ScriptStatus.Error;
        return;
      }
    }

    // *Now* parse it.
    callback(line, lines, ref status);

    // And because we're having problems, for DEBUG, print the current
    // ScriptStatus.
    if (DEBUG) CPH.LogInfo($"After '{keyword}', script status is {status}.");
  }

  // =======================
  // == KEYWORD CALLBACKS ==
  // =======================
  private void BothKeyword(string line, List<string> otherLines, ref ScriptStatus status)
  {
    // Error if this line is otherwise blank.
    if (line == null)
    {
      status = ScriptStatus.Error;
    }

    // Parse the rest of the first line...
    ScriptStatus stat1 = ScriptStatus.True;
    ParseLine(true, line, otherLines, ref stat1);

    if (stat1 == ScriptStatus.Error)
    {
      status = ScriptStatus.Error;
      return;
    }

    // ... and now the next line.
    string line2 = otherLines[0];
    if (line2 == "")
    {
      status = ScriptStatus.Error;
      return;
    }
    otherLines.RemoveAt(0);
    ScriptStatus stat2 = ScriptStatus.True;
    ParseLine(true, line2, otherLines, ref stat2);

    if (stat2 == ScriptStatus.Error) status = ScriptStatus.Error;
    if (stat1 == ScriptStatus.False || stat2 == ScriptStatus.False) status = ScriptStatus.False;
  }

  private void EitherKeyword(string line, List<string> otherLines, ref ScriptStatus status)
  {
    // Error if this line is otherwise blank.
    if (line == null)
    {
      status = ScriptStatus.Error;
    }

    // Parse the rest of the first line...
    ScriptStatus stat1 = ScriptStatus.True;
    ParseLine(true, line, otherLines, ref stat1);

    if (stat1 == ScriptStatus.Error)
    {
      status = ScriptStatus.Error;
      return;
    }

    // ... and now the next line.
    string line2 = otherLines[0];
    if (line2 == "")
    {
      status = ScriptStatus.Error;
      return;
    }
    otherLines.RemoveAt(0);
    ScriptStatus stat2 = ScriptStatus.True;
    ParseLine(true, line2, otherLines, ref stat2);

    if (stat2 == ScriptStatus.Error) status = ScriptStatus.Error;
    if (stat1 == ScriptStatus.False && stat2 == ScriptStatus.False) status = ScriptStatus.False;
  }

  private void ElseKeyword(string line, List<string> otherLines, ref ScriptStatus status)
  {
    if (line == null)
    {
      // A standalone "else" just inverts the status.
      if (status == ScriptStatus.True) status = ScriptStatus.False;
      else status = ScriptStatus.True;
    }
    else
    {
      // An "else" at the start of a line inverts the status *for this line*.
      ScriptStatus newStatus;
      if (status == ScriptStatus.True) newStatus = ScriptStatus.False;
      else newStatus = ScriptStatus.True;

      ParseLine(false, line, otherLines, ref newStatus);

      // And propogate errors upwards.
      if (newStatus == ScriptStatus.Error) status = ScriptStatus.Error;
    }
  }

  private void FailKeyword(string line, List<string> otherLines, ref ScriptStatus status)
  {
    status = ScriptStatus.False;
  }

  private void LogKeyword(string line, List<string> otherLines, ref ScriptStatus status)
  {
    if (status == ScriptStatus.True) CPH.LogInfo(line);
  }

  private void NotKeyword(string line, List<string> otherLines, ref ScriptStatus status)
  {
    // First parse a line with a new ScriptStatus...
    ScriptStatus newStatus = ScriptStatus.True;
    ParseLine(true, line, otherLines, ref newStatus);

    if (DEBUG) CPH.LogInfo($"NOT keyword: {line} returned {newStatus}");

    // And if it returns *true*, it's false. (Also propogate errors.)
    if (newStatus == ScriptStatus.Error) status = ScriptStatus.Error;
    else if (newStatus == ScriptStatus.True) status = ScriptStatus.False;
  }

  private void PassKeyword(string line, List<string> otherLines, ref ScriptStatus status)
  {
    // this actually does nothing at all besides exist
  }

  private void SayKeyword(string line, List<string> otherLines, ref ScriptStatus status)
  {
    if (status == ScriptStatus.True) CPH.SendMessage(line);
  }
}

public enum ScriptStatus
{
  False = 0,
  True = 1,
  Error = -1
}

internal delegate void KeywordCallback(string line, List<string> otherLines, ref ScriptStatus status);
