using System.IO;
using System.Text.Json;
using GeriBildirim.Models;

namespace GeriBildirim.Services;

/// <summary>
/// Yönetim ekranındaki filtre seçimlerini kullanıcının AppData klasöründe
/// saklar, bir sonraki açılışta geri yükler.
/// </summary>
public class FiltreAyarService
{
    private static string DosyaYolu => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "GeriBildirim",
        "filtre.json");

    /// <summary>
    /// Kaydedilmiş filtreyi döner. Dosya yoksa veya okunamazsa boş filtre verir.
    /// </summary>
    public GeriBildirimFilter Yukle()
    {
        try
        {
            if (!File.Exists(DosyaYolu))
            {
                return new GeriBildirimFilter();
            }

            var filtre = JsonSerializer.Deserialize<GeriBildirimFilter>(File.ReadAllText(DosyaYolu));
            return filtre ?? new GeriBildirimFilter();
        }
        catch (Exception)
        {
            return new GeriBildirimFilter();
        }
    }

    /// <summary>
    /// Filtreyi dosyaya yazar. Yazılamazsa sessizce geçilir; filtre
    /// hatırlanmaması uygulamanın çalışmasını etkilemez.
    /// </summary>
    public void Kaydet(GeriBildirimFilter filtre)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(DosyaYolu)!);
            File.WriteAllText(DosyaYolu, JsonSerializer.Serialize(filtre));
        }
        catch (Exception)
        {
            // Ayar yazılamadı; yok sayılır.
        }
    }
}