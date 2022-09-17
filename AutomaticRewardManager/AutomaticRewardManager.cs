// AutomaticRewardManager.cs
using System;
using System.Text.RegularExpressions;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Globalization;

using Streamer.bot.Plugin.Interface;

public class CPHInline
{
  private const string ScriptFile = @"C:\Users\Nixill\Documents\Streaming\Scripts\rewards.txt";

  private static Regex SplitString = new Regex(@"^([^\s]+?) +(.*)$", RegexOptions.Compiled);
  private static Regex MonthRegex = new Regex(@"^(0?[1-9]|1[12]|jan(?:uary)?|feb(?:ruary)?|mar(?:ch)?|apr(?:il)?|may|" +
    @"june?|july?|aug(?:ust)?|sep(?:tember)?|oct(?:ober)?|nov(?:ember)?|dec(?:ember)?)$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase);
  private static Regex DayRegex = new Regex(@"^(-?([012]?[1-9]|[123]0|31)|mon(?:day)?|tue(?:sday)?|wed(?:nesday)?|" +
    @"thu(?:rsday)?|fri(?:day)?|sat(?:urday)?|sun(?:day)?)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

  private static char[] Hyphen = new char[] { '-' };
  private static char[] Space = new char[] { ' ' };

  private static Dictionary<string, (bool Conditional, KeywordCallback Callback)> Keywords = new();
  private static Dictionary<string, bool> IgnoredKeywords = new();

  private int TotalLines = 0;

  private CompareInfo StringComparer = CultureInfo.InvariantCulture.CompareInfo;
  private CompareOptions ComparisonOptions = CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace;

  public void Init()
  {
    Keywords["both"] = (true, BothKeyword);
    Keywords["day"] = (true, DayKeyword);
    Keywords["disable"] = (false, DisableKeyword);
    Keywords["either"] = (true, EitherKeyword);
    Keywords["else"] = (false, ElseKeyword);
    Keywords["enable"] = (false, EnableKeyword);
    Keywords["fail"] = (true, FailKeyword);
    Keywords["game-is"] = (true, GameIsKeyword);
    Keywords["game-start"] = (true, GameStartKeyword);
    Keywords["game"] = (true, GameKeyword);
    Keywords["goto"] = (true, GotoKeyword);
    Keywords["log"] = (false, LogKeyword);
    Keywords["month"] = (true, MonthKeyword);
    Keywords["not"] = (true, NotKeyword);
    Keywords["pass"] = (true, PassKeyword);
    Keywords["price"] = (false, PriceKeyword);
    Keywords["prices"] = (false, PricesKeyword);
    Keywords["say"] = (false, SayKeyword);
    Keywords["set"] = (false, SetKeyword);
    Keywords["title"] = (true, TitleKeyword);
    Keywords["update"] = (false, UpdateKeyword);

    IgnoredKeywords["and"] = false;
    IgnoredKeywords["or"] = false;
    IgnoredKeywords["//"] = true;
    IgnoredKeywords["label"] = true;
    IgnoredKeywords["#"] = true;
    IgnoredKeywords["--"] = true;
  }

  // =====================
  // == UTILITY METHODS ==
  // =====================
  #region UTILITY METHODS
  private (string, string) Keyword(string input, bool lowercase = true)
  {
    Match mtc = SplitString.Match(input);

    string keyword;
    string restOfLine = null;

    if (mtc.Success)
    {
      keyword = mtc.Groups[1].Value;
      restOfLine = mtc.Groups[2].Value;
    }
    else keyword = input.Trim();

    if (lowercase) keyword = keyword.ToLower();

    return (keyword, restOfLine);
  }

  // Whether or not value starts with, matches, or contains input
  private bool StartsWithInsensitive(string input, string value) =>
    StringComparer.IsPrefix(value, input, ComparisonOptions);
  private bool MatchesInsensitive(string input, string value) =>
    StartsWithInsensitive(value, input) && StartsWithInsensitive(input, value);
  private bool ContainsInsensitive(string input, string value) =>
    StringComparer.IndexOf(value, input, ComparisonOptions) != -1;

  private StreamObject GetScriptObject(ref string line)
  {
    string objectType, objectID;
    (objectType, line) = Keyword(line);
    if (line == null) return null;

    (objectID, line) = Keyword(line);

    if (objectType == "reward") return new StreamReward(objectID);
    if (objectType == "timer") return new StreamTimer(objectID);
    if (objectType == "command") return new StreamCommand(objectID);
    if (objectType == "message") return new StreamTimedMessage(objectID);
    return null;
  }
  #endregion

  // ===============
  // == CALLABLES ==
  // ===============
  #region CALLABLES

  // ARGUMENTS IN
  //   %status% - New stream title
  //   %gameName% - New stream game
  public bool RunScript()
  {
    CPH.LogInfo("Running rewards management script");

    List<(string Text, int Number)> lines = File.ReadAllLines(ScriptFile)
      .Select((x, i) => (Text: x.Trim(), Number: i + 1))
      .ToList();
    TotalLines = lines.Count;
    ScriptStatus status = ScriptStatus.True;

    // Iterate over all the lines of the script. But we're using a list
    // and lines.Count because some keywords can also delete lines early.
    while (lines.Count > 0)
    {
      var scriptLine = lines[0];
      lines.RemoveAt(0);
      string line = scriptLine.Text;

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
      ParseLine(null, scriptLine, lines, ref status);
    }

    CPH.ExecuteMethod("Timed Message Dispatch", "UpdateFile");

    // why do these have to be bools anyway
    return true;
  }
  #endregion

  // ====================
  // == ACTION METHODS ==
  // ====================
  #region ACTION METHODS
  public void ParseLine(bool? conditional, (string Text, int Number) scriptLine, List<(string Text, int Number)> lines,
    ref ScriptStatus status)
  {
    string line = scriptLine.Text;

    // Get the first keyword of the line
    string keyword;
    (keyword, line) = Keyword(line);

    // Make sure it's not an ignored keyword
    while (IgnoredKeywords.ContainsKey(keyword))
    {
      // whether or not to skip the whole line
      if (IgnoredKeywords[keyword])
      {
        if (lines.Count == 0)
        {
          CPH.LogWarn($"ERROR: On line {scriptLine.Number}, unexpected end of script.");
          status = ScriptStatus.Error;
          return;
        }
        scriptLine = lines[0];
        if (scriptLine.Text == "")
        {
          CPH.LogWarn($"ERROR: On line {scriptLine.Number}, unexpected end of block.");
          status = ScriptStatus.Error;
          return;
        }
        lines.RemoveAt(0);
        (keyword, line) = Keyword(scriptLine.Text);
      }
      else
      {
        if (line == null)
        {
          CPH.LogWarn($"ERROR: On line {scriptLine.Number}, incomplete line.");
          status = ScriptStatus.Error;
          return;
        }
        (keyword, line) = Keyword(line);
      }
    }

    // Make sure it exists as a keyword
    if (!Keywords.ContainsKey(keyword))
    {
      CPH.LogWarn($"ERROR: On line {scriptLine.Number}, {keyword} is an invalid keyword.");
      status = ScriptStatus.Error;
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
        if (conditional.Value) CPH.LogWarn(
          $"ERROR: On line {scriptLine.Number}, conditional keyword expected but {keyword} is a directive.");
        else CPH.LogWarn(
          $"ERROR: On line {scriptLine.Number}, directive keyword expected but {keyword} is conditional.");
        status = ScriptStatus.Error;
        return;
      }
    }

    // *Now* parse it.
    callback((line, scriptLine.Number), lines, ref status);
  }
  #endregion

  // =======================
  // == KEYWORD CALLBACKS ==
  // =======================
  #region KEYWORD CALLBACKS
  private void BothKeyword((string Text, int Number) scriptLine, List<(string Text, int Number)> otherLines,
    ref ScriptStatus status)
  {
    string line = scriptLine.Text;
    // Error if this line is otherwise blank.
    if (line == null)
    {
      CPH.LogWarn($"ERROR: On line {scriptLine.Number}, no condition is given for the first half of \"both\" keyword.");
      status = ScriptStatus.Error;
      return;
    }

    // Parse the rest of the first line...
    ScriptStatus stat1 = ScriptStatus.True;
    ParseLine(true, scriptLine, otherLines, ref stat1);

    if (stat1 == ScriptStatus.Error)
    {
      status = ScriptStatus.Error;
      return;
    }

    // Make sure a next line exists!
    if (otherLines.Count == 0)
    {
      CPH.LogWarn($"ERROR: On line {scriptLine.Number}, unexpected end of script during \"both\" keyword.");
      status = ScriptStatus.Error;
      return;
    }

    // ... and now the next line.
    (string Text, int Number) scriptLine2 = otherLines[0];
    string line2 = scriptLine2.Text;
    if (line2 == "")
    {
      CPH.LogWarn($"ERROR: On line {scriptLine2.Number}, unexpected end of block during \"both\" keyword.");
      status = ScriptStatus.Error;
      return;
    }
    otherLines.RemoveAt(0);
    ScriptStatus stat2 = ScriptStatus.True;
    ParseLine(true, scriptLine2, otherLines, ref stat2);

    if (stat2 == ScriptStatus.Error) status = ScriptStatus.Error;
    if (stat1 == ScriptStatus.False || stat2 == ScriptStatus.False) status = ScriptStatus.False;
  }

  private void DayKeyword((string Text, int Number) scriptLine, List<(string Text, int Number)> otherLines,
    ref ScriptStatus status)
  {
    string line = scriptLine.Text;
    if (line == null)
    {
      CPH.LogWarn($"ERROR: On line {scriptLine.Number}, no days were provided to check against.");
      status = ScriptStatus.Error;
      return;
    }

    // We need today's date so we can get the total number of days in the
    // current month.
    DateTime today = DateTime.Today;

    HashSet<int> validDaysOfMonth = new HashSet<int>(31);
    HashSet<DayOfWeek> validDaysOfWeek = new HashSet<DayOfWeek>(7);
    int daysOfThisMonth = DateTime.DaysInMonth(today.Year, today.Month);

    // Now split it into individual values.
    foreach (string value in line.Split(Space).Where(x => x != ""))
    {
      // Deal with ranges, too. Unlike with month we have to be a *little*
      // more cautious about it since negatives are a thing.
      int index = value.IndexOf("-", 1);
      if (index > 0)
      {
        // Split the range into min and max.
        string mins = value.Substring(0, index);
        string maxs = value.Substring(index + 1);

        // Make sure they both pass the regex.
        if (!DayRegex.IsMatch(mins) || !DayRegex.IsMatch(maxs))
        {
          CPH.LogWarn($"WARNING: On line {scriptLine.Number}, {value} is not a valid day or range of days.");
          continue;
        }

        // Convert them to day numbers or names.
        if (int.TryParse(mins, out int min))
        {
          if (int.TryParse(maxs, out int max))
          {
            // This long nested if block converts days from end to days
            // from start, and warns if you try to mix those types.
            if (min < 0)
            {
              min = daysOfThisMonth + min + 1;
              if (max < 0)
              {
                max = daysOfThisMonth + max + 1;
              }
              else
              {
                CPH.LogWarn($"WARNING: On line {scriptLine.Number}, mixing days from start of month and days from " +
                  "end of month in a range may have unexpected effects!");
              }
            }
            else
            {
              if (max < 0)
              {
                max = daysOfThisMonth + max + 1;
                CPH.LogWarn($"WARNING: On line {scriptLine.Number}, mixing days from start of month and days from " +
                  "end of month in a range may have unexpected effects!");
              }
            }

            // This accounts for ranges that span the end of a month.
            if (min > max) max += 31;

            // And then the Select here undoes that accounting.
            foreach (int day in Enumerable.Range(min, max - min + 1).Select(x => (x - 1) % 31 + 1))
            {
              validDaysOfMonth.Add(day);
            }
          }
          else
          {
            CPH.LogWarn($"WARNING: On line {scriptLine.Number}, cannot mix day-of-month and day-of-week in a range!");
            continue;
          }
        }
        else
        {
          min = (int)TimeNames.DayNames[mins.ToLower().Substring(0, 2)];
          if (int.TryParse(maxs, out int max))
          {
            CPH.LogWarn($"WARNING: On line {scriptLine.Number}, cannot mix day-of-month and day-of-week in a range!");
            continue;
          }
          else
          {
            max = (int)TimeNames.DayNames[maxs.ToLower().Substring(0, 2)];

            // This accounts for ranges that span the end of a week.
            if (min > max) max += 7;

            // And then the Select here undoes that accounting. This
            // doesn't have the "subtract 1, add 1" magic the other two
            // have because days of week are numbered from 0.
            foreach (int day in Enumerable.Range(min, max - min + 1).Select(x => x % 7))
            {
              validDaysOfWeek.Add((DayOfWeek)day);
            }
          }
        }
      }
      else
      {
        // Make sure it passes regex!
        if (!DayRegex.IsMatch(value))
        {
          CPH.LogWarn($"WARNING: On line {scriptLine.Number}, {value} is not a valid day or range of days.");
          continue;
        }

        // Try to convert to a day-of-month number.
        if (int.TryParse(value, out int day))
        {
          // If it's from the end of month, convert it.
          if (day < 0) day = daysOfThisMonth + day + 1;

          // And add it.
          validDaysOfMonth.Add(day);
        }
        else
        {
          // Convert it to a day-of-week.
          DayOfWeek dow = TimeNames.DayNames[value.ToLower().Substring(0, 2)];

          // And add it.
          validDaysOfWeek.Add(dow);
        }
      }
    }

    // DEBUG STUFF
    CPH.LogInfo($"Line: day {line}");
    CPH.LogInfo($"Valid days of week: {string.Join(", ", validDaysOfWeek)}");
    CPH.LogInfo($"Valid days of month: {string.Join(", ", validDaysOfMonth)}");

    // If there were no valid days of month *or* week (remember, we
    // skipped invalid ones), then the condition errors.
    if (validDaysOfMonth.Count == 0 && validDaysOfWeek.Count == 0)
    {
      CPH.LogWarn($"ERROR: On line {scriptLine.Number}, no days were provided to check against.");
      status = ScriptStatus.Error;
      return;
    }

    // Remove days of month that aren't occurring this month. (This is
    // after the last line because technically "day 31" is a valid
    // condition even during months that don't have 31 days.)
    validDaysOfMonth.RemoveWhere(x => x <= 0 || x > daysOfThisMonth);

    // If all days of the month are valid, warn the script writer.
    if (validDaysOfMonth.Count == daysOfThisMonth)
    {
      CPH.LogWarn($"WARNING: On line {scriptLine.Number}, all days of the month are valid " +
        $"(at least for months containing {daysOfThisMonth} days).");
    }

    // If all days of the week are valid, warn the script writer.
    if (validDaysOfWeek.Count == 7)
    {
      CPH.LogWarn($"WARNING: On line {scriptLine.Number}, all days of the week are valid.");
    }

    // Unlike with the month checker, we already have today's date.
    if ((validDaysOfMonth.Count > 0 && !validDaysOfMonth.Contains(today.Day))
      || (validDaysOfWeek.Count > 0 && !validDaysOfWeek.Contains(today.DayOfWeek)))
      status = ScriptStatus.False;
  }

  private void DisableKeyword((string Text, int Number) scriptLine, List<(string Text, int Number)> otherLines,
    ref ScriptStatus status)
  {
    // If the conditions weren't met, we're not even interested in trying.
    if (status == ScriptStatus.False) return;

    string line = scriptLine.Text;

    // Get the toggleable object.
    var obj = GetScriptObject(ref line);
    var toggle = (obj as IToggleable);

    if (toggle == null)
    {
      CPH.LogWarn($"ERROR: On line {scriptLine.Number}, no valid object was given to disable.");
      status = ScriptStatus.Error;
      return;
    }

    toggle.Disable(CPH);
  }

  private void EitherKeyword((string Text, int Number) scriptLine, List<(string Text, int Number)> otherLines,
    ref ScriptStatus status)
  {
    string line = scriptLine.Text;
    // Error if this line is otherwise blank.
    if (line == null)
    {
      CPH.LogWarn(
        $"ERROR: On line {scriptLine.Number}, no condition is given for the first half of \"either\" keyword.");
      status = ScriptStatus.Error;
      return;
    }

    // Parse the rest of the first line...
    ScriptStatus stat1 = ScriptStatus.True;
    ParseLine(true, scriptLine, otherLines, ref stat1);

    if (stat1 == ScriptStatus.Error)
    {
      status = ScriptStatus.Error;
      return;
    }

    // Make sure a next line exists!
    if (otherLines.Count == 0)
    {
      CPH.LogWarn($"ERROR: On line {scriptLine.Number}, unexpected end of script during \"either\" keyword.");
      status = ScriptStatus.Error;
      return;
    }

    // ... and now the next line.
    (string Text, int Number) scriptLine2 = otherLines[0];
    string line2 = scriptLine2.Text;
    if (line2 == "")
    {
      CPH.LogWarn($"ERROR: On line {scriptLine2.Number}, unexpected end of block during \"either\" keyword.");
      status = ScriptStatus.Error;
      return;
    }
    otherLines.RemoveAt(0);
    ScriptStatus stat2 = ScriptStatus.True;
    ParseLine(true, scriptLine2, otherLines, ref stat2);

    if (stat2 == ScriptStatus.Error) status = ScriptStatus.Error;
    if (stat1 == ScriptStatus.False && stat2 == ScriptStatus.False) status = ScriptStatus.False;
  }


  private void ElseKeyword((string Text, int Number) scriptLine, List<(string Text, int Number)> otherLines,
    ref ScriptStatus status)
  {
    string line = scriptLine.Text;

    if (line == null)
    {
      // A standalone "else" just inverts the status.
      if (status == ScriptStatus.True) status = ScriptStatus.False;
      else status = ScriptStatus.True;
    }
    else
    {
      // An "else" at the start of a line inverts the status *for this
      // line*.
      ScriptStatus newStatus;
      if (status == ScriptStatus.True) newStatus = ScriptStatus.False;
      else newStatus = ScriptStatus.True;

      ParseLine(false, scriptLine, otherLines, ref newStatus);

      // And propogate errors upwards.
      if (newStatus == ScriptStatus.Error) status = ScriptStatus.Error;
    }
  }

  private void EnableKeyword((string Text, int Number) scriptLine, List<(string Text, int Number)> otherLines,
    ref ScriptStatus status)
  {
    // If the conditions weren't met, we're not even interested in trying.
    if (status == ScriptStatus.False) return;

    string line = scriptLine.Text;

    // Get the toggleable object.
    var obj = GetScriptObject(ref line);
    var toggle = (obj as IToggleable);

    if (toggle == null)
    {
      CPH.LogWarn($"ERROR: On line {scriptLine.Number}, no valid object was given to enable.");
      status = ScriptStatus.Error;
      return;
    }

    toggle.Enable(CPH);
  }

  private void ErrorKeyword((string Text, int Number) scriptLine, List<(string Text, int Number)> otherLines,
    ref ScriptStatus status)
  {
    CPH.LogWarn($"ERROR: On line {scriptLine.Number}, \"error\" keyword used with the following message:");
    CPH.LogWarn(scriptLine.Text);
    status = ScriptStatus.Error;
  }

  private void FailKeyword((string Text, int Number) scriptLine, List<(string Text, int Number)> otherLines,
    ref ScriptStatus status)
  {
    status = ScriptStatus.False;
  }

  private void GameIsKeyword((string Text, int Number) scriptLine, List<(string Text, int Number)> otherLines,
    ref ScriptStatus status)
  {
    string line = scriptLine.Text;

    // First off, it's an error if there's no input to check, or no game
    // name to check against.
    if (line == null)
    {
      CPH.LogWarn($"ERROR: On line {scriptLine.Number}, no game name was provided to check.");
      status = ScriptStatus.Error;
      return;
    }

    if (!args.ContainsKey("gameName"))
    {
      CPH.LogWarn($"ERROR: On line {scriptLine.Number}, \"game-is\" has no game to check against.");
      status = ScriptStatus.Error;
      return;
    }

    // For the "game-is" keyword, we have to get the game title...
    string game = (string)args["gameName"];

    // And now compare it to the input:
    bool match = MatchesInsensitive(line, game);

    if (!match)
    {
      status = ScriptStatus.False;
    }
  }

  private void GameKeyword((string Text, int Number) scriptLine, List<(string Text, int Number)> otherLines,
    ref ScriptStatus status)
  {
    string line = scriptLine.Text;

    // First off, it's an error if there's no input to check, or no game
    // name to check against.
    if (line == null)
    {
      CPH.LogWarn($"ERROR: On line {scriptLine.Number}, no game name was provided to check.");
      status = ScriptStatus.Error;
      return;
    }

    if (!args.ContainsKey("gameName"))
    {
      CPH.LogWarn($"ERROR: On line {scriptLine.Number}, \"game\" has no game to check against.");
      status = ScriptStatus.Error;
      return;
    }

    // For the "game" keyword, we have to get the game title...
    string game = (string)args["gameName"];

    // And now compare it to the input:
    bool match = ContainsInsensitive(line, game);

    if (!match)
    {
      status = ScriptStatus.False;
    }
  }

  private void GameStartKeyword((string Text, int Number) scriptLine, List<(string Text, int Number)> otherLines,
    ref ScriptStatus status)
  {
    string line = scriptLine.Text;

    // First off, it's an error if there's no input to check, or no game
    // name to check against.
    if (line == null)
    {
      CPH.LogWarn($"ERROR: On line {scriptLine.Number}, no game name was provided to check.");
      status = ScriptStatus.Error;
      return;
    }

    if (!args.ContainsKey("gameName"))
    {
      CPH.LogWarn($"ERROR: On line {scriptLine.Number}, \"game-start\" has no game to check against.");
      status = ScriptStatus.Error;
      return;
    }

    // For the "game" keyword, we have to get the game title...
    string game = (string)args["gameName"];

    // And now compare it to the input:
    bool match = ContainsInsensitive(line, game);

    if (!match)
    {
      status = ScriptStatus.False;
    }
  }

  private void GotoKeyword((string Text, int Number) scriptLine, List<(string Text, int Number)> otherLines,
    ref ScriptStatus status)
  {
    // Skip if conditions are unmet.
    if (status == ScriptStatus.False) return;

    // Get the text first
    string line = scriptLine.Text;

    if (line == null)
    {
      CPH.LogWarn(
        $"WARNING: On line {scriptLine.Number}, an unspecific goto is used. Gotos should always be specific.");
    }
    else
    {
      line = line.ToLower();
    }

    while (otherLines.Count > 0)
    {
      // Take the top line off the script
      (string Text, int Number) scriptLine2 = otherLines[0];
      otherLines.RemoveAt(0);
      string line2 = scriptLine2.Text;

      // If it's blank, try again
      if (line2 == "") continue;
      string keyword;
      (keyword, line2) = Keyword(line2);

      // If it's not a label, try again
      if (keyword != "label") continue;

      // If it's a label, is it the right one (or any, for an unspecific
      // goto)? If so, stop skipping. We're done.
      if (line == null || line == line2) return;

      // Otherwise, try again.
    }

    // If we depleted the whole script, throw an error and return.
    CPH.LogWarn(
      $"FATAL: On line {scriptLine.Number}, a goto was provided without a matching label. " +
      "(goto can only move down the script, not up.)");
    // not that I think this does anything now that the rest of the script
    // is gone, but just in case
    status = ScriptStatus.Error;
  }

  private void LogKeyword((string Text, int Number) scriptLine, List<(string Text, int Number)> otherLines,
    ref ScriptStatus status)
  {
    if (status == ScriptStatus.True) CPH.LogInfo(scriptLine.Text);
  }

  private void MonthKeyword((string Text, int Number) scriptLine, List<(string Text, int Number)> otherLines,
    ref ScriptStatus status)
  {
    // Let's get the rest of this line...
    string line = scriptLine.Text;
    if (line == null)
    {
      CPH.LogWarn($"ERROR: On line {scriptLine.Number}, no months were provided to check against.");
      status = ScriptStatus.Error;
      return;
    }

    HashSet<int> validMonths = new HashSet<int>(12);

    // Now split it into individual values.
    foreach (string value in line.Split(Space).Where(x => x != ""))
    {
      // Deal with ranges, too.
      int index = value.IndexOf("-", 1);
      if (index > 0)
      {
        // Split the range into min and max.
        string mins = value.Substring(0, index);
        string maxs = value.Substring(index + 1);

        // Make sure they both pass the regex.
        if (!MonthRegex.IsMatch(mins) || !MonthRegex.IsMatch(maxs))
        {
          CPH.LogWarn($"WARNING: On line {scriptLine.Number}, {value} is not a valid month or range of months.");
          continue;
        }

        // Convert them to month numbers.
        if (!int.TryParse(mins, out int min)) min = TimeNames.MonthNames[mins.ToLower().Substring(0, 3)];
        if (!int.TryParse(maxs, out int max)) max = TimeNames.MonthNames[maxs.ToLower().Substring(0, 3)];

        // If we're dealing with a range that spans end-of-year, account
        // for that.
        if (min > max) max += 12;

        // The Select here accounts for the widening above by bringing
        // everything back within range.
        foreach (int i in Enumerable.Range(min, max - min + 1).Select(x => (x - 1) % 12 + 1))
        {
          validMonths.Add(i);
        }
      }
      else
      {
        // Make sure it passes regex!
        if (!MonthRegex.IsMatch(value))
        {
          CPH.LogWarn($"WARNING: On line {scriptLine.Number}, {value} is not a valid month or range of months.");
          continue;
        }

        // Convert to a month number.
        if (!int.TryParse(value, out int month)) month = TimeNames.MonthNames[value.ToLower().Substring(0, 3)];

        validMonths.Add(month);
      }
    }

    // DEBUG STUFF
    CPH.LogInfo($"Line: month {line}");
    CPH.LogInfo($"Valid months: {string.Join(", ", validMonths)}");

    // If there were no valid months (remember, we skipped invalid ones),
    // then the condition errors.
    if (validMonths.Count == 0)
    {
      CPH.LogWarn($"ERROR: On line {scriptLine.Number}, no months were provided to check against.");
      status = ScriptStatus.Error;
      return;
    }

    // If all months are valid, warn the script writer, but let the
    // condition pass.
    if (validMonths.Count == 12)
    {
      CPH.LogWarn($"WARNING: On line {scriptLine.Number}, all months are valid.");
    }

    // Otherwise, ugh, fine, we'll actually have to get today's date out
    // and check.
    DateTime today = DateTime.Today;
    if (!validMonths.Contains(today.Month)) status = ScriptStatus.False;
  }

  private void NotKeyword((string Text, int Number) scriptLine, List<(string Text, int Number)> otherLines,
    ref ScriptStatus status)
  {
    // First parse a line with a new ScriptStatus...
    ScriptStatus newStatus = ScriptStatus.True;
    ParseLine(true, scriptLine, otherLines, ref newStatus);

    // And if it returns *true*, it's false. (Also propogate errors.)
    if (newStatus == ScriptStatus.Error) status = ScriptStatus.Error;
    else if (newStatus == ScriptStatus.True) status = ScriptStatus.False;
  }

  private void PassKeyword((string Text, int Number) scriptLine, List<(string Text, int Number)> otherLines,
    ref ScriptStatus status)
  {
    // this actually does nothing at all besides exist
  }

  private void PriceKeyword((string Text, int Number) scriptLine, List<(string Text, int Number)> otherLines,
    ref ScriptStatus status)
  {
    // If the conditions weren't met, we're not even interested in trying.
    if (status == ScriptStatus.False) return;

    string line = scriptLine.Text;

    // Get the object first
    StreamObject obj = GetScriptObject(ref line);
    IPriceable priceObj = obj as IPriceable;

    if (priceObj == null)
    {
      CPH.LogWarn($"ERROR: On line {scriptLine.Number}, no valid object was given to price-adjust.");
      status = ScriptStatus.Error;
      return;
    }

    // Now get a price
    if (line == null)
    {
      CPH.LogWarn($"ERROR: On line {scriptLine.Number}, no price was given to set.");
      status = ScriptStatus.Error;
      return;
    }

    string priceStr;
    (priceStr, line) = Keyword(line);

    if (!int.TryParse(priceStr, out int price))
    {
      CPH.LogWarn($"ERROR: On line {scriptLine.Number}, {priceStr} is not a valid price.");
      status = ScriptStatus.Error;
      return;
    }

    // And now set the price
    priceObj.SetPrice(CPH, price);
  }

  private void PricesKeyword((string Text, int Number) scriptLine, List<(string Text, int Number)> otherLines,
    ref ScriptStatus status)
  {
    string line = scriptLine.Text;

    // Get the object first
    StreamObject obj = GetScriptObject(ref line);
    IPriceable priceObj = obj as IPriceable;

    if (priceObj == null)
    {
      CPH.LogWarn($"ERROR: On line {scriptLine.Number}, no valid object was given to price-adjust.");
      status = ScriptStatus.Error;
      return;
    }

    // Now get a price
    if (line == null)
    {
      CPH.LogWarn($"ERROR: On line {scriptLine.Number}, no prices were given to set.");
      status = ScriptStatus.Error;
      return;
    }

    string priceStr;
    (priceStr, line) = Keyword(line);

    if (!int.TryParse(priceStr, out int price))
    {
      CPH.LogWarn($"ERROR: On line {scriptLine.Number}, {priceStr} is not a valid price.");
      status = ScriptStatus.Error;
      return;
    }

    if (line == null)
    {
      CPH.LogWarn($"ERROR: On line {scriptLine.Number}, no second price was given to set.");
      status = ScriptStatus.Error;
      return;
    }

    string price2Str;
    (price2Str, line) = Keyword(line);

    if (!int.TryParse(price2Str, out int price2))
    {
      CPH.LogWarn($"ERROR: On line {scriptLine.Number}, {price2Str} is not a valid price.");
      status = ScriptStatus.Error;
      return;
    }

    // And now set the price
    if (status == ScriptStatus.True) priceObj.SetPrice(CPH, price);
    else priceObj.SetPrice(CPH, price2);
  }

  private void SayKeyword((string Text, int Number) scriptLine, List<(string Text, int Number)> otherLines,
    ref ScriptStatus status)
  {
    if (status == ScriptStatus.True) CPH.SendMessage(scriptLine.Text);
  }

  private void SetKeyword((string Text, int Number) scriptLine, List<(string Text, int Number)> otherLines,
    ref ScriptStatus status)
  {
    string line = scriptLine.Text;

    // Get the toggleable object.
    var obj = GetScriptObject(ref line);
    var toggle = (obj as IToggleable);

    if (toggle == null)
    {
      CPH.LogWarn($"ERROR: On line {scriptLine.Number}, no valid object was given to set.");
      status = ScriptStatus.Error;
      return;
    }

    if (status == ScriptStatus.True) toggle.Enable(CPH);
    else toggle.Disable(CPH);
  }

  private void TitleKeyword((string Text, int Number) scriptLine, List<(string Text, int Number)> otherLines,
    ref ScriptStatus status)
  {
    string line = scriptLine.Text;

    // First off, it's an error if there's no input to check, or no title
    // to check against.
    if (line == null)
    {
      CPH.LogWarn($"ERROR: On line {scriptLine.Number}, no title word was provided to check.");
      status = ScriptStatus.Error;
      return;
    }

    if (!args.ContainsKey("status"))
    {
      CPH.LogWarn($"ERROR: On line {scriptLine.Number}, \"title\" has no stream title to check against.");
      status = ScriptStatus.Error;
      return;
    }

    // For the "game" keyword, we have to get the game title...
    string title = (string)args["status"];

    // And now compare it to the input:
    bool match = ContainsInsensitive(line, title);

    if (!match)
    {
      status = ScriptStatus.False;
    }
  }

  private void UpdateKeyword((string Text, int Number) scriptLine, List<(string Text, int Number)> otherLines,
    ref ScriptStatus status)
  {
    // If the conditions weren't met, we're not even interested in trying.
    if (status == ScriptStatus.False) return;

    string line = scriptLine.Text;

    // Get the object first
    StreamObject obj = GetScriptObject(ref line);
    ITextable priceObj = obj as ITextable;

    if (priceObj == null)
    {
      CPH.LogWarn($"ERROR: On line {scriptLine.Number}, no valid object was given to set text.");
      status = ScriptStatus.Error;
      return;
    }

    // Now get the text
    if (line == null)
    {
      CPH.LogWarn($"ERROR: On line {scriptLine.Number}, no text was given to set.");
      status = ScriptStatus.Error;
      return;
    }

    // And now set the message
    priceObj.Update(CPH, line);
  }
  #endregion
}

// ===================
// == OTHER CLASSES ==
// ===================
#region OTHER CLASSES
public enum ScriptStatus
{
  False = 0,
  True = 1,
  Error = -1
}

internal delegate void KeywordCallback((string Text, int Number) scriptLine, List<(string Text, int Number)> otherLines,
  ref ScriptStatus status);

internal abstract class StreamObject
{
  protected string ID;
  protected StreamObject(string id)
  {
    ID = id;
  }
}

internal interface IToggleable
{
  internal void Disable(IInlineInvokeProxy CPH);
  internal void Enable(IInlineInvokeProxy CPH);
}

internal interface ITextable
{
  internal void Update(IInlineInvokeProxy CPH, string text);
}

internal interface IPriceable
{
  internal void SetPrice(IInlineInvokeProxy CPH, int price);
}

internal class StreamCommand : StreamObject, IToggleable
{
  internal StreamCommand(string id) : base(id) { }

