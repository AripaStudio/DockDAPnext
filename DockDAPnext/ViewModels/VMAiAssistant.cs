using DockDAPnext.Service;
using DockDAPnext.Service.InterFaces;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Threading.Tasks;

namespace DockDAPnext.ViewModels
{
    // NOT a chat: one stateless request -> one {summary, changes} JSON proposal -> the
    // user confirms -> the extension merges the changes into dub.json with its own code.
    // The AI never writes a file itself; it only proposes a partial dub.json fragment.
    // Split into: VMAiAssistant.Commands.cs (button commands) and
    // VMAiAssistant.TemplateEditor.cs (template dialog + before/after diff helper).
    public partial class VmAiAssistant : BaseViewModel
    {
        private readonly IAiService _aiService;
        private readonly IDubManagerService _dubManager;
        private readonly VMprojectDubManager _vmProjectDubManager;

        // The proposal waiting for the user's confirmation; null until one is received.
        private AiChangeProposal _pendingProposal;

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

        public ReactiveCommand<Unit, Unit> ButtonCommandAiSend { get; private set; }

        public ReactiveCommand<string, Unit> ButtonCommandAiExample { get; private set; }

        public ReactiveCommand<Unit, Unit> ButtonCommandAiEditTemplate { get; private set; }

        public ReactiveCommand<Unit, Unit> ButtonCommandAiApply { get; private set; }

        public ReactiveCommand<Unit, Unit> ButtonCommandAiCreateConfig { get; private set; }

        public ReactiveCommand<Unit, Unit> ButtonCommandAiOpenConfig { get; private set; }

        public ReactiveCommand<Unit, Unit> ButtonCommandAiAskCopilot { get; private set; }

        public VmAiAssistant(
            IAiService aiService,
            IDubManagerService dubManager,
            VMprojectDubManager vmProjectDubManager)
        {
            // SetupMiscCommands wires 'Ask Copilot', which talks to DTE (UI thread only).
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();

            this._aiService = aiService;
            this._dubManager = dubManager;
            this._vmProjectDubManager = vmProjectDubManager;

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

            SetupSendCommand();
            SetupApplyCommand();
            SetupMiscCommands();
        }

        // What will be sent: the visible template from ai.json + the current dub.json.
        private void RefreshRequestPreview()
        {
            AiRequestPreview = _aiService.BuildDubChangeRequest(AiPromptText, BuildLoadedConfigContext());
        }

        private static string BuildLoadedConfigContext()
        {
            var config = CurrentDubFileConfigAp.CurrentDubConfigAp;
            if (config == null)
            {
                return null;
            }

            return JsonConvert.SerializeObject(config, Formatting.Indented);
        }

        private async Task<string> BuildProjectContextAsync()
        {
            var path = await _dubManager.GetDubFilePathApAsync();
            var content = BuildLoadedConfigContext();

            if (!path.IsSuccess || content == null)
            {
                return content;
            }

            return "dub.json path: " + path.Value + Environment.NewLine + Environment.NewLine +
                "Current dub.json content:" + Environment.NewLine + content;
        }

        private void ClearProposal()
        {
            _pendingProposal = null;
            HasProposal = false;
        }
    }
}
