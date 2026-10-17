using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Threading;

namespace DockDAPnext.ViewModels
{
    public partial class VmAiAssistant
    {
        // A modal WinForms editor for the template, on its own STA thread so it works
        // no matter which thread the command runs on; Cancel/close returns null.
        private static string ShowTemplateEditor(string currentTemplate, string configPath)
        {
            string result = null;
            var thread = new Thread(() =>
            {
                using (var form = new System.Windows.Forms.Form())
                {
                    var label = new System.Windows.Forms.Label
                    {
                        Text = "This exact text is sent with every request (stored in ai.json). It must not be empty.",
                        Dock = System.Windows.Forms.DockStyle.Top,
                        Height = 34,
                        Padding = new System.Windows.Forms.Padding(8)
                    };
                    var textBox = new System.Windows.Forms.TextBox
                    {
                        Multiline = true,
                        ScrollBars = System.Windows.Forms.ScrollBars.Vertical,
                        Dock = System.Windows.Forms.DockStyle.Fill,
                        Text = currentTemplate ?? string.Empty,
                        Font = new System.Drawing.Font(System.Drawing.FontFamily.GenericMonospace, 9.5f)
                    };
                    var ok = new System.Windows.Forms.Button { Text = "Save", DialogResult = System.Windows.Forms.DialogResult.OK, Width = 100 };
                    var cancel = new System.Windows.Forms.Button { Text = "Cancel", DialogResult = System.Windows.Forms.DialogResult.Cancel, Width = 100 };
                    var panel = new System.Windows.Forms.FlowLayoutPanel
                    {
                        Dock = System.Windows.Forms.DockStyle.Bottom,
                        Height = 42,
                        FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft,
                        Padding = new System.Windows.Forms.Padding(8)
                    };
                    panel.Controls.Add(cancel);
                    panel.Controls.Add(ok);

                    form.Text = "Prompt template  -  " + configPath;
                    form.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
                    form.Width = 760;
                    form.Height = 500;
                    form.Controls.Add(textBox);
                    form.Controls.Add(panel);
                    form.Controls.Add(label);
                    form.AcceptButton = ok;
                    form.CancelButton = cancel;

                    if (form.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        result = textBox.Text;
                    }
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
            return result;
        }

        // One "before -> after" block per changed top-level key; long JSON values are
        // truncated - this is the question asked, not a replacement for the full view.
        private static string BuildApplyDiff(JObject current, JObject changes)
        {
            var lines = new List<string>();
            foreach (var property in changes.Properties())
            {
                var before = current[property.Name];
                lines.Add(property.Name + ":" + Environment.NewLine +
                    "    before: " + (before == null ? "(not set)" : TruncateText(before.ToString(Formatting.None))) + Environment.NewLine +
                    "    after:  " + TruncateText(property.Value.ToString(Formatting.None)));
            }
            return string.Join(Environment.NewLine, lines);
        }

        private static string TruncateText(string text) =>
            string.IsNullOrEmpty(text) || text.Length <= 300 ? text : text.Substring(0, 300) + "...";
    }
}
