// Code for gift sub bomb alert

using System;
using Newtonsoft.Json.Linq;

public class CPHInline
{
  private string CurrentScene => CPH.ObsGetCurrentScene();

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
    // Set the texts first
    string text1 = "Thanks for the bits!";
    string text2 = string.Format("{0}\n{1} bits",
      args["user"],
      args["bits"]
    );

    // Show the first text
    SetPango("txt_AlertWhite", text1);
    CPH.ObsShowSource(CurrentScene, "grp_AlertPopup");

    CPH.Wait(2750);

    // Now the name
    CPH.ObsHideSource(CurrentScene, "txt_AlertWhite");
    CPH.Wait(250);
    SetPango("txt_AlertRed", text2);
    CPH.ObsShowSource(CurrentScene, "txt_AlertRed");
    CPH.Wait(2750);

    // Reset the alert popup
    CPH.ObsHideSource(CurrentScene, "grp_AlertPopup");
    CPH.Wait(250);
    CPH.ObsHideSource(CurrentScene, "txt_AlertRed");
    CPH.ObsShowSource(CurrentScene, "txt_AlertWhite");

    // And create a delay in the queue
    CPH.Wait(750);

    // your main code goes here
    return true;
  }
}
