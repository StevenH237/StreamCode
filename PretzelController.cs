using System;
using System.IO;
using Newtonsoft.Json.Linq;

public class CPHInline
{
  private string Scene => CPH.ObsGetCurrentScene();

  public void SetPango(string sourceName, string sourceText)
  {
    JObject obj = new();
    obj["sourceName"] = new JValue(sourceName);
    obj["sourceType"] = new JValue("text_pango_source");
    JObject stgs = new();
    obj["sourceSettings"] = stgs;
    stgs["text"] = new JValue(sourceText);

    CPH.ObsSendRaw("SetSourceSettings", obj.ToString());
  }

  public bool Execute()
  {
    // This variable is used for the folder
    string folder = @"C:\Users\Nixill\Documents\Streaming\Pretzel\";

    // Load current pretzel json
    JObject obj = JObject.Parse(File.ReadAllText(folder + @"pretzel.json"));

    // Get strings
    JObject track = (JObject)obj["track"];
    string title = (string)track["title"];
    string artist = (string)track["artistsString"];
    JObject release = (JObject)track["release"];
    string album = (string)release["title"];
    JObject player = (JObject)obj["player"];
    bool playing = (bool)player["playing"];

    // Set individual texts
    SetPango("txt_PretzelTitle", (string)title);
    SetPango("txt_PretzelArtist", (string)artist);
    SetPango("txt_PretzelAlbum", (string)album);

    // Output paused state
    CPH.ObsSetSourceVisibility(Scene, "txt_PretzelPause", !playing);

    // your main code goes here
    return true;
  }
}
