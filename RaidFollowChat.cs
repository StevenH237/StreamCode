// RaidFollowChat.cs
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

  public bool Execute()
  {
    CPH.SendMessage(".followersoff", false);
    CPH.SendMessage("Followers only chat is disabled for five minutes!");
    CPH.Wait(5 * 60 * 1000);
    CPH.SendMessage(".followers 0 seconds", false);
    CPH.SendMessage("Followers only chat is back!");
    CPH.Wait(5 * 60 * 1000);
    CPH.SendMessage(".followers 5 minutes", false);
    return true;
  }
}
