// ResetMonitors.cs
using System;
using Newtonsoft.Json.Linq;

public class CPHInline
{
  public static string[] AudioInputs = new string[]
  {
    "Mic/Aux",
    "ao_Pretzel",
    "ao_Alerts",
    "vcd_Elgato"
  };

  public bool Execute()
  {
    JObject obj = new();
    obj["inputName"] = new JValue("");
    obj["monitorType"] = new JValue("");

    JValue monitorOff = new JValue("OBS_MONITORING_TYPE_NONE");
    JValue monitorOn = new JValue("OBS_MONITORING_TYPE_MONITOR_AND_OUTPUT");

    foreach (string input in AudioInputs)
    {
      obj["inputName"] = new JValue(input);

      // Turn monitoring off
      obj["monitorType"] = monitorOff;
      CPH.ObsSendRaw("SetInputAudioMonitorType", obj.ToString());

      // Turn monitoring back on
      obj["monitorType"] = monitorOn;
      CPH.ObsSendRaw("SetInputAudioMonitorType", obj.ToString());
    }
    // your main code goes here
    return true;
  }
}
