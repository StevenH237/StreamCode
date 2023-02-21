// Test.cs
using System;
using System.IO;
using Newtonsoft.Json.Linq;

public class CPHInline
{
  public bool Execute()
  {
    File.WriteAllText(@"C:\Users\Nixill\Documents\Streaming\Data\last-stream.json", new JObject()
    {
      ["title"] = "Part 1 | Pokémon Scarlet",
      ["last-changed"] = new DateTime(2023, 1, 2, 19, 0, 0)
    }.ToString());

    // don't forget this
    return true;
  }
}
