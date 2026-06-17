using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace TheCup_Infrastructure.Services;

public sealed class GameSchedulePdfExporter : IGameSchedulePdfExporter
{
    static GameSchedulePdfExporter()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public Task ExportAsync(
        GameSchedulePdfRequest request,
        string filePath,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.Run(() =>
        {
            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(40);
                    page.Size(PageSizes.A4);
                    page.DefaultTextStyle(style => style.FontSize(11));

                    page.Header().Column(column =>
                    {
                        column.Item().Text(request.TournamentName).Bold().FontSize(18);
                        column.Item().PaddingTop(4).Text("Games schedule").FontSize(12).FontColor(Colors.Grey.Darken1);
                    });

                    page.Content().PaddingTop(20).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(3);
                            columns.RelativeColumn(3);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Element(HeaderCellStyle).Text("Pitch");
                            header.Cell().Element(HeaderCellStyle).Text("Team A");
                            header.Cell().Element(HeaderCellStyle).Text("Team B");
                        });

                        foreach (var game in request.Games)
                        {
                            cancellationToken.ThrowIfCancellationRequested();

                            table.Cell().Element(BodyCellStyle).Text(game.PitchName);
                            table.Cell().Element(BodyCellStyle).Text(game.HomeTeamName);
                            table.Cell().Element(BodyCellStyle).Text(game.AwayTeamName);
                        }
                    });

                    page.Footer().AlignCenter().Text(text =>
                    {
                        text.Span("Page ");
                        text.CurrentPageNumber();
                        text.Span(" of ");
                        text.TotalPages();
                    });
                });
            }).GeneratePdf(filePath);
        }, cancellationToken);
    }

    private static IContainer HeaderCellStyle(IContainer container) =>
        container
            .DefaultTextStyle(style => style.SemiBold())
            .PaddingVertical(6)
            .PaddingHorizontal(4)
            .BorderBottom(1)
            .BorderColor(Colors.Grey.Lighten1);

    private static IContainer BodyCellStyle(IContainer container) =>
        container
            .PaddingVertical(6)
            .PaddingHorizontal(4)
            .BorderBottom(1)
            .BorderColor(Colors.Grey.Lighten3);
}
