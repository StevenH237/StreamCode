// Code for big gift sub alert

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
    string tier = (string)args["tier"];

    // Let's make the alert noise too
    // Same noise as a regular sub of the same tier
    if (tier == "tier 1") CPH.PlaySound(@"C:\Users\Nixill\Documents\Streaming\Sounds\sub1.ogg");
    if (tier == "tier 2") CPH.PlaySound(@"C:\Users\Nixill\Documents\Streaming\Sounds\sub2.ogg");
    if (tier == "tier 3") CPH.PlaySound(@"C:\Users\Nixill\Documents\Streaming\Sounds\sub3.ogg");

    // Set the texts first
    string mainText = string.Format("Welcome (back?) to\nthe {0} foxes!",
      (tier == "tier 3") ? "gilded" :
        (tier == "tier 2") ? "shiny" : "shady"
    );

    string winnerText = (string)args["recipientUser"];

    string thanksText = $"Thanks,\n{args["user"]}!";

    // Show the first text
    SetPango("txt_AlertWhite", mainText);
    CPH.ObsShowSource(CurrentScene, "grp_AlertPopup");

    CPH.Wait(2750);

    // Now the name
    CPH.ObsHideSource(CurrentScene, "txt_AlertWhite");
    CPH.Wait(250);
    SetPango("txt_AlertRed", winnerText);
    CPH.ObsShowSource(CurrentScene, "txt_AlertRed");
    CPH.Wait(2750);

    // Now the gifter
    CPH.ObsHideSource(CurrentScene, "txt_AlertRed");
    CPH.Wait(250);
    SetPango("txt_AlertRed", thanksText);
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
