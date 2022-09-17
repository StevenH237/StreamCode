// AdScene.cs
using System;

public class CPHInline
{
  public bool Execute()
  {
    string scene = CPH.ObsGetCurrentScene();

    if (scene == "sc_Break (Ad)")
    {
      CPH.Wait(60_000);
      scene = CPH.ObsGetCurrentScene();
      if (scene != "sc_Break (Ad)") return false;
      CPH.TwitchRunCommercial(90);
      CPH.ObsSetScene("sc_Break (No ad)");
    }

    // your main code goes here
    return true;
  }
}
