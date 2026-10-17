using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using EnvDTE;
using Microsoft.VisualStudio.Shell;

namespace DockDAPnext.Service
{
    // Best-effort bridge to the GitHub Copilot Chat tool window (AI tab: 'Ask Copilot').
    // Microsoft documents no command name for it and no API for its input box, so this
    // discovers the command at runtime (DTE.Commands), then tries to type the prompt
    // into the chat TextBox through the tool window's WPF visual tree. Both steps are
    // unsupported hacks and may stop working with any Copilot update - therefore the
    // prompt is ALWAYS copied to the clipboard first, so a manual paste finishes the
    // job no matter what. Call on the UI thread.
    public static class CopilotBridgeAp
    {
        public static OperationResult<string> OpenChatWithPrompt(string promptText)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                System.Windows.Forms.Clipboard.SetText(string.IsNullOrWhiteSpace(promptText) ? " " : promptText);
            }
            catch (Exception ex)
            {
                return OperationResult<string>.Fail($"The prompt could not be copied to the clipboard: {ex.Message}", ex);
            }

            var dte = Package.GetGlobalService(typeof(DTE)) as DTE;
            if (dte == null)
            {
                return OperationResult<string>.Fail(
                    "Visual Studio did not answer; open Copilot Chat from View > GitHub Copilot Chat and paste the prompt (Ctrl+V).");
            }

            var commandName = FindCopilotChatCommand(dte);
            if (commandName != null)
            {
                try
                {
                    dte.ExecuteCommand(commandName);
                }
                catch (Exception)
                {
                    // The command exists but is disabled or renamed: fall through to the manual path.
                    commandName = null;
                }
            }

            var typed = TryTypeIntoChatInput(dte, promptText);

            var opening = commandName != null
                ? $"Copilot Chat opened via '{commandName}'."
                : "Copilot Chat could not be opened automatically - open it from View > GitHub Copilot Chat.";
            var filling = typed
                ? " The prompt was typed into the chat box; write your request after the 'user text' line and send."
                : " The prompt is on the clipboard; paste it (Ctrl+V) into the chat box, write your request after the 'user text' line and send.";

            return OperationResult<string>.Ok(commandName ?? string.Empty, opening + filling);
        }

        // No published GUID/name exists for Copilot Chat, so the running IDE is asked:
        // any command containing 'copilot' is a candidate, a 'chat' one wins.
        private static string FindCopilotChatCommand(DTE dte)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            string best = null;
            string any = null;

            try
            {
                foreach (EnvDTE.Command command in dte.Commands)
                {
                    var name = command?.Name;
                    if (string.IsNullOrEmpty(name) ||
                        name.IndexOf("copilot", StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        continue;
                    }

                    if (name.IndexOf("chat", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        if (best == null || name.Length < best.Length)
                        {
                            best = name;
                        }
                    }
                    else if (any == null)
                    {
                        any = name;
                    }
                }
            }
            catch (Exception)
            {
                // Command enumeration is opportunistic; the clipboard path still works.
            }

            return best ?? any;
        }

        // Walks the tool window caption for 'Copilot', then every child HwndSource of
        // that window, and fills the first chat-like TextBox it finds. Returns false
        // when the input is not WPF (remote UI / WebView) or no window matched.
        private static bool TryTypeIntoChatInput(DTE dte, string promptText)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                foreach (EnvDTE.Window window in dte.Windows)
                {
                    var caption = window?.Caption;
                    if (string.IsNullOrEmpty(caption) ||
                        caption.IndexOf("copilot", StringComparison.OrdinalIgnoreCase) < 0 ||
                        window.HWnd == IntPtr.Zero)
                    {
                        continue;
                    }

                    if (TryFillTextBoxInChildren(window.HWnd, promptText))
                    {
                        return true;
                    }
                }
            }
            catch (Exception)
            {
                // Unsupported route: clipboard fallback is already in place.
            }

            return false;
        }

        private static bool TryFillTextBoxInChildren(IntPtr parent, string promptText)
        {
            var children = new List<IntPtr>();
            EnumChildWindows(parent, (child, _) => { children.Add(child); return true; }, IntPtr.Zero);

            foreach (var child in children)
            {
                if (HwndSource.FromHwnd(child)?.RootVisual is not Visual root)
                {
                    continue;
                }

                // The chat box is the multiline one; any empty box is the next best guess.
                TextBox best = null;
                foreach (var box in Descendants<TextBox>(root))
                {
                    if (!box.IsEnabled || !box.Focusable)
                    {
                        continue;
                    }

                    if (box.AcceptsReturn && string.IsNullOrEmpty(box.Text))
                    {
                        best = box;
                        break;
                    }

                    best = best ?? (string.IsNullOrEmpty(box.Text) ? box : best);
                }

                if (best != null)
                {
                    best.Text = promptText;
                    best.Focus();
                    return true;
                }
            }

            return false;
        }

        private static IEnumerable<T> Descendants<T>(DependencyObject root) where T : DependencyObject
        {
            var count = VisualTreeHelper.GetChildrenCount(root);
            for (var i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(root, i);
                if (child is T match)
                {
                    yield return match;
                }

                foreach (var descendant in Descendants<T>(child))
                {
                    yield return descendant;
                }
            }
        }

        [DllImport("user32.dll")]
        private static extern bool EnumChildWindows(IntPtr hWndParent, EnumChildWindowsProc lpEnumFunc, IntPtr lParam);

        private delegate bool EnumChildWindowsProc(IntPtr hWnd, IntPtr lParam);
    }
}
