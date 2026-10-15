namespace GeriBildirim.Models;

/// <summary>
/// Yönetim listesindeki tek bir geri bildirim satırı. Dapper, kurucu
/// parametrelerini sütun adlarıyla eşleştirir.
/// </summary>
public record GeriBildirimSatir(
    int Id,
    string KullaniciAdSoyad,
    string ModulAdi,
    string SayfaAdi,
    string BildirimIcerigi,
    DateTime KayitTarihi,
    bool Okundu,
    bool Arsivlendi,
    string ProjeAdi)
{
    private const int OzetUzunluk = 100;

    /// <summary>Grid'de gösterilecek durum metni.</summary>
    public string DurumMetni => Arsivlendi ? "Arşiv" : Okundu ? "İşlem Yapıldı" : "Bekliyor";

    /// <summary>Bildirim metninin tek satırlık, 100 karaktere kısaltılmış hali.</summary>
    public string Ozet
    {
        get
        {
            string tekSatir = BildirimIcerigi
                .Replace("\r\n", " ")
                .Replace('\n', ' ')
                .Replace('\r', ' ');

            return tekSatir.Length > OzetUzunluk
                ? tekSatir.Substring(0, OzetUzunluk) + "..."
                : tekSatir;
        }
    }
}