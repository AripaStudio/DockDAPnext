using DockDAPnext.Service;
using DockDAPnext.Service.InterFaces;
using ReactiveUI;
using System.Reactive;
using System.Threading;
using System.Threading.Tasks;

namespace DockDAPnext.ViewModels
{
    public class VMAiAssistant : BaseViewModel
    {
        private readonly IAiService aiService;

        private string _aiPromptText = string.Empty;

        public string AiPromptText
        {
            get => _aiPromptText;
            set => this.RaiseAndSetIfChanged(ref _aiPromptText, value);
        }

        private string _aiResponseText = string.Empty;

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

        public ReactiveCommand<Unit, Unit> ButtonCommand_AiSend { get; }

        public ReactiveCommand<Unit, Unit> ButtonCommand_AiCreateConfig { get; }

        public ReactiveCommand<Unit, Unit> ButtonCommand_AiOpenConfig { get; }

        public VMAiAssistant(IAiService aiService)
        {
            this.aiService = aiService;

            AiStatusText = aiService.IsConfigured(out var problem)
                ? "Ready."
                : problem;

            DiagLog.Log("VMAiAssistant: commands being created, aiStatus=" + (AiStatusText == "Ready." ? "Ready" : "not-configured"));

            ButtonCommand_AiSend = Guard(ReactiveCommand.CreateFromTask(async () =>
            {
                DiagLog.Log("ENTER ButtonCommand_AiSend");
                if (string.IsNullOrWhiteSpace(AiPromptText))
                {
                    ShowWarning("Enter a prompt first.");
                    return;
                }

                if (!aiService.IsConfigured(out var configProblem))
                {
                    ShowWarning(configProblem);
                    return;
                }

                AiStatusText = "Asking the AI...";
                var reply = await aiService.AskAsync(AiPromptText, CancellationToken.None);

                if (reply.Success)
                {
                    AiResponseText = reply.Text;
                    AiStatusText = "Answer received.";
                }
                else
                {
                    AiStatusText = "Request failed.";
                    ShowWarning(reply.Error);
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
            }));

            ButtonCommand_AiOpenConfig = Guard(ReactiveCommand.Create(() =>
            {
                DiagLog.Log("ENTER ButtonCommand_AiOpenConfig");
                // Safe no-op when the file exists, so Open always has a file to show.
                aiService.CreateTemplateConfig();
                BtnManagerAP.OpenLinkAP(aiService.ConfigFilePath);
            }));

            DiagLog.Log("VMAiAssistant: ctor done, sendNull=" + (ButtonCommand_AiSend == null)
                + " createNull=" + (ButtonCommand_AiCreateConfig == null)
                + " openNull=" + (ButtonCommand_AiOpenConfig == null));
        }
    }
}
