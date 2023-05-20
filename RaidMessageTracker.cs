// RaidMessageTracker.cs
using System;

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

  string CurrentRaidMessage = "Raid message winnerPls";

  public bool Execute()
  {
    // your main code goes here
    return true;
  }

  public bool SetRaidMessageCommand()
  {
    string msg = (string)args["rawInput"];

    if (msg == "")
    {
      // Only "!setraidmessage" typed in chat (with no actual message)
      CPH.SendMessage("Error: no raid message specified!");
    }
    else
    {
      // "!setraidmessage New raid message!" (or another message)
      // set raid message
      CurrentRaidMessage = msg;
    }
  }
}
