using DockDAPnext.Service;
using DockDAPnext.Service.InterFaces;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Threading;
using System.Threading.Tasks;

namespace DockDAPnext.ViewModels
{
    // NOT a chat: one stateless request -> one {summary, changes} JSON proposal -> the
    // user confirms -> the extension merges the changes into dub.json with its own code.
    // The AI never writes a file itself; it only proposes a partial dub.json fragment.
    public class VMAiAssistant : BaseViewModel
    {
        private readonly IAiService aiService;
        private readonly IdubManagerService dubManager;
        private readonly VMprojectDubManager vmProjectDubManager;

        // The proposal waiting for the user's confirmation; null until one is received.
        private AiChangeProposal pendingProposal;

        private string _aiPromptText = string.Empty;

        public string AiPromptText
        {
            get => _aiPromptText;
            set
            {
                this.RaiseAndSetIfChanged(ref _aiPromptText, value);
                RefreshRequestPreview();
            }
        }

        private string _aiRequestPreview = string.Empty;

        // The exact text that will be sent, so nothing travels to the model unseen.
        public string AiRequestPreview
        {
            get => _aiRequestPreview;
            private set => this.RaiseAndSetIfChanged(ref _aiRequestPreview, value);
        }

        private string _aiResponseText = string.Empty;

        // The received proposal: its summary plus the pretty-printed changes fragment.
        public string AiResponseText
        {
            get => _aiResponseText;
            set => this.RaiseAndSetIfChanged(ref _aiResponseText, value);
        }

        private string _aiStatusText;

        public string AiStatusText
        {
            get => _aiStatusText;
            set => this.RaiseAndSetIfChanged(ref _aiStatusText, value);
        }

        // Drives the Apply button: only a validated proposal with changes enables it.
        private bool _hasProposal;

        public bool HasProposal
        {
            get => _hasProposal;
            private set => this.RaiseAndSetIfChanged(ref _hasProposal, value);
        }

        // Newest-first history of previous requests, persisted in ai.json; the ComboBox
        // fills the request box with one click so nothing has to be retyped.
        private ObservableCollection<string> _requestHistory = new ObservableCollection<string>();

        public ObservableCollection<string> RequestHistory
        {
            get => _requestHistory;
            private set => this.RaiseAndSetIfChanged(ref _requestHistory, value);
        }

        private string _selectedHistoryItem;

