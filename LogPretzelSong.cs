// LogPretzelSong.cs
using System;
using System.IO;

public class CPHInline
{
  public bool Execute()
  {
    string nowPlaying = args["nowPlaying"];

    File.AppendAllText(@"C:\Users\Nixill\Documents\Streaming\Pretzel\pretzel.log", "");

    // your main code goes here
    return true;
  }
}