  void IToggleable.Disable(IInlineInvokeProxy CPH) => CPH.DisableCommand(ID);
  void IToggleable.Enable(IInlineInvokeProxy CPH) => CPH.EnableCommand(ID);
}

internal class StreamTimer : StreamObject, IToggleable
{
  internal StreamTimer(string id) : base(id) { }

  void IToggleable.Disable(IInlineInvokeProxy CPH) => CPH.DisableTimer(ID);
  void IToggleable.Enable(IInlineInvokeProxy CPH) => CPH.EnableTimer(ID);
}

internal class StreamReward : StreamObject, IToggleable, ITextable, IPriceable
{
  internal StreamReward(string id) : base(id) { }

  void IToggleable.Disable(IInlineInvokeProxy CPH) => CPH.DisableReward(ID);
  void IToggleable.Enable(IInlineInvokeProxy CPH) => CPH.EnableReward(ID);
  void ITextable.Update(IInlineInvokeProxy CPH, string text) => CPH.UpdateRewardPrompt(ID, text);
  void IPriceable.SetPrice(IInlineInvokeProxy CPH, int price) => CPH.UpdateRewardCost(ID, price);
}

internal class StreamTimedMessage : StreamObject, IToggleable, ITextable
{
  internal StreamTimedMessage(string id) : base(id) { }

