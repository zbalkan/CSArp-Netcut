using System;
using System.Diagnostics;

namespace CSArp
{
    public static class DebugOutputClass
    {
        public static void Print(IView view, string output)
        {
            try
            {
                var datetimenow = DateTime.Now.ToString();
                _ = view.LogRichTextBox.Invoke(new Action(() =>
                  {
                      view.LogRichTextBox.Text += datetimenow + " : " + output + "\n";
                      view.LogRichTextBox.SelectionStart = view.LogRichTextBox.Text.Length;
                      view.LogRichTextBox.ScrollToCaret();
                  }));

                Debug.Print(output);
            }
            catch (InvalidOperationException) { }

        }
    }
}
