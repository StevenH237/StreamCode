// ShoutoutUser.cs
using System;

public class CPHInline
{
  private string LastRaider = "NixillShadowFox";

  public bool Execute()
  {
    // Was a parameter provided?
    if (args.ContainsKey("input0")) CPH.SetArgument("targetUser", args["input0"]);

    // Otherwise just put in who last raided us.
    else CPH.SetArgument("targetUser", LastRaider);

    // your main code goes here
    return true;
  }

  public bool SetLastRaider()
  {
    LastRaider = (string)args["userName"];
    return true;
  }
}
