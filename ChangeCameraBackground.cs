// ChangeCameraBackground.cs
using System;
using System.IO;

public class CPHInline
{
  private string ImageFolder = @"C:\Users\Nixill\Documents\Streaming\Images\Backgrounds\Camera\Finished\";
  private string CurrentScene => CPH.ObsGetCurrentScene();

  public bool Execute()
  {
    string input = ((string)args["rawInput"]).ToLower();
    string reward = (string)args["rewardId"];
    string redemption = (string)args["redemptionId"];

    if (input.StartsWith("!"))
    {
      CPH.TwitchRedemptionCancel(reward, redemption);
      return false;
    }

    if (input.StartsWith("\"") && input.EndsWith("\""))
    {
      input = input.Substring(1, input.Length - 2).Trim();
    }

    string file = ImageFolder + input;

    if (File.Exists(file + ".jpg"))
    {
      CPH.ObsSetImageSourceFile(CurrentScene, "img_CameraBG", file + ".jpg");
      CPH.ObsSetImageSourceFile(CurrentScene, "img_CameraFG", file + ".png");

      CPH.TwitchRedemptionFulfill(reward, redemption);
    }
    else
    {
      CPH.SendMessage(input + " is not a valid file.");
      CPH.TwitchRedemptionCancel(reward, redemption);
    }

    // your main code goes here
    return true;
  }
}
