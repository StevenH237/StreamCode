using System;

public class CPHInline
{
  public bool Execute()
  {
    int rand = (int)args["randomNumber"];

    CPH.SendMessage("Flipping a coin...");

    CPH.Wait(1000);

    if (rand == 0) CPH.SendMessage("It landed on tails.");
    else CPH.SendMessage("It landed on heads.");

    // your main code goes here
    return true;
  }
}
