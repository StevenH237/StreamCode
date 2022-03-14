using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class CPHInline
{
  private const string Folder = @"C:\Users\Nixill\Documents\Streaming\Images\Slideshow\";
  private List<string> Queue = new();
  public List<Func<bool>> Actions = new();

  private string Scene => CPH.ObsGetCurrentScene();

  public void Init()
  {
    Queue = Directory.GetFiles(Folder, "*.JPG")
      .OrderBy(x => CPH.NextDouble())
      .Where((x, i) => (i % 2 == 0))
      .ToList();

    Actions.Add(SetImage1);
    Actions.Add(HideImage2);
    Actions.Add(SetImage2);
    Actions.Add(HideImage1);
  }

  public void Enqueue()
  {
    // Get the current files in the folder
    string[] files = Directory.GetFiles(Folder, "*.JPG");

    // Remove invalid list items
    Queue = Queue.Intersect(files).ToList();

    // How many do we need to have in the queue?
    // Round half up.
    int halfSize = (files.Length + 1) / 2;

    if (halfSize > 0)
      Queue.AddRange(
        Directory.GetFiles(Folder, "*.JPG")
          .Except(Queue)
          .OrderBy(x => CPH.NextDouble())
          .Take(halfSize)
        );
  }

	public bool Execute()
	{
    Func<bool> next = Actions[0];
    Actions.RemoveAt(0);
    Actions.Add(next);

		// your main code goes here
		return next();
	}

  public bool SetImage1() {
    // Get the next image
    string next = Queue[0];
    Queue.RemoveAt(0);
    Enqueue();

    // Set the next image in 1A and 1B
    CPH.ObsSetImageSourceFile(Scene, "img_Slideshow1A", next);
    CPH.ObsSetImageSourceFile(Scene, "img_Slideshow1B", next);

    // Set 1A visible
    CPH.ObsShowSource(Scene, "img_Slideshow1A");

    // compat
    return true;
  }

  public bool HideImage2() {
    // Set 2 invisible
    CPH.ObsHideSource(Scene, "img_Slideshow2");

    // Wait
    CPH.Wait(1000);

    // Switch which 1 is visible
    CPH.ObsShowSource(Scene, "img_Slideshow1B");
    CPH.ObsHideSource(Scene, "img_Slideshow1A");

    // compat
    return true;
  }

  public bool SetImage2() {
    // Get the next image
    string next = Queue[0];
    Queue.RemoveAt(0);
    Enqueue();

    // Set the next image
    CPH.ObsSetImageSourceFile(Scene, "img_Slideshow2", next);

    // Set 1A visible
    CPH.ObsShowSource(Scene, "img_Slideshow2");

    // compat
    return true;
  }

  public bool HideImage1() {
    // Set 2 invisible
    CPH.ObsHideSource(Scene, "img_Slideshow1B");

    // compat
    return true;
  }
}
