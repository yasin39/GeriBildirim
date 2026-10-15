using System.Text.Json;

namespace GeriBildirim.Models;

public static class ProjeTanimi
{
    public static (string Ad, string MenuJson) Hazirla(string ad, string satirlar)
    {
        ad = ad.Trim();
        if (ad.Length is 0 or > 150)
            throw new ArgumentException("Proje adı 1–150 karakter olmalıdır.");

        var moduller = new List<MenuModulu>();
        foreach (var satir in satirlar.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            if (string.IsNullOrWhiteSpace(satir)) continue;

            var alanlar = satir.Split('|');
            if (alanlar.Length != 2 || alanlar.Any(a => string.IsNullOrWhiteSpace(a)))
                throw new ArgumentException(
                    "Her satıra Modül | Sayfa biçiminde bir tanım yazınız.");

            var modul = alanlar[0].Trim();
            var sayfa = alanlar[1].Trim();

            if (modul.Length > 100 || sayfa.Length > 100)
                throw new ArgumentException(
                    "Modül ve sayfa adları en fazla 100 karakter olabilir.");

            var kayit = moduller.FirstOrDefault(m =>
                string.Equals(m.Modul, modul, StringComparison.OrdinalIgnoreCase));

            if (kayit is null)
            {
                kayit = new MenuModulu { Modul = modul };
                moduller.Add(kayit);
            }

            if (!kayit.Sayfalar.Contains(sayfa, StringComparer.OrdinalIgnoreCase))
                kayit.Sayfalar.Add(sayfa);
        }

        if (moduller.Count == 0)
            throw new ArgumentException("En az bir modül ve sayfa tanımlayınız.");

        return (ad, JsonSerializer.Serialize(moduller));
    }
}