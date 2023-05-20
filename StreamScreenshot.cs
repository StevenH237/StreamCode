// StreamScreenshot.cs

// disusername
// disurl
// dismessage
// disavatar
// disfile

using System;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;

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

  string[] Sources = new string[] {
    "vcd_GameStick",
    "gc_Primary"
  };

  string Folder = @"C:\Users\Nixill\Documents\Streaming\Screenshots\";

  public bool Execute()
  {
    // Get the timestamp as a string
    var now = DateTime.UtcNow;
    var pattern = @"yyyyMMdd-HHmmssfff";
    string time = now.ToString(pattern);

    // Set static arguments
    // CPH.SetArgument("disurl", "This is actually set in a sub-action and not open source! :D");
    CPH.SetArgument("disusername", "OBSScreenshotBot");

    // Get all the active sources, then filter that just to the sources we want screenshots from.
    var activeSources = GetActiveSources();
    var activeScreenshotSources = Sources.Intersect(activeSources);

    // Are there any?
    if (activeScreenshotSources.Any())
    {
      foreach (string source in Sources.Intersect(activeSources))
      {
        string imageFilename = $"{time}-{source}.png";
        string imagePath = $"{Folder}{imageFilename}";

        // Save the screenshot
        CPH.ObsSendRaw("SaveSourceScreenshot",
          new JObject()
          {
            ["sourceName"] = source,
            ["imageFormat"] = "png",
            ["imageFilePath"] = imagePath
          }.ToString(),
        0);

        // Prepare the discord webhook message:
        CPH.SetArgument("dismessage", imageFilename);
        CPH.SetArgument("disfile", imagePath);

        // And actually send it:
        CPH.RunActionById("0db52cd5-2375-4ad9-8f7b-5d76c2357dfa"); // Discord: Send file by webhook
      }
    }
    else
    {
      CPH.SetArgument("dismessage", "No screenshottable sources active!");
      CPH.SetArgument("disfile", null);

      CPH.RunActionById("0db52cd5-2375-4ad9-8f7b-5d76c2357dfa"); // Discord: Send file by webhook
    }

    // your main code goes here
    return true;
  }

  HashSet<string> GetActiveSources() => ActiveSourcesIn(CPH.ObsGetCurrentScene(), "GetSceneItemList");

  HashSet<string> ActiveSourcesIn(string scene, string method)
  {
    HashSet<string> output = new();
    HashSet<string> groups = new();
    HashSet<string> scenes = new();

    JObject obj = JObject.Parse(CPH.ObsSendRaw(method,
      new JObject()
      {
        ["sceneName"] = scene
      }.ToString(),
    0));

    // Check each item
    foreach (JToken itm in (JArray)(obj["sceneItems"]))
    {
      // Skip anything that's not enabled.
      if (!(bool)(itm["sceneItemEnabled"])) continue;

      string name = (string)itm["sourceName"];

      // Add that item to the output
      output.Add(name);

      // Is it a scene or group (yes or no)?
      if ((string)itm["sourceType"] == "OBS_SOURCE_TYPE_SCENE")
      {
        // Add it to the appropriate list:
        // Is it a scene or group (pick one)?
        if ((bool?)itm["isGroup"] == true)
        {
          groups.Add(name);
        }
        else
        {
          scenes.Add(name);
        }
      }
    }

    // Recurse groups and scenes:
    foreach (string grp in groups)
    {
      output.UnionWith(ActiveSourcesIn(grp, "GetGroupSceneItemList"));
    }

    foreach (string scn in scenes)
    {
      output.UnionWith(ActiveSourcesIn(scn, "GetSceneItemList"));
    }

    return output;
  }
}
