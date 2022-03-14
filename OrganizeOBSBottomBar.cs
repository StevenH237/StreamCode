using System;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.Collections.Generic;

public class CPHInline
{
  public bool Execute()
  {
    // First, get the properties of the item that actually changed visibility.
    JObject getProps = new JObject();
    getProps["request-type"] = new JValue("GetSceneItemProperties");
    getProps["item"] = new JValue(args["obsEvent.item-name"]);

    JObject itemProps = JObject.Parse(CPH.ObsSendRaw("GetSceneItemProperties", getProps.ToString()));

    // If the object isn't part of the group "grp_BottomBar", halt.
    if (!itemProps.ContainsKey("parentGroupName")) return true;
    if ((string)itemProps["parentGroupName"] != "grp_BottomBar") return true;

    // If it is, then let's go over the bottom bar's contents.
    getProps["item"] = new JValue("grp_BottomBar");

    JObject groupProps = JObject.Parse(CPH.ObsSendRaw("GetSceneItemProperties", getProps.ToString()));

    // Sort the visible items by position.
    // Note that a position of 5 means an item wasn't visible.
    JArray arr = (JArray)groupProps["groupChildren"];

    // Also apparently we need to get the names of these items separately for some fucking reason?
    // Thank fuck CPH has a method that does exactly that.
    // I swear to Arceus if that's not an ordered list...
    List<string> names = CPH.ObsGetGroupSources(CPH.ObsGetCurrentScene(), "grp_BottomBar");

    var visibleItems = arr
      .Select((x, i) => new
      {
        Object = (JObject)x,
        Name = names[i]
      })
      .Where(x => (bool)x.Object["visible"])
      .OrderBy(x =>
      {
        double pos = (double)x.Object.SelectToken("position.x");
        return (pos == 5) ? 10000 : pos;
      });

    double width = 0;

    // Prepare a JSON object to set the items properly.
    JObject newPositionReq = new JObject();
    newPositionReq["request-type"] = "SetSceneItemProperties";
    newPositionReq["item"] = new JValue("");
    JObject newPosition = new JObject();
    newPosition["x"] = new JValue(0);
    newPositionReq["position"] = newPosition;

    // Now set the items properly.
    foreach (var o in visibleItems)
    {
      double pos = (double)o.Object.SelectToken("position.x");
      if (pos != width)
      {
        newPositionReq["item"] = new JValue(o.Name);
        newPosition["x"] = new JValue(width);

        CPH.ObsSendRaw("SetSceneItemProperties", newPositionReq.ToString());
      }

      // Update width for the newly passed item
      width += (double)o.Object["width"] + 10;
    }

    // Now prepare a JSON request to set the bounding box correctly
    JObject newBoundsReq = new JObject();
    newBoundsReq["request-type"] = "SetSceneItemProperties";
    newBoundsReq["item"] = "grp_BottomBar";
    JObject newBounds = new JObject();

    if (width > 1460) newBounds["type"] = new JValue("OBS_BOUNDS_STRETCH");
    else newBounds["type"] = new JValue("OBS_BOUNDS_MAX_ONLY");

    newBoundsReq["bounds"] = newBounds;

    CPH.ObsSendRaw("SetSceneItemProperties", newBoundsReq.ToString());

    // Repurpose newPosition above to be used to set invisible items over to 5.
    newPosition["x"] = new JValue(5);

    foreach (String name in arr
      .Select((x, i) => new
      {
        Object = (JObject)x,
        Name = names[i]
      })
      .Where(x => !(bool)x.Object["visible"])
      .Select(x => x.Name))
    {
      newPositionReq["item"] = new JValue(name);

      CPH.ObsSendRaw("SetSceneItemProperties", newPositionReq.ToString());
    }

    return true;
  }
}
