namespace TheCup_Infrastructure.Services;

public interface IGameSchedulePdfExporter
{
    Task ExportAsync(GameSchedulePdfRequest request, string filePath, CancellationToken cancellationToken = default);
}
