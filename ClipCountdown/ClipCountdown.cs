// ClipCountdown.cs
using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;

public class CPHInline
{
  JObject Data;
  JObject Clip;
  const string Folder = @"C:\Users\Nixill\Documents\Streaming\ClipCountdown\2022\";
  string WebhookUrl;

  string LastScene = "";

  public void SetPangoText(string sourceName, string sourceText)
  {
    CPH.SetArgument("pangoTextName", sourceName);
    CPH.SetArgument("pangoTextValue", sourceText);
    CPH.RunAction("OBS Set Pango Text");
  }

  public void Discord(string message)
  {
    CPH.DiscordPostTextToWebhook(WebhookUrl, message);
  }

  public void Init()
  {
    WebhookUrl = File.ReadAllText(@"C:\Users\Nixill\Documents\Streaming\Code\secrets\clip-countdown-webhook");

    Data = JObject.Parse(File.ReadAllText(Folder + "data.json"));
    int which = (int)Data["next"];
    Clip = (JObject)Data["clips"][which];

    Discord("Clip countdown tracker initiated!");
    Discord($"First clip: {Clip["cue"]}");
  }

  public void Dispose()
  {
    Discord("Clip countdown tracker shutting down.");
    File.WriteAllText(Folder + "data.json", Data.ToString());
  }

  public bool Execute()
  {
    // your main code goes here
    return true;
  }

  public bool PlayNext()
  {
    LastScene = CPH.ObsGetCurrentScene();
    CPH.ObsSetScene("sc_Clipshow");

    if (Clip.ContainsKey("credit"))
    {
      string credit = (string)Clip["credit"];
      CPH.SendMessage($"This clip was from {credit}'s stream! https://twitch.tv/{credit.ToLower()}");
    }

    return true;
  }

  public bool OnEndOfPlay()
  {
    if (LastScene != "")
    {
      CPH.ObsSetScene(LastScene);
      LastScene = "";
      CPH.Wait(500);
    }
    NextClip();
    return true;
  }

  public bool SkipClip()
  {
    Discord("Skipping clip");
    NextClip();
    return true;
  }

  public bool RevertClip()
  {
    Discord("Going back a clip");

    int nextClip = (int)Data["next"] - 1;
    if (nextClip == -1)
    {
      Discord("That's the first clip! Wrapping back to the end...");
      nextClip += ((JArray)Data["clips"]).Count;
    }
    SetNextClip(nextClip);
    return true;
  }

  public void SetNextClip(int which)
  {
    Data["next"] = which;
    Clip = (JObject)Data["clips"][which];

    Discord($"Coming up: {Clip["cue"]}");
    CPH.ObsSetMediaSourceFile("sc_Clipshow", "med_Clip", Folder + (string)Clip["file"]);
    SetPangoText("txt_ClipTitle", (string)Clip["title"]);
  }

  public bool NextClip()
  {
    int nextClip = (int)Data["next"] + 1;
    if (nextClip == ((JArray)Data["clips"]).Count)
    {
      Discord("That's all the clips! Wrapping back to the start...");
      nextClip = 0;
    }
    SetNextClip(nextClip);
    return true;
  }
}
