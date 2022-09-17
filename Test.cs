// Test.cs
using System;
using Newtonsoft.Json.Linq;

public class CPHInline
{
  public bool Execute()
  {
    //*
    JObject obj = new();
    obj["inputName"] = new JValue("txt_PretzelTitle");
    JObject stgs = new();
    obj["inputSettings"] = stgs;
    stgs["text"] = new JValue("owowo");
    obj["overlay"] = new JValue(true);

    CPH.LogInfo(obj.ToString());

    CPH.ObsSendRaw("SetInputSettings", obj.ToString());
    // */
    /*
        CPH.ObsSendRaw("SetInputSettings", @"{
      ""inputName"": ""txt_PretzelTitle"",
      ""inputSettings"": {
        ""text"": ""owwo""
      },
      ""overlay"": true
    }
    ");
    // */

    /*
    {
      "requestType": "SetInputSettings",
      "requestData": {
        "inputName": "txt_PretzelTitle",
        "inputSettings": {
          "text": "owo"
        },
        "overlay": true
      }
    }
    */
    // don't forget this
    return true;
  }
}