        public string SelectedHistoryItem
        {
            get => _selectedHistoryItem;
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedHistoryItem, value);
                if (!string.IsNullOrEmpty(value))
                {
                    AiPromptText = value;
                    // Reset so choosing the same entry twice works and the box keeps focus content.
                    _selectedHistoryItem = null;
                    this.RaisePropertyChanged(nameof(SelectedHistoryItem));
                }
            }
        }

        public ReactiveCommand<Unit, Unit> ButtonCommand_AiSend { get; }

        public ReactiveCommand<string, Unit> ButtonCommand_AiExample { get; }

        public ReactiveCommand<Unit, Unit> ButtonCommand_AiEditTemplate { get; }

        public ReactiveCommand<Unit, Unit> ButtonCommand_AiApply { get; }

        public ReactiveCommand<Unit, Unit> ButtonCommand_AiCreateConfig { get; }

        public ReactiveCommand<Unit, Unit> ButtonCommand_AiOpenConfig { get; }

        public VMAiAssistant(
            IAiService aiService,
            IdubManagerService dubManager,
            VMprojectDubManager vmProjectDubManager)
        {
            this.aiService = aiService;
            this.dubManager = dubManager;
            this.vmProjectDubManager = vmProjectDubManager;

            // First open: write the pre-written prompt template (ai.json) when it does
            // not exist yet; CreateTemplateConfig never overwrites an existing file.
            aiService.CreateTemplateConfig();

            AiStatusText = aiService.IsConfigured(out var problem)
                ? "Ready."
                : problem;

            // The request box starts empty on purpose: what the model always gets is the
            // template (ai.json), reached through its own button; requests come from the
            // history, the example chips, or the user's own words.
            RequestHistory = new ObservableCollection<string>(aiService.GetHistory());

            DiagLog.Log("VMAiAssistant: commands being created, aiStatus=" + AiStatusText);

            // Step 1: ask for a proposal. Nothing is written to disk here.
            ButtonCommand_AiSend = Guard(ReactiveCommand.CreateFromTask(async () =>
            {
                DiagLog.Log("ENTER ButtonCommand_AiSend");
                if (string.IsNullOrWhiteSpace(AiPromptText))
                {
                    ShowWarning("Describe the dub.json change you want first (example: 'Add unit-threaded as a dependency').");
                    return;
                }

                if (!aiService.IsConfigured(out var configProblem))
                {
                    ShowWarning(configProblem);
                    return;
                }

                if (!EnsureConfigLoaded())
                {
                    return;
                }

                // The preview must show exactly what this request sends.
                var context = await BuildProjectContextAsync();
                AiRequestPreview = aiService.BuildDubChangeRequest(AiPromptText, context);

                // Remember it (newest first, capped) so the next request can reuse it.
                if (aiService.AddToHistory(AiPromptText))
                {
                    RequestHistory = new ObservableCollection<string>(aiService.GetHistory());
                }

                ClearProposal();
                AiStatusText = "Asking the AI...";

                var proposal = await aiService.AskForDubChangesAsync(AiPromptText, context, CancellationToken.None);

                if (!proposal.Success)
                {
                    AiStatusText = "No proposal received.";
                    AiResponseText = string.IsNullOrWhiteSpace(proposal.RawText)
                        ? string.Empty
                        : proposal.RawText;
                    ShowWarning(proposal.Error);
                    return;
                }

                pendingProposal = proposal;

                if (!proposal.HasChanges)
                {
                    AiStatusText = "The AI says no change is needed.";
                    AiResponseText = proposal.Summary;
                    return;
                }

                HasProposal = true;
                AiResponseText = proposal.Summary + Environment.NewLine + Environment.NewLine +
                    proposal.Changes.ToString(Formatting.Indented);
                AiStatusText = "Proposal received. Review it below, then 'Apply to dub.json'.";
            }));

            // Step 2: the user confirmed -> merge the fragment into dub.json ourselves.
            ButtonCommand_AiApply = Guard(ReactiveCommand.CreateFromTask(async () =>
            {
                DiagLog.Log("ENTER ButtonCommand_AiApply");
                var proposal = pendingProposal;
                if (proposal == null || !proposal.HasChanges)
                {
                    ShowWarning("There is no proposal to apply. Ask for one first.");
                    return;
                }

                var dubJsonPath = await dubManager.GetDubFilePathAP();
                if (string.IsNullOrEmpty(dubJsonPath) || !System.IO.File.Exists(dubJsonPath))
                {
                    ShowWarning("dub.json was not found in this solution. Create it first with 'Create or load File'.");
                    return;
                }

                JObject root;
                try
                {
                    root = JObject.Parse(System.IO.File.ReadAllText(dubJsonPath));
                }
                catch (Exception ex)
                {
                    ShowWarning("dub.json could not be read, so nothing was changed: " + ex.Message);
                    return;
                }

                // The question must answer "exactly what changes?": one before/after line
                // per changed top-level key instead of raw JSON the user has to diff mentally.
                var diff = BuildApplyDiff(root, proposal.Changes);
                var backupPath = dubJsonPath + ".dockdap.bak";

                var confirmed = AskYesNo(
                    "Apply this change to dub.json?" + Environment.NewLine + Environment.NewLine +
                    proposal.Summary + Environment.NewLine + Environment.NewLine +
                    diff + Environment.NewLine + Environment.NewLine +
                    "A backup is saved first as " + System.IO.Path.GetFileName(backupPath) + ".");
                if (!confirmed)
                {
                    AiStatusText = "Proposal discarded; dub.json was not changed.";
                    ClearProposal();
                    return;
                }

                try
                {
                    // Backup first: the user must always have a way back.
                    System.IO.File.Copy(dubJsonPath, backupPath, true);

                    // Merge, do not overwrite: only the keys the AI proposed change,
                    // everything else in the user's dub.json survives untouched.
                    root.Merge(proposal.Changes, new JsonMergeSettings
                    {
                        MergeArrayHandling = MergeArrayHandling.Replace
                    });
                    System.IO.File.WriteAllText(dubJsonPath, root.ToString(Formatting.Indented));
                }
                catch (Exception ex)
                {
                    ShowWarning("Applying the proposal failed: " + ex.Message);
                    return;
                }

                // Refresh the in-memory config and the manager tab so the UI shows the new file.
                var refreshed = await dubManager.CreateOrLoadDubFileAP();
                if (refreshed != null)
                {
                    CurrentDubFileConfigAP.CurrentDubConfigAp = refreshed;
                    vmProjectDubManager.LoadFromConfig(refreshed);
                }

                ClearProposal();
                AiStatusText = "Proposal applied: dub.json was updated.";
                DiagLog.Log("APPLY done: " + dubJsonPath);
}));

            // Example chips: one click fills the request box with a ready-made prompt.
            ButtonCommand_AiExample = Guard(ReactiveCommand.Create<string>(text =>
            {
                DiagLog.Log("ENTER ButtonCommand_AiExample");
                if (!string.IsNullOrWhiteSpace(text))
                {
                    AiPromptText = text;
                }
            }));

            ButtonCommand_AiCreateConfig = Guard(ReactiveCommand.Create(() =>
            {
                DiagLog.Log("ENTER ButtonCommand_AiCreateConfig");
                var created = aiService.CreateTemplateConfig();
                ShowInfo(created
                    ? $"AI config created at {aiService.ConfigFilePath}. Add your apiKey, then reload this tab."
                    : $"AI config already exists: {aiService.ConfigFilePath} (not overwritten).");
                AiStatusText = aiService.IsConfigured(out var problem) ? "Ready." : problem;
                RefreshRequestPreview();
            }));

            ButtonCommand_AiOpenConfig = Guard(ReactiveCommand.Create(() =>
            {
                DiagLog.Log("ENTER ButtonCommand_AiOpenConfig");
                // Safe no-op when the file exists, so Open always has a file to show.
                aiService.CreateTemplateConfig();
                BtnManagerAP.OpenLinkAP(aiService.ConfigFilePath);
            }));

            // The template lives in ai.json, not in the request box; this button is the
            // one place to view it and change it, and an empty result is refused.
            ButtonCommand_AiEditTemplate = Guard(ReactiveCommand.Create(() =>
            {
                DiagLog.Log("ENTER ButtonCommand_AiEditTemplate");
                var edited = ShowTemplateEditor(aiService.PromptTemplate, aiService.ConfigFilePath);
                if (edited == null)
                {
                    return;
                }

                if (!aiService.SetPromptTemplate(edited))
                {
                    ShowWarning("The prompt template cannot be empty - the model needs it to answer correctly. " +
                        "The previous template was kept.");
                    return;
                }

                RefreshRequestPreview();
                AiStatusText = "Prompt template saved to ai.json.";
            }));

            DiagLog.Log("VMAiAssistant: ctor done, sendNull=" + (ButtonCommand_AiSend == null)
                + " applyNull=" + (ButtonCommand_AiApply == null)
                + " createNull=" + (ButtonCommand_AiCreateConfig == null)
                + " openNull=" + (ButtonCommand_AiOpenConfig == null));
        }

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

        // What will be sent: the visible template from ai.json + the current dub.json.
        private void RefreshRequestPreview()
        {
            AiRequestPreview = aiService.BuildDubChangeRequest(AiPromptText, BuildLoadedConfigContext());
        }

        private static string BuildLoadedConfigContext()
        {
            var config = CurrentDubFileConfigAP.CurrentDubConfigAp;
            if (config == null)
            {
                return null;
            }

            return JsonConvert.SerializeObject(config, Formatting.Indented);
        }

        private async Task<string> BuildProjectContextAsync()
        {
            var path = await dubManager.GetDubFilePathAP();
            var content = BuildLoadedConfigContext();

            if (string.IsNullOrEmpty(path) || content == null)
            {
                return content;
            }

            return "dub.json path: " + path + Environment.NewLine + Environment.NewLine +
                "Current dub.json content:" + Environment.NewLine + content;
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

        private void ClearProposal()
        {
            pendingProposal = null;
            HasProposal = false;
        }
    }
}
