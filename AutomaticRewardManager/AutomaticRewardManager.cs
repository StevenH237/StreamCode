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
    Keywords["price"] = (false, PriceKeyword);
    Keywords["prices"] = (false, PricesKeyword);
    Keywords["say"] = (false, SayKeyword);
    Keywords["set"] = (false, SetKeyword);
    // Keywords["skip"] = (false, SkipKeyword);
    Keywords["title"] = (true, TitleKeyword);
    Keywords["update"] = (true, UpdateKeyword);
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

    List<(string Text, int Number)> lines = File.ReadAllLines(ScriptFile)
      .Select((x, i) => (Text: x.Trim(), Number: i + 1))
      .Where(x => !x.Text.StartsWith("//"))
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

    // why do these have to be bools anyway
    return true;
  }

  public void ParseLine(bool? conditional, (string Text, int Number) scriptLine, List<(string Text, int Number)> lines, ref ScriptStatus status)
  {
    string line = scriptLine.Text;

    // Get the first keyword of the line
    string keyword;
    (keyword, line) = Keyword(line);

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

  // =======================
  // == KEYWORD CALLBACKS ==
  // =======================
  private void BothKeyword((string Text, int Number) scriptLine, List<(string Text, int Number)> otherLines, ref ScriptStatus status)
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

  private void DisableKeyword((string Text, int Number) scriptLine, List<(string Text, int Number)> otherLines, ref ScriptStatus status)
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

  private void EitherKeyword((string Text, int Number) scriptLine, List<(string Text, int Number)> otherLines, ref ScriptStatus status)
  {
    string line = scriptLine.Text;
    // Error if this line is otherwise blank.
    if (line == null)
    {
      CPH.LogWarn($"ERROR: On line {scriptLine.Number}, no condition is given for the first half of \"either\" keyword.");
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


  private void ElseKeyword((string Text, int Number) scriptLine, List<(string Text, int Number)> otherLines, ref ScriptStatus status)
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
      // An "else" at the start of a line inverts the status *for this line*.
      ScriptStatus newStatus;
      if (status == ScriptStatus.True) newStatus = ScriptStatus.False;
      else newStatus = ScriptStatus.True;

      ParseLine(false, scriptLine, otherLines, ref newStatus);

      // And propogate errors upwards.
      if (newStatus == ScriptStatus.Error) status = ScriptStatus.Error;
    }
  }

  private void EnableKeyword((string Text, int Number) scriptLine, List<(string Text, int Number)> otherLines, ref ScriptStatus status)
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

  private void ErrorKeyword((string Text, int Number) scriptLine, List<(string Text, int Number)> otherLines, ref ScriptStatus status)
  {
    CPH.LogWarn($"ERROR: On line {scriptLine.Number}, \"error\" keyword used with the following message:");
    CPH.LogWarn(scriptLine.Text);
    status = ScriptStatus.Error;
  }

  private void FailKeyword((string Text, int Number) scriptLine, List<(string Text, int Number)> otherLines, ref ScriptStatus status)
  {
    status = ScriptStatus.False;
  }

  private void GameIsKeyword((string Text, int Number) scriptLine, List<(string Text, int Number)> otherLines, ref ScriptStatus status)
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

  private void GameKeyword((string Text, int Number) scriptLine, List<(string Text, int Number)> otherLines, ref ScriptStatus status)
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

  private void GameStartKeyword((string Text, int Number) scriptLine, List<(string Text, int Number)> otherLines, ref ScriptStatus status)
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

  private void LogKeyword((string Text, int Number) scriptLine, List<(string Text, int Number)> otherLines, ref ScriptStatus status)
  {
    if (status == ScriptStatus.True) CPH.LogInfo(scriptLine.Text);
  }

  private void NotKeyword((string Text, int Number) scriptLine, List<(string Text, int Number)> otherLines, ref ScriptStatus status)
  {
    // First parse a line with a new ScriptStatus...
    ScriptStatus newStatus = ScriptStatus.True;
    ParseLine(true, scriptLine, otherLines, ref newStatus);

    // And if it returns *true*, it's false. (Also propogate errors.)
    if (newStatus == ScriptStatus.Error) status = ScriptStatus.Error;
    else if (newStatus == ScriptStatus.True) status = ScriptStatus.False;
  }

  private void PassKeyword((string Text, int Number) scriptLine, List<(string Text, int Number)> otherLines, ref ScriptStatus status)
  {
    // this actually does nothing at all besides exist
  }

  private void PriceKeyword((string Text, int Number) scriptLine, List<(string Text, int Number)> otherLines, ref ScriptStatus status)
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

  private void PricesKeyword((string Text, int Number) scriptLine, List<(string Text, int Number)> otherLines, ref ScriptStatus status)
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

  private void SayKeyword((string Text, int Number) scriptLine, List<(string Text, int Number)> otherLines, ref ScriptStatus status)
  {
    if (status == ScriptStatus.True) CPH.SendMessage(scriptLine.Text);
  }

  private void SetKeyword((string Text, int Number) scriptLine, List<(string Text, int Number)> otherLines, ref ScriptStatus status)
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

  private void TitleKeyword((string Text, int Number) scriptLine, List<(string Text, int Number)> otherLines, ref ScriptStatus status)
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

  private void UpdateKeyword((string Text, int Number) scriptLine, List<(string Text, int Number)> otherLines, ref ScriptStatus status)
  {
    // If the conditions weren't met, we're not even interested in trying.
    if (status == ScriptStatus.False) return;

    string line = scriptLine.Text;

    // Get the object first
    StreamObject obj = GetScriptObject(ref line);
    ITextable priceObj = obj as ITextable;

    if (priceObj == null)
    {
      CPH.LogWarn($"ERROR: On line {scriptLine.Number}, no valid object was given to price-adjust.");
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
}

public enum ScriptStatus
{
  False = 0,
  True = 1,
  Error = -1
}

internal delegate void KeywordCallback((string Text, int Number) scriptLine, List<(string Text, int Number)> otherLines, ref ScriptStatus status);

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
