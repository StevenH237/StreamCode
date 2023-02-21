// NewYearTick.cs
using System;

public class CPHInline
{
  bool Run = true;

  public void SetPango(string sourceName, string sourceText)
  {
    CPH.SetArgument("pangoTextName", sourceName);
    CPH.SetArgument("pangoTextValue", sourceText);
    CPH.RunAction("OBS Set Pango Text");
  }

  public void Dispose()
  {
    Run = false;
  }

  public bool Execute()
  {
    Run = true;

    DateTime midnightUTC = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    DateTime midnightEST = new DateTime(2023, 1, 1, 5, 0, 0, DateTimeKind.Utc);
    DateTime now = DateTime.UtcNow;

    while (Run)
    {
      now = DateTime.UtcNow;

      var timeLeftUTC = midnightUTC - now;
      SetPango("txt_CountdownUTC", Math.Max(0, Math.Ceiling(timeLeftUTC.TotalSeconds)).ToString());

      var timeLeftEST = midnightEST - now;
      if (timeLeftEST > TimeSpan.Zero)
      {
        SetPango("txt_CountdownEST", Math.Max(0, Math.Ceiling(timeLeftEST.TotalSeconds)).ToString());
      }
      else
      {
        SetPango("txt_CountdownEST", "Happy new year!");
        break;
      }

      CPH.Wait(100);
    }

    // your main code goes here
    return true;
  }

  public bool Cease()
  {
    Run = false;
    return true;
  }
}
