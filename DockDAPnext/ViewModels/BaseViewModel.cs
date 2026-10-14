using System;
using DockDAPnext.Service;
using ReactiveUI;
using System.Windows.Forms;
using MessageBox = System.Windows.Forms.MessageBox;

namespace DockDAPnext.ViewModels
{
    // TEMPORARY diagnostics: every log line below answers "why does this button do nothing?"
    // Remove this class and its call sites once the dead-command root cause is found.
    public static class DiagLog
    {
        private static readonly object Sync = new object();

        public static string FilePath => System.IO.Path.Combine(
            System.IO.Path.GetTempPath(), "dockdap-diag.log");

        // Two files: if one is locked by a reader/AV, the other still records the line.
        public static string FilePath2 => System.IO.Path.Combine(
            System.IO.Path.GetTempPath(), "dockdap-diag2.log");

        public static void Log(string message)
        {
            string line = DateTime.Now.ToString("HH:mm:ss.fff") + " [" + System.Threading.Thread.CurrentThread.ManagedThreadId + "] " + message + Environment.NewLine;
            lock (Sync)
            {
                try { System.IO.File.AppendAllText(FilePath, line); } catch (Exception) { }
                try { System.IO.File.AppendAllText(FilePath2, line); } catch (Exception) { }
            }
        }
    }

    public class BaseViewModel : ReactiveObject
    {
        protected static DubConfigAP CurrentConfig => CurrentDubFileConfigAP.CurrentDubConfigAp;

        protected static bool EnsureConfigLoaded()
        {
            if (CurrentDubFileConfigAP.CurrentDubConfigAp != null)
            {
                return true;
            }

            ShowWarning("Create or load dub.json first (the 'Create or load File' button).");
            return false;
        }

        protected static void ShowInfo(string message) => ShowMessage(message, MessageBoxIcon.Information);

        protected static bool AskYesNo(string message)
        {
            DiagLog.Log("ASK[YesNo]: " + message);
            var answer = MessageBox.Show(message, "DockDAP", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
            return answer == System.Windows.Forms.DialogResult.Yes;
        }

        protected static void ShowWarning(string message) => ShowMessage(message, MessageBoxIcon.Warning);

        private static void ShowMessage(string message, MessageBoxIcon icon)
        {
            DiagLog.Log("MSG[" + icon + "]: " + message);
            // Messages are English (AI_Rules 12); RTL options would mirror their punctuation.
            MessageBox.Show(message, "DockDAP", MessageBoxButtons.OK, icon, MessageBoxDefaultButton.Button1);
        }

        // An exception nobody observes on a ReactiveCommand is rethrown by ReactiveUI on the
        // scheduler captured at construction; inside devenv that terminates the IDE.
        protected static ReactiveCommand<TParam, TResult> Guard<TParam, TResult>(
            ReactiveCommand<TParam, TResult> command)
        {
            DiagLog.Log("GUARD subscribed: " + command.GetType().Name);
            command.ThrownExceptions.Subscribe(ex =>
            {
                DiagLog.Log("THROWN: " + ex);
                ShowWarning("Operation failed: " + ex.Message);
            });

            return command;
        }
    }
}
