// SetPango.cs
using System;
using Newtonsoft.Json.Linq;

public class CPHInline
{
  public void SetPango(string inputName, string inputText)
  {
    JObject obj = new();
    obj["inputName"] = new JValue(inputName);
    obj["inputKind"] = new JValue("text_pango_source");
    JObject stgs = new();
    obj["inputSettings"] = stgs;
    stgs["text"] = new JValue(inputText);

    CPH.ObsSendRaw("SetInputSettings", obj.ToString());
  }

  public bool Execute()
  {
    SetPango(args["pangoTextName"].ToString(), args["pangoTextValue"].ToString());

    // your main code goes here
    return true;
  }

  /*
  Code to copy to other methods:

  public void SetPango(string sourceName, string sourceText)
  {
    CPH.SetArgument("pangoTextName", sourceName);
    CPH.SetArgument("pangoTextValue", sourceText);
    CPH.RunAction("OBS Set Pango Text");
  }
  */
}
