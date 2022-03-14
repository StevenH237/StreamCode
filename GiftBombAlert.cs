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
    // First: Do nothing on 1 or 2 sub bombs. (Does 1 even count as a bomb?)
    int count = (int)args["gifts"];
    if (count < 3) return false;

    string tier = (string)args["tier"];

    // Let's make the alert noise too
    // Same noise as a regular sub of the same tier
    if (tier == "tier 1") CPH.PlaySound(@"C:\Users\Nixill\Documents\Streaming\Sounds\sub1.ogg");
    if (tier == "tier 2") CPH.PlaySound(@"C:\Users\Nixill\Documents\Streaming\Sounds\sub2.ogg");
    if (tier == "tier 3") CPH.PlaySound(@"C:\Users\Nixill\Documents\Streaming\Sounds\sub3.ogg");

    // Set the texts first
    string whoa = "WHOA!";

    string mainText = string.Format("{0}\n just gifted {1} subs!!!",
      args["user"], count
    );

    string helloText;
    if (count <= 10)
    {
      helloText = "Say hello to the\nnew {0} foxes!";
    }
    else
    {
      helloText = "We have new\n{0} foxes!";
    }
    helloText = string.Format(helloText,
      (tier == "tier 3") ? "gilded" :
        (tier == "tier 2") ? "shiny" : "shady"
    );

    // Show the first text
    SetPango("txt_AlertWhite", whoa);
    CPH.ObsShowSource(CurrentScene, "grp_AlertPopup");

    CPH.Wait(2750);

    // Now the name
    CPH.ObsHideSource(CurrentScene, "txt_AlertWhite");
    CPH.Wait(250);
    SetPango("txt_AlertRed", mainText);
    CPH.ObsShowSource(CurrentScene, "txt_AlertRed");
    CPH.Wait(2750);

    // Now the final text
    CPH.ObsHideSource(CurrentScene, "txt_AlertRed");
    CPH.Wait(250);
    SetPango("txt_AlertWhite", helloText);
    CPH.ObsShowSource(CurrentScene, "txt_AlertWhite");
    CPH.Wait(2750);

    // Reset the alert popup
    CPH.ObsHideSource(CurrentScene, "grp_AlertPopup");

    // And create a delay in the queue
    CPH.Wait(1000);

    // your main code goes here
    return true;
  }
}
