// StreamMarkerTimestamp.cs
using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;

public class CPHInline
{
  // easy to turn off all at once
  void Print(string text)
  {
    CPH.LogInfo(text);
  }

  // public void Init()
  // {
  //
  // }

  // public void Dispose()
  // {
  //
  // }

  public bool Execute()
  {
    Print("Stream marker pressed!");

    var recResponse = JObject.Parse(CPH.ObsSendRaw("GetRecordStatus", "{}", 0));

    // First check if it's active before we do anything else
    bool active = (bool)(recResponse["outputActive"]);

    if (active)
    {
      Print("Recording active...");

      // Let's get the current recording filename!
      // This is gonna be tricky because OBS doesn't let us do that except
      // as the recording ends - so instead we'll have to use the
      // recording directory and just find the most recently modified
      // file.
      var recDirResponse = JObject.Parse(CPH.ObsSendRaw("GetRecordDirectory", "{}", 0));

      var dir = (string)(recDirResponse["recordDirectory"]);
      Print($"Directory: {dir}");

      var dirInfo = new DirectoryInfo(dir);

      var file = dirInfo.GetFiles()
          .Where(f => !f.Name.EndsWith(".markers.txt"))
          .OrderByDescending(f => f.LastWriteTimeUtc)
          .First();
      string filename = file.Name;

      Print($"Filename: {filename}");

      // Now translate it to a marker filename:
      string markerFilename = dir + "\\" + filename.Substring(0, filename.LastIndexOf('.')) + ".markers.txt";

      Print($"Marker filename: {markerFilename}");

      // Get the current timestamp to write to marker file:
      var time = (string)(recResponse["outputTimecode"]);

      // And finally, actually write it!
      File.AppendAllText(markerFilename, time + "\n", System.Text.Encoding.UTF8);

      Print($"Written!");
    }
    else
    {
      Print("Recording not active.");
    }

    // Also we should actually output a stream marker if we're live.
    if (CPH.ObsIsStreaming(0))
    {
      Print("Stream active!");
      CPH.CreateStreamMarker("Created by C# code!");
    }
    else
    {
      Print("Stream not active.");
    }

    // your main code goes here
    return true;
  }
}
