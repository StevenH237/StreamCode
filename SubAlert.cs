// SubAlert.cs
// Code for big sub alert

using System;
using Newtonsoft.Json.Linq;

public class CPHInline
{
  private string CurrentScene => CPH.ObsGetCurrentScene();

  public void SetPango(string sourceName, string sourceText)
  {
    CPH.SetArgument("pangoTextName", sourceName);
    CPH.SetArgument("pangoTextValue", sourceText);
    CPH.RunAction("OBS Set Pango Text");
  }

  public bool Execute()
  {
    string tier = (string)args["tier"];

    bool isPrime = tier == "prime";
    bool isResub = args.ContainsKey("cumulative");
    string months = (isResub) ? args["cumulative"].ToString() : "1";

    // Let's make the alert noise too
    if (tier == "tier 2") CPH.PlaySound(@"C:\Users\Nixill\Documents\Streaming\Sounds\sub2.ogg");
    else if (tier == "tier 3") CPH.PlaySound(@"C:\Users\Nixill\Documents\Streaming\Sounds\sub3.ogg");
    else CPH.PlaySound(@"C:\Users\Nixill\Documents\Streaming\Sounds\sub1.ogg");

    // Set the texts first
    string whiteText = string.Format("Welcome {0}{1} foxes!",
      (isResub) ? "back to\nthe " : "to the\n",
      (tier == "tier 3") ? "gilded" :
        (tier == "tier 2") ? "shiny" : "shady"
    );

    string redText = string.Format("{0}{1}",
      args["user"],
      (isResub) ? $"\n{months} months" : ""
    );

    // Now show the first text
    SetPango("txt_AlertWhite", whiteText);
    CPH.ObsShowSource(CurrentScene, "grp_AlertPopup");

    // If prime, show the prime stinger
    if (isPrime)
    {
      CPH.Wait(2750);
      CPH.ObsHideSource(CurrentScene, "txt_AlertWhite");
      CPH.Wait(250);
      SetPango("txt_AlertWhite", "Your Bezos bucks\nare well spent.");
      CPH.ObsShowSource(CurrentScene, "txt_AlertWhite");
      CPH.Wait(1750);
    }
    else
    {
      CPH.Wait(4750);
    }

    // Now the name
    CPH.ObsHideSource(CurrentScene, "txt_AlertWhite");
    CPH.Wait(250);
    SetPango("txt_AlertRed", redText);
    CPH.ObsShowSource(CurrentScene, "txt_AlertRed");
    CPH.Wait(4750);

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
