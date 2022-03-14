using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq;
using System.Collections.Generic;

public class CPHInline
{
  public void Init() => UpdateQueue();
  public void Dispose() => UpdateFile();

  private bool Saving = false;
  private const string MessageFile = @"C:\Users\Nixill\Documents\Streaming\messages.txt";

  private List<(string ID, bool Enabled, string Message)> Messages;
  private static Regex MessageFormat = new Regex(@"^\[([+-])([A-Za-z0-9-_]{1,32})\] (.+)$",
    RegexOptions.Compiled
  );

  // ARGUMENTS IN: none
  // RETURNS: Always true
  public bool UpdateQueue()
  {
    Messages = new List<(string, bool, string)>();

    foreach (string line in File.ReadAllLines(MessageFile))
    {
      Match mtc = MessageFormat.Match(line);
      HashSet<string> msgs = new HashSet<string>();

      if (mtc.Success)
      {
        string id = mtc.Groups[2].Value;

        // Make sure we haven't duplicated an ID
        if (!msgs.Contains(id))
        {
          string message = mtc.Groups[3].Value;
          bool enabled = mtc.Groups[1].Value == "+";

          Messages.Add((id, enabled, message));
          msgs.Add(id);
        }
      }
    }

    return true;
  }

  // ARGUMENTS IN: none
  // RETURNS: True iff UpdateQueue() was actually run
  public bool ConditionalUpdateQueue()
  {
    if (Saving)
    {
      Saving = false;
      return false;
    }

    return UpdateQueue();
  }

  // ARGUMENTS IN: none
  // RETURNS: Always true
  public bool UpdateFile()
  {
    File.WriteAllLines(MessageFile, Messages.Select(
      x => "[" + (x.Enabled ? "+" : "-") + x.ID + "] " + x.Message
    ));

    Saving = true;

    return true;
  }

  // ARGUMENTS IN
  //   %messageID% - The ID of the message to add.
  //   %messageText% - The text of the message to add.
  //   %messageEnabled% - Whether or not the message should be enabled.
  //     Defaults to true.
  //   %messageSkipUpdate% - Should file updating be skipped?
  //     Defaults to false. Use this for batch updates to the list.
  // RETURNS: Always true.
  public bool AddMessage()
  {
    string id = (string)args["messageID"];
    string text = (string)args["messageText"];
    bool enabled = true;
    if (args.ContainsKey("messageEnabled")) enabled = (bool)args["messageText"];

    // First remove any existing message with that ID.
    Messages.RemoveAll(x => x.ID == id);

    Messages.Add((id, enabled, text));

    UpdateFile();

    return true;
  }

  // ARGUMENTS IN
  //   %messageID% - The ID of the message to toggle.
  //   %messageEnabled% - If true or false, set the message to that state.
  //     Otherwise, invert the state of that message.
  //   %messageSkipUpdate% - Should file updating be skipped?
  //     Defaults to false. Use this for batch updates to the list.
  // RETURNS: True iff the message exists.
  public bool ToggleMessage()
  {
    string id = (string)args["messageID"];
    int index = Messages.FindIndex(x => x.ID == id);

    // No message found?
    if (index == -1) return false;

    var msg = Messages[index];
    bool enabled = !msg.Enabled;
    if (args.ContainsKey("messageEnabled") && args["messageEnabled"] is bool)
      enabled = (bool)args["messageEnabled"];

    msg.Enabled = enabled;

    Messages[index] = msg; // Not sure if necessary?

    if (!args.ContainsKey("messageSkipUpdate") ||
      !(args["messageSkipUpdate"] is bool) ||
      !((bool)args["messageSkipUpdate"]))
      UpdateFile();

    return true;
  }

  // ARGUMENTS IN
  //   %messageID% - The ID of the message to edit.
  //   %messageText% - The new text of the message.
  //   %messageSkipUpdate% - Should file updating be skipped?
  //     Defaults to false. Use this for batch updates to the list.
  // RETURNS: True iff the message exists.
  public bool EditMessage()
  {
    string id = (string)args["messageID"];
    string text = (string)args["messageText"];
    int index = Messages.FindIndex(x => x.ID == id);

    // No message found?
    if (index == -1) return false;

    var msg = Messages[index];
    msg.Message = text;
    Messages[index] = msg;

    if (!args.ContainsKey("messageSkipUpdate") ||
      !(args["messageSkipUpdate"] is bool) ||
      !((bool)args["messageSkipUpdate"]))
      UpdateFile();

    return true;
  }

  // ARGUMENTS IN
  //   %messageID% - The ID of the message to remove.
  //   %messageSkipUpdate% - Should file updating be skipped?
  //     Defaults to false. Use this for batch updates to the list.
  // RETURNS: True iff the message existed.
  public bool RemoveMessage()
  {
    string id = (string)args["messageID"];
    int removed = Messages.RemoveAll(x => x.ID == id);

    if (!args.ContainsKey("messageSkipUpdate") ||
      !(args["messageSkipUpdate"] is bool) ||
      !((bool)args["messageSkipUpdate"]))
      UpdateFile();

    return removed > 0;
  }

  // ARGUMENTS IN: none
  // RETURNS: true if any message was sent
  public bool SendMessage()
  {
    // Find first message that's currently enabled
    int index = Messages.FindIndex(x => x.Enabled);

    if (index == -1) return false;
    var msg = Messages[index];

    // Send the message.
    CPH.SendMessage(msg.Message);

    // Reorder the list.
    index += 1;
    Messages = Messages.Skip(index).Union(Messages.Take(index)).ToList();

    return true;
  }

  // ARGUMENTS IN:
  //   %messageID% - The ID of the specific message to get. Optional, no
  //     default. If omitted, method will get the first enabled message.
  // RETURNS: true iff any message was retrieved
  // ARGUMENTS OUT:
  //   %messageExists% - true iff any message was retrieved.
  //   %messageID% - The ID of the retrieved message.
  //   %messageText% - The text of the retrieved message.
  //   %messageEnabled% - Whether or not the retrieved message is enabled.
  public bool GetMessage()
  {
    int index = -1;

    if (args.ContainsKey("messageID")) index = Messages.FindIndex(x => x.ID == (string)args["messageID"]);
    else index = Messages.FindIndex(x => x.Enabled);

    if (index == -1)
    {
      CPH.SetArgument("messageExists", false);
      return false;
    }

    var msg = Messages[index];
    CPH.SetArgument("messageExists", true);
    CPH.SetArgument("messageID", msg.ID);
    CPH.SetArgument("messageText", msg.Message);
    CPH.SetArgument("messageEnabled", msg.Enabled);
    return true;
  }
}
