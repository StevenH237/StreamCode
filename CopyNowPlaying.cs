using System;
using System.Threading;
using System.Windows;

public class CPHInline
{
  public bool Execute()
  {
    // Get the song link
    string songText = args["nowPlaying"].ToString();

    // Get just the link
    string songLink = songText.Substring(songText.LastIndexOf(' ') + 1);

    // Copy the link with angle brackets
    Thread thread = new Thread(() => Clipboard.SetText($"<{songLink}>"));
    thread.SetApartmentState(ApartmentState.STA); //Set the thread to STA
    thread.Start();
    thread.Join();

    // required
    return true;
  }
}
