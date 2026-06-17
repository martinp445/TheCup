using TheCup_Application.Ports;

namespace TheCup_Application.Services;

internal sealed class NullFileSaveDialogService : IFileSaveDialogService
{
    public string? PromptSavePdf(string suggestedFileName) => null;
}
