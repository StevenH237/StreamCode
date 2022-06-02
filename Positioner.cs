// This exists because somehow my things keep getting thrown out of place.
// I don't know how to stop it, but I can at least reset it! :D

using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

public class CPHInline
{
  private Dictionary<string, (int X, int Y)> Positions = new Dictionary<string, (int, int)>()
  {
    ["sc_Starting"] = (960, -5),
    ["sc_BRB"] = (960, -5),
    ["sc_Break (No ad)"] = (960, -5),
    ["sc_Break (Ad)"] = (960, -5),
    ["sc_Loading raid"] = (960, -5),
    ["sc_Raid target"] = (960, -5),
    ["sc_No raid"] = (960, -5),
    ["sc_Technical difficulties"] = (960, -5),
    ["sc_Camera"] = (1690, 1085),
    ["sc_16:9 Gaming"] = (1690, 1085)
  };

  public bool Execute()
  {
    string scene = CPH.ObsGetCurrentScene();

    if (Positions.ContainsKey(scene))
    {
      (int X, int Y) pos = Positions[scene];

      JObject obj = new()
      {
        ["item"] = new JValue("grp_AlertPopup"),
        ["scene-name"] = new JValue(scene),
        ["position"] = new JObject()
        {
          ["x"] = new JValue(pos.X),
          ["y"] = new JValue(pos.Y)
        }
      };

      CPH.ObsSendRaw("SetSceneItemProperties", obj.ToString());

      obj["item"] = new JValue("grp_SmallAlertPopup");

      CPH.ObsSendRaw("SetSceneItemProperties", obj.ToString());
    }

    return true;
  }
}
