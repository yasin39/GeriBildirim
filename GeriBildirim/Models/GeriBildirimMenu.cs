using System.IO;
using System.Text.Json;

namespace GeriBildirim.Models;

/// <summary>
/// Modül ve sayfa seçeneklerinin tek kaynağı. Veriler exe'nin yanındaki
/// menu.json dosyasından okunur; değişiklik için yeniden derleme gerekmez.
/// Modül ve sayfa adları veritabanına AYNEN yazılır.
/// </summary>
public static class GeriBildirimMenu
{
    private const string MenuDosyasi = "menu.json";

    /// <summary>menu.json içindeki modüller (dosyadaki sırayla).</summary>
    public static List<MenuModulu> Moduller { get; private set; } = [];

    /// <summary>Modül adları. Gönder formundaki ve filtredeki dropdown'ların kaynağı.</summary>
    public static IEnumerable<string> ModulAdlari => Moduller.Select(m => m.Modul);

    /// <summary>
    /// menu.json dosyasını okur.
    /// </summary>
    /// <returns>Başarılıysa null, değilse kullanıcıya gösterilecek hata metni.</returns>
    public static string? Yukle()
    {
        string yol = Path.Combine(AppContext.BaseDirectory, MenuDosyasi);

        if (!File.Exists(yol))
        {
            return $"Menü dosyası bulunamadı:\n{yol}";
        }

        try
        {
            var secenekler = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var liste = JsonSerializer.Deserialize<List<MenuModulu>>(File.ReadAllText(yol), secenekler);

            if (liste is null || liste.Count == 0)
            {
                return "menu.json içinde modül tanımı yok.";
            }

            Moduller = liste;
            return null;
        }
        catch (Exception ex)
        {
            return $"Menü dosyası okunamadı:\n{ex.Message}";
        }
    }

    /// <summary>
    /// Seçilen modüle ait sayfa adlarını döner. Modül bulunamazsa boş liste verir.
    /// </summary>
    public static List<string> SayfalariGetir(string modulAdi)
    {
        var modul = Moduller.FirstOrDefault(m => m.Modul == modulAdi);
        return modul is null ? [] : modul.Sayfalar;
    }
}