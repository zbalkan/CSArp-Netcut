using System;
using System.Diagnostics;
using CSArp.View;

namespace CSArp.Model.Utilities
{
    public static class DebugOutput
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
