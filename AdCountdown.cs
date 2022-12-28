// AdCountdown.cs
using System;
using System.Linq;
using Newtonsoft.Json.Linq;

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

  string CurrentScene => CPH.ObsGetCurrentScene();

  public void SetPango(string sourceName, string sourceText)
  {
    CPH.SetArgument("pangoTextName", sourceName);
    CPH.SetArgument("pangoTextValue", sourceText);
    CPH.RunAction("OBS Set Pango Text");
  }

  public bool Execute()
  {
    CPH.ObsShowSource(CurrentScene, "grp_AdAlert");

    foreach (int i in Enumerable.Range(0, 60).Select(x => 60 - x))
    {
      SetPango("txt_AdCountdown", i.ToString());
      CPH.Wait(1000);
    }

    CPH.ObsHideSource(CurrentScene, "grp_AdAlert");
    CPH.TwitchRunCommercial(90);

    // your main code goes here
    return true;
  }
}
