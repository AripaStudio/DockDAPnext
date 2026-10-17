using System;
using DockDAPnext.Service;
using ReactiveUI;
using System.Windows.Forms;
using MessageBox = System.Windows.Forms.MessageBox;

namespace DockDAPnext.ViewModels
{
    public class BaseViewModel : ReactiveObject
    {
        protected static DubConfigAp CurrentConfig => CurrentDubFileConfigAp.CurrentDubConfigAp;

        protected static bool EnsureConfigLoaded()
        {
            if (CurrentDubFileConfigAp.CurrentDubConfigAp != null)
            {
                return true;
            }

            ShowWarning("Create or load dub.json first (the 'Create or load File' button).");
            return false;
        }

        protected static void ShowInfo(string message) => ShowMessage(message, MessageBoxIcon.Information);

        protected static bool AskYesNo(string message)
        {
            var answer = MessageBox.Show(message, "DockDAP", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
            return answer == System.Windows.Forms.DialogResult.Yes;
        }

        protected static void ShowWarning(string message) => ShowMessage(message, MessageBoxIcon.Warning);

        private static void ShowMessage(string message, MessageBoxIcon icon)
        {
            // Messages are English (AI_Rules 12); RTL options would mirror their punctuation.
            MessageBox.Show(message, "DockDAP", MessageBoxButtons.OK, icon, MessageBoxDefaultButton.Button1);
        }

        // An exception nobody observes on a ReactiveCommand is rethrown by ReactiveUI on the
        // scheduler captured at construction; inside devenv that terminates the IDE.
        protected static ReactiveCommand<TParam, TResult> Guard<TParam, TResult>(
            ReactiveCommand<TParam, TResult> command)
        {
            command.ThrownExceptions.Subscribe(ex =>
            {
                ShowWarning("Operation failed: " + ex.Message);
            });

            return command;
        }
    }
}