  void IToggleable.Disable(IInlineInvokeProxy CPH)
  {
    CPH.SetArgument("messageID", ID);
    CPH.SetArgument("messageEnabled", false);
    CPH.ExecuteMethod("Timed Message Dispatch", "ToggleMessage");
  }

  void IToggleable.Enable(IInlineInvokeProxy CPH)
  {
    CPH.SetArgument("messageID", ID);
    CPH.SetArgument("messageEnabled", true);
    CPH.ExecuteMethod("Timed Message Dispatch", "ToggleMessage");
  }

  void ITextable.Update(IInlineInvokeProxy CPH, string text)
  {
    CPH.SetArgument("messageID", ID);
    CPH.SetArgument("messageText", text);
    CPH.ExecuteMethod("Timed Message Dispatch", "EditMessage");
  }
}

internal static class TimeNames
{
  internal static Dictionary<string, DayOfWeek> DayNames = new()
  {
    ["mo"] = DayOfWeek.Monday,
    ["tu"] = DayOfWeek.Tuesday,
    ["we"] = DayOfWeek.Wednesday,
    ["th"] = DayOfWeek.Thursday,
    ["fr"] = DayOfWeek.Friday,
    ["sa"] = DayOfWeek.Saturday,
    ["su"] = DayOfWeek.Sunday
  };

  internal static Dictionary<string, int> MonthNames = new()
  {
    ["jan"] = 1,
    ["feb"] = 2,
    ["mar"] = 3,
    ["apr"] = 4,
    ["may"] = 5,
    ["jun"] = 6,
    ["jul"] = 7,
    ["aug"] = 8,
    ["sep"] = 9,
    ["oct"] = 10,
    ["nov"] = 11,
    ["dec"] = 12
  };
}
#endregion