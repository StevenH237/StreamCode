using System;
using System.Text.RegularExpressions;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Globalization;

public class CPHInline
{
  private const string ScriptFile = @"C:\Users\Nixill\Documents\Streaming\Scripts\rewards.txt";
  private static Regex SplitString = new Regex(@"^([^\s]+?) +(.*)$", RegexOptions.Compiled);

  private static Dictionary<string, (bool Conditional, KeywordCallback Callback)> Keywords = new();

  private const bool DEBUG = false;
  private const bool LogErrors = true;

  private int TotalLines = 0;

  private CompareInfo StringComparer = CultureInfo.InvariantCulture.CompareInfo;
  private CompareOptions ComparisonOptions = CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace;

  public void Init()
  {
    Keywords["both"] = (true, BothKeyword);
    Keywords["disable"] = (false, DisableKeyword);
    Keywords["either"] = (true, EitherKeyword);
    Keywords["else"] = (false, ElseKeyword);
    Keywords["enable"] = (false, EnableKeyword);
    Keywords["fail"] = (true, FailKeyword);
    Keywords["game-is"] = (true, GameIsKeyword);
    Keywords["game-start"] = (true, GameStartKeyword);
    Keywords["game"] = (true, GameKeyword);
    Keywords["log"] = (false, LogKeyword);
    Keywords["not"] = (true, NotKeyword);
    Keywords["pass"] = (true, PassKeyword);
    // Keywords["price"] = (false, PriceKeyword);
    // Keywords["prices"] = (false, PricesKeyword);
    Keywords["say"] = (false, SayKeyword);
    Keywords["set"] = (false, SetKeyword);
    // Keywords["skip"] = (false, SkipKeyword);
    Keywords["title"] = (true, TitleKeyword);
    // Keywords["update"] = (true, UpdateKeyword);
  }

  // =====================
  // == UTILITY METHODS ==
  // =====================
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
  private bool StartsWithInsensitive(string input, string value) => StringComparer.IsPrefix(value, input, ComparisonOptions);
  private bool MatchesInsensitive(string input, string value) => StartsWithInsensitive(value, input) && StartsWithInsensitive(input, value);
  private bool ContainsInsensitive(string input, string value) => StringComparer.IndexOf(value, input, ComparisonOptions) != -1;

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

