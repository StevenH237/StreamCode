// PretzelController.cs
using System;
using System.IO;
using Newtonsoft.Json.Linq;

public class CPHInline
{
  private string Scene => CPH.ObsGetCurrentScene();

  public void SetPango(string sourceName, string sourceText)
  {
    CPH.SetArgument("pangoTextName", sourceName);
    CPH.SetArgument("pangoTextValue", sourceText);
    CPH.RunAction("OBS Set Pango Text");
  }

  public bool Execute()
  {
    // WHYYYYYYYYY
    CPH.LogInfo("Pretzel splitter go!");

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

    // Log strings because apparently things aren't working 🙃
    CPH.LogInfo(title);
    CPH.LogInfo(artist);
    CPH.LogInfo(album);
    CPH.LogInfo(playing.ToString());

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
