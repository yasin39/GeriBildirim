using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace GeriBildirim.Services;

/// <summary>
/// Geri bildirim listesinden PDF raporu üretir.
/// <para>
/// Sütun genişlikleri eşit dağıtılmaz: numara, modül ve sayfa adı dar tutulup
/// alanın büyük kısmı uzun olabilen bildirim metnine ayrılır.
/// </para>
/// </summary>
public class PdfService
{
    static PdfService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    /// <summary>
    /// Geri bildirim listesi için A4 yatay PDF üretir.
    /// </summary>
    /// <param name="rows">
    /// Her satır sırasıyla [numara, proje, modül, sayfa, bildirim] değerlerini içerir.
    /// </param>
    /// <returns>PDF dosyasının byte dizisi.</returns>
    public byte[] GenerateGeriBildirimReport(List<List<string>> rows)
    {
        const string title = "Geri Bildirim Listesi";
        var headers = new List<string> { "#", "Proje", "Modül", "Sayfa", "Geri Bildirim" };
        int toplamKayit = rows.Count;

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(1.5f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Header().Element(header => BuildHeader(header, title));
                page.Content().Element(content => BuildTable(content, headers, rows));
                page.Footer().Element(footer => BuildFooter(footer, toplamKayit));
            });
        }).GeneratePdf();
    }

    private static void BuildHeader(IContainer container, string title)
    {
        container
            .PaddingBottom(12)
            .Column(col =>
            {
                col.Item()
                    .Text(title)
                    .SemiBold()
                    .FontSize(14)
                    .FontColor(Color.FromHex("#2E5B9A"));

                col.Item()
                    .PaddingTop(2)
                    .Text($"Oluşturma Tarihi: {DateTime.Now:dd.MM.yyyy HH:mm}")
                    .FontSize(8)
                    .FontColor(Color.FromHex("#6B7280"));
            });
    }

    private static void BuildTable(IContainer container, List<string> headers, List<List<string>> rows)
    {
        container.Table(table =>
        {
            // Sütun genişlik oranları: # 3, Proje 10, Modül 10, Sayfa 12, Geri Bildirim 65
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(3);
                columns.RelativeColumn(10);
                columns.RelativeColumn(10);
                columns.RelativeColumn(12);
                columns.RelativeColumn(65);
            });

            table.Header(header =>
            {
                foreach (var h in headers)
                {
                    header.Cell()
                        .Background(Color.FromHex("#2E5B9A"))
                        .Padding(6)
                        .Text(h)
                        .Bold()
                        .FontSize(8)
                        .FontColor(Colors.White);
                }
            });

            bool isOdd = true;
            foreach (var row in rows)
            {
                string bgColor = isOdd ? "#FFFFFF" : "#F3F4F6";
                foreach (var cell in row)
                {
                    table.Cell()
                        .Background(Color.FromHex(bgColor))
                        .Padding(6)
                        .Text(cell)
                        .FontSize(8);
                }
                isOdd = !isOdd;
            }
        });
    }

    private static void BuildFooter(IContainer container, int toplamKayit)
    {
        container
            .PaddingTop(8)
            .Row(row =>
            {
                row.RelativeItem()
                    .Text($"Toplam Kayıt: {toplamKayit}")
                    .FontSize(8)
                    .FontColor(Color.FromHex("#6B7280"));

                row.RelativeItem()
                    .AlignRight()
                    .Text(text =>
                    {
                        text.CurrentPageNumber().FontSize(8);
                        text.Span(" / ").FontSize(8);
                        text.TotalPages().FontSize(8);
                    });
            });
    }
}