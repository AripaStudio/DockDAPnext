using DockDAPnext.Service;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ReactiveUI;
using System;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Threading;

namespace DockDAPnext.ViewModels
{
    public partial class VmAiAssistant
    {
        // Step 1: ask for a proposal. Nothing is written to disk here.
        private void SetupSendCommand()
        {
            ButtonCommandAiSend = Guard(ReactiveCommand.CreateFromTask(async () =>
            {
                if (string.IsNullOrWhiteSpace(AiPromptText))
                {
                    ShowWarning("Describe the dub.json change you want first (example: 'Add unit-threaded as a dependency').");
                    return;
                }

                if (!_aiService.IsConfigured(out var configProblem))
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
                AiRequestPreview = _aiService.BuildDubChangeRequest(AiPromptText, context);

                // Remember it (newest first, capped) so the next request can reuse it.
                if (_aiService.AddToHistory(AiPromptText))
                {
                    RequestHistory = new ObservableCollection<string>(_aiService.GetHistory());
                }

                ClearProposal();
                AiStatusText = "Asking the AI...";

                var proposal = await _aiService.AskForDubChangesAsync(AiPromptText, context, CancellationToken.None);

                if (!proposal.Success)
                {
                    AiStatusText = "No proposal received.";
                    AiResponseText = string.IsNullOrWhiteSpace(proposal.RawText)
                        ? string.Empty
                        : proposal.RawText;
                    ShowWarning(proposal.Error);
                    return;
                }

                _pendingProposal = proposal;

                if (!proposal.HasChanges)
                {
                    AiStatusText = "The AI says no change is needed.";
                    AiResponseText = proposal.Summary;
                    return;
                }

                HasProposal = true;
                AiResponseText = proposal.Summary + Environment.NewLine + Environment.NewLine +
                    proposal.Changes.ToString(Formatting.Indented);
                AiStatusText = "Proposal received. Review it below, then 'Apply to dub.json'."
;
            }));
        }

        // Step 2: the user confirmed -> merge the fragment into dub.json ourselves.
        private void SetupApplyCommand()
        {
            ButtonCommandAiApply = Guard(ReactiveCommand.CreateFromTask(async () =>
            {
                var proposal = _pendingProposal;
                if (proposal == null || !proposal.HasChanges)
                {
                    ShowWarning("There is no proposal to apply. Ask for one first.");
                    return;
                }

                var found = await _dubManager.GetDubFilePathApAsync();
                if (!found.IsSuccess || string.IsNullOrEmpty(found.Value) || !System.IO.File.Exists(found.Value))
                {
                    ShowWarning("dub.json was not found in this solution. Create it first with 'Create or load File'.");
                    return;
                }

                var dubJsonPath = found.Value;

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
                var refreshed = await _dubManager.CreateOrLoadDubFileApAsync();
                if (refreshed.IsSuccess)
                {
                    CurrentDubFileConfigAp.CurrentDubConfigAp = refreshed.Value;
                    _vmProjectDubManager.LoadFromConfig(refreshed.Value);
                    AiStatusText = "Proposal applied: dub.json was updated.";
                }
                else
                {
                    AiStatusText = "Proposal applied, but reloading dub.json failed: " + refreshed.Message;
                }

                ClearProposal();
            }));
        }

        // Example chips (one click fills the request box) and the two config buttons.
        private void SetupMiscCommands()
        {
            ButtonCommandAiExample = Guard(ReactiveCommand.Create<string>(text =>
            {
                if (!string.IsNullOrWhiteSpace(text))
                {
                    AiPromptText = text;
                }
            }));

            ButtonCommandAiCreateConfig = Guard(ReactiveCommand.Create(() =>
            {
                var created = _aiService.CreateTemplateConfig();
                ShowInfo(created
                    ? $"AI config created at {_aiService.ConfigFilePath}. Add your apiKey, then reload this tab."
                    : $"AI config already exists: {_aiService.ConfigFilePath} (not overwritten).");
                AiStatusText = _aiService.IsConfigured(out var problem) ? "Ready." : problem;
                RefreshRequestPreview();
            }));

            ButtonCommandAiOpenConfig = Guard(ReactiveCommand.Create(() =>
            {
                // Safe no-op when the file exists, so Open always has a file to show.
                _aiService.CreateTemplateConfig();
                BtnManagerAp.OpenLinkAp(_aiService.ConfigFilePath);
            }));

            // 'Ask Copilot' does not call any AI itself: it opens the GitHub Copilot
            // chat tab with the prompt template (rules) + a 'user text' marker ready to
            // paste, so the user writes their own request there and sends it there.
            ButtonCommandAiAskCopilot = Guard(ReactiveCommand.CreateFromTask(async () =>
            {
                var rules = _aiService.PromptTemplate;
                if (string.IsNullOrWhiteSpace(rules))
                {
                    // First use: make sure ai.json (and its template) exist, then retry.
                    _aiService.CreateTemplateConfig();
                    rules = _aiService.PromptTemplate;
                }

                if (string.IsNullOrWhiteSpace(rules))
                {
                    ShowWarning("The prompt template is empty. Create the AI config first, then try again.");
                    return;
                }

                // rules text = the fixed template; 'user text' = where the user's own
                // request belongs when they paste it into the Copilot chat box.
                var promptText = rules + Environment.NewLine + Environment.NewLine + "user text" + Environment.NewLine;
                await Microsoft.VisualStudio.Shell.ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                var result = CopilotBridgeAp.OpenChatWithPrompt(promptText);
                AiStatusText = result.IsSuccess ? result.Message : "Ask Copilot failed: " + result.Message;
                
            }));

            // The template lives in ai.json, not in the request box; this button is the
            // one place to view it and change it, and an empty result is refused.
            ButtonCommandAiEditTemplate = Guard(ReactiveCommand.Create(() =>
            {
                var edited = ShowTemplateEditor(_aiService.PromptTemplate, _aiService.ConfigFilePath);
                if (edited == null)
                {
                    return;
                }

                if (!_aiService.SetPromptTemplate(edited))
                {
                    ShowWarning("The prompt template cannot be empty - the model needs it to answer correctly. " +
                        "The previous template was kept.");
                    return;
                }

                RefreshRequestPreview();
                AiStatusText = "Prompt template saved to ai.json.";
            }));
        }
    }
}
