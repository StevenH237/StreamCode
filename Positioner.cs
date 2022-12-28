// Positioner.cs
// This exists because somehow my things keep getting thrown out of place.
// I don't know how to stop it, but I can at least reset it! :D

using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

public class CPHInline
{
  private Dictionary<string, (int X, int Y)> Positions = new Dictionary<string, (int, int)>()
  {
    ["sc_Starting"] = (960, -5),
    ["sc_BRB"] = (960, -5),
    ["sc_Break"] = (960, -5),
    ["sc_Loading raid"] = (960, -5),
    ["sc_Raid target"] = (960, -5),
    ["sc_No raid"] = (960, -5),
    ["sc_Technical difficulties"] = (960, -5),
    ["st_Title Screens"] = (960, -5),
    ["sc_Camera"] = (1690, 1085),
    ["sc_16:9 Gaming"] = (1690, 1085),
    ["sc_GBA Gaming"] = (1690, 1085)
  };

  private string[] MovedObjects = new string[] {
    "grp_AlertPopup",
    "grp_SmallAlertPopup"
  };

  public bool Execute()
  {
    string scene = CPH.ObsGetCurrentScene();

    if (Positions.ContainsKey(scene))
    {
      (int X, int Y) pos = Positions[scene];

      var listResponse = ((IEnumerable<JToken>)(JObject
        .Parse(CPH.ObsSendRaw("GetSceneItemList", new JObject() { ["sceneName"] = new JValue(scene) }.ToString()))["sceneItems"]))
        .Select(x => ((JObject)x))
        .Where(x => MovedObjects.Contains((string)x["sourceName"]))
        .Select(x => (int)x["sceneItemId"]);

      JObject obj = new()
      {
        ["sceneName"] = new JValue(scene),
        ["sceneItemTransform"] = new JObject()
        {
          ["positionX"] = new JValue(pos.X),
          ["positionY"] = new JValue(pos.Y)
        }
      };

      foreach (int id in listResponse)
      {
        obj["sceneItemId"] = id;
        CPH.ObsSendRaw("SetSceneItemTransform", obj.ToString());
      }
    }

    return true;
  }
}
