// StreamScreenshot.cs

using System;
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

  public bool Execute()
  {
    var now = DateTime.UtcNow;

    // Get the timestamp as a string
    var pattern = @"yyyy-MM-dd-HH.mm.ss.fff";
    string time = now.ToString(pattern);

    CPH.SetArgument("timestamp", time);

    CPH.ObsSendRaw("SaveSourceScreenshot",
      new JObject()
      {
        ["sourceName"] = "vcd_Elgato",
        ["imageFormat"] = "png",
        ["imageFilePath"] = @$"C:\Users\Nixill\Documents\Streaming\Screenshots\{time}.png"
      }.ToString(),
    0);

    // your main code goes here
    return true;
  }
}
