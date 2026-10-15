using DockDAPnext.Service;
using DockDAPnext.Service.InterFaces;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ReactiveUI;
using System;
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

        public ReactiveCommand<Unit, Unit> ButtonCommand_AiSend { get; }

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

            AiStatusText = aiService.IsConfigured(out var problem)
                ? "Ready."
                : problem;

            RefreshRequestPreview();

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

                var confirmed = AskYesNo(
                    "Apply this change to dub.json?" + Environment.NewLine + Environment.NewLine +
                    proposal.Summary + Environment.NewLine + Environment.NewLine +
                    proposal.Changes.ToString(Formatting.Indented));
                if (!confirmed)
                {
                    AiStatusText = "Proposal discarded; dub.json was not changed.";
                    ClearProposal();
                    return;
                }

                var dubJsonPath = await dubManager.GetDubFilePathAP();
                if (string.IsNullOrEmpty(dubJsonPath) || !System.IO.File.Exists(dubJsonPath))
                {
                    ShowWarning("dub.json was not found in this solution. Create it first with 'Create or load File'.");
                    return;
                }

                try
                {
                    // Merge, do not overwrite: only the keys the AI proposed change,
                    // everything else in the user's dub.json survives untouched.
                    var root = JObject.Parse(System.IO.File.ReadAllText(dubJsonPath));
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

            DiagLog.Log("VMAiAssistant: ctor done, sendNull=" + (ButtonCommand_AiSend == null)
                + " applyNull=" + (ButtonCommand_AiApply == null)
                + " createNull=" + (ButtonCommand_AiCreateConfig == null)
                + " openNull=" + (ButtonCommand_AiOpenConfig == null));
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

        private void ClearProposal()
        {
            pendingProposal = null;
            HasProposal = false;
        }
    }
}
