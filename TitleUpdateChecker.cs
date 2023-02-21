// TitleUpdateChecker.cs
using System;
using System.Linq;
using System.IO;
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

  const string InfoFile = @"C:\Users\Nixill\Documents\Streaming\Data\stream-info.json";
  readonly TimeSpan SixHours = new(6, 0, 0);

  JObject GetStreamInfo() =>
    JObject.Parse(File.ReadAllText(InfoFile));

  void SaveStreamInfo(JObject info) =>
    File.WriteAllText(InfoFile, info.ToString());

  // On stream info update:
  public bool Execute()
  {
    var obj = GetStreamInfo();
    obj["title"] = new JValue(args["status"]);
    obj["game"] = new JValue(args["gameName"]);
    obj["last-changed"] = new JValue(DateTime.Now);
    SaveStreamInfo(obj);
    return true;
  }

  // On stream start:
  public bool CheckStreamStart()
  {
    var obj = GetStreamInfo();

    var now = DateTime.Now;

    // Check if the stream info was updated within the last six hours.
    var lastChange = obj["last-changed"].ToObject<DateTime>();
    if (now - lastChange > SixHours)
    {
      // Check if a warning was given for this within the last six hours.
      var lastWarn = obj["no-update-warning"].ToObject<DateTime>();

      if (now - lastWarn > SixHours)
      {
        // Stop the attempt to stream
        CPH.ObsStopStreaming();
        CPH.DiscordPostTextToWebhook((string)obj["webhook"], "<@106621544809115648> You didn't update stream info!");
        obj["no-update-warning"] = now;
        SaveStreamInfo(obj);
        return false;
      }
    }

    // Check if the game mismatch warning has been given recently.
    // Since that's the last warning, we can just immediately proceed if true.
    var lastGameWarn = obj["game-mismatch-warning"].ToObject<DateTime>();
    if (now - lastGameWarn <= SixHours) return true;

    // Check if the game title matches the stream title.
    string streamTitle = (string)obj["title"];
    string gameTitle = (string)obj["game"];

    if (!streamTitle.Contains(gameTitle))
    {
      // Check if the game title has any aliases
      JObject aliases = (JObject)obj["aliases"];
      if (!aliases.ContainsKey(gameTitle))
      {
        CPH.ObsStopStreaming();
        CPH.DiscordPostTextToWebhook((string)obj["webhook"], "<@106621544809115648> Game and stream title don't match!");
        obj["game-mismatch-warning"] = now;
        SaveStreamInfo(obj);
        return false;
      }

      JArray currentAliases = (JArray)aliases[gameTitle];
      if (!currentAliases.Where(x => streamTitle.Contains((string)x)).Any())
      {
        CPH.ObsStopStreaming();
        CPH.DiscordPostTextToWebhook((string)obj["webhook"], "<@106621544809115648> Game and stream title don't match!");
        obj["game-mismatch-warning"] = now;
        SaveStreamInfo(obj);
        return false;
      }
    }

    return true;
  }
}
