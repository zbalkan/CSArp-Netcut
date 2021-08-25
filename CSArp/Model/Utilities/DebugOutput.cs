using System;
using System.Diagnostics;
using CSArp.View;

namespace CSArp.Model.Utilities
{
    public static class DebugOutput
    {
        private static IView _view;

        public static void Init(IView view)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
        }

        public static void Print(string output)
        {
            if (_view == null)
            {
                throw new ArgumentNullException(nameof(_view));
            }

            try
            {
                var datetimenow = DateTime.Now.ToString();
                _ = _view.LogRichTextBox.Invoke(new Action(() =>
                  {
                      _view.LogRichTextBox.Text += datetimenow + " : " + output + "\n";
                      _view.LogRichTextBox.SelectionStart = _view.LogRichTextBox.Text.Length;
                      _view.LogRichTextBox.ScrollToCaret();
                  }));

                Debug.Print(output);
            }
            catch (InvalidOperationException) { }
        }
    }
}