  private void DisableKeyword(string line, List<string> otherLines, ref ScriptStatus status)
  {
    // If the conditions weren't met, we're not even interested in trying.
    if (status == ScriptStatus.False) return;

    // Get the toggleable object.
    var obj = GetScriptObject(ref line);
    var toggle = (obj as IToggleable);

    if (toggle == null)
    {
      status = ScriptStatus.Error;
      return;
    }

    toggle.Disable(CPH);
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

  private void EnableKeyword(string line, List<string> otherLines, ref ScriptStatus status)
  {
    // If the conditions weren't met, we're not even interested in trying.
    if (status == ScriptStatus.False) return;

    // Get the toggleable object.
    var obj = GetScriptObject(ref line);
    var toggle = (obj as IToggleable);

    if (toggle == null)
    {
      status = ScriptStatus.Error;
      return;
    }

    toggle.Enable(CPH);
  }

  private void FailKeyword(string line, List<string> otherLines, ref ScriptStatus status)
  {
    status = ScriptStatus.False;
  }

  private void GameIsKeyword(string line, List<string> otherLines, ref ScriptStatus status)
  {
    // First off, it's an error if there's no input to check, or no game
    // name to check against.
    if (line == null || !args.ContainsKey("gameName"))
    {
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

  private void GameKeyword(string line, List<string> otherLines, ref ScriptStatus status)
  {
    // First off, it's an error if there's no input to check, or no game
    // name to check against.
    if (line == null || !args.ContainsKey("gameName"))
    {
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

  private void GameStartKeyword(string line, List<string> otherLines, ref ScriptStatus status)
  {
    // First off, it's an error if there's no input to check, or no game
    // name to check against.
    if (line == null || !args.ContainsKey("gameName"))
    {
      status = ScriptStatus.Error;
      return;
    }

    // For the "game" keyword, we have to get the game title...
    string game = (string)args["gameName"];

    // And now compare it to the input:
    bool match = StartsWithInsensitive(line, game);

    if (!match)
    {
      status = ScriptStatus.False;
    }
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

  private void SetKeyword(string line, List<string> otherLines, ref ScriptStatus status)
  {
    // Get the toggleable object.
    var obj = GetScriptObject(ref line);
    var toggle = (obj as IToggleable);

    if (toggle == null)
    {
      status = ScriptStatus.Error;
      return;
    }

    if (status == ScriptStatus.True) toggle.Enable(CPH);
    else toggle.Disable(CPH);
  }

  private void TitleKeyword(string line, List<string> otherLines, ref ScriptStatus status)
  {
    // First off, it's an error if there's no input to check, or no game
    // name to check against.
    if (line == null || !args.ContainsKey("status"))
    {
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
}

public enum ScriptStatus
{
  False = 0,
  True = 1,
  Error = -1
}

internal delegate void KeywordCallback(string line, List<string> otherLines, ref ScriptStatus status);

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
  internal void Disable(Plugins.InlineInvokeProxy CPH);
  internal void Enable(Plugins.InlineInvokeProxy CPH);
}

internal interface ITextable
{
  internal void Update(Plugins.InlineInvokeProxy CPH, string text);
}

internal interface IPriceable
{
  internal void SetPrice(Plugins.InlineInvokeProxy CPH, int price);
}

internal class StreamCommand : StreamObject, IToggleable
{
  internal StreamCommand(string id) : base(id) { }

  void IToggleable.Disable(Plugins.InlineInvokeProxy CPH) => CPH.DisableCommand(ID);
  void IToggleable.Enable(Plugins.InlineInvokeProxy CPH) => CPH.EnableCommand(ID);
}

internal class StreamTimer : StreamObject, IToggleable
{
  internal StreamTimer(string id) : base(id) { }

  void IToggleable.Disable(Plugins.InlineInvokeProxy CPH) => CPH.DisableTimer(ID);
  void IToggleable.Enable(Plugins.InlineInvokeProxy CPH) => CPH.EnableTimer(ID);
}

internal class StreamReward : StreamObject, IToggleable, ITextable, IPriceable
{
  internal StreamReward(string id) : base(id) { }

  void IToggleable.Disable(Plugins.InlineInvokeProxy CPH) => CPH.DisableReward(ID);
  void IToggleable.Enable(Plugins.InlineInvokeProxy CPH) => CPH.EnableReward(ID);
  void ITextable.Update(Plugins.InlineInvokeProxy CPH, string text) => CPH.UpdateRewardPrompt(ID, text);
  void IPriceable.SetPrice(Plugins.InlineInvokeProxy CPH, int price) => CPH.UpdateRewardCost(ID, price);
}

internal class StreamTimedMessage : StreamObject, IToggleable, ITextable
{
  internal StreamTimedMessage(string id) : base(id) { }

  void IToggleable.Disable(Plugins.InlineInvokeProxy CPH)
  {
    CPH.SetArgument("messageID", ID);
    CPH.SetArgument("messageEnabled", false);
    CPH.RunAction("Timed Messages Toggle");
  }

  void IToggleable.Enable(Plugins.InlineInvokeProxy CPH)
  {
    CPH.SetArgument("messageID", ID);
    CPH.SetArgument("messageEnabled", true);
    CPH.RunAction("Timed Messages Toggle");
  }

  void ITextable.Update(Plugins.InlineInvokeProxy CPH, string text)
  {
    CPH.SetArgument("messageID", ID);
    CPH.SetArgument("messageText", text);
    CPH.RunAction("Timed Messages Update");
  }
}
