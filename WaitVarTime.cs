using System;

public class CPHInline
{
  public bool Execute()
  {
    if (!args.ContainsKey("input0") || !int.TryParse((args["input0"] as string), out int timer)) timer = 60;

    timer *= 1000;

    CPH.Wait(timer);

    return true;
  }
}
