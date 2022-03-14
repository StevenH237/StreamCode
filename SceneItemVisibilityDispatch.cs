using System;

public class CPHInline
{
  public bool Execute()
  {
    string itemName = (string)args["obsEvent.item-name"];

    if (itemName == "mirr_grp_Discord"
      || itemName == "mirr_grp_Pretzel"
      || itemName == "mirr_grp_Spectralizer")
      return CPH.RunAction("Reorganize OBS Bottom Bar");

    // your main code goes here
    return true;
  }
}
