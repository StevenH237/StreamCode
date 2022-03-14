using System;
using Newtonsoft.Json.Linq;

public class CPHInline
{
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
    SetPango(args["pangoTextName"], args["pangoTextValue"]);

    // your main code goes here
    return true;
  }
}
