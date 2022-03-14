using System;
using System.Collections.Generic;

public class CPHInline
{
  private Dictionary<string, int> GiftTracker;

  public void Init()
  {
    GiftTracker = new Dictionary<string, int>();
  }

  public bool AddGiftBomb()
  {
    string user = (string)args["userName"];
    int gifts = (int)args["gifts"];

    // Do nothing if it's 1 or 2 subs (lack indicates large alerts)
    if (gifts < 3) return true;

    // If it's 3–10 set positive (indicates small alerts)
    if (gifts < 11)
    {
      GiftTracker[user] = gifts;
      return true;
    }

    // Otherwise set negative (indicates no alerts)
    GiftTracker[user] = -gifts;
    return true;
  }

  public bool DispatchGiftEvent()
  {
    string user = (string)args["userName"];
    bool tracked = GiftTracker.ContainsKey(user);

    // A lack of count means large events
    if (!tracked)
    {
      CPH.SetArgument("giftEvent", "large");
      return true;
    }

    int count = GiftTracker[user];

    // A positive count means small events
    if (count > 0)
    {
      GiftTracker[user] = count - 1;
      CPH.SetArgument("giftEvent", "small");
      return true;
    }

    // A negative count means no events
    else if (count < 0)
    {
      GiftTracker[user] = count + 1;
      CPH.SetArgument("giftEvent", "none");
      return true;
    }

    // A count of 0 means large events
    else
    {
      CPH.SetArgument("giftEvent", "large");
      return true;
    }
  }
}