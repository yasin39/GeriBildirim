using System.Text;
using Dapper;
using GeriBildirim.Models;
using Microsoft.Data.SqlClient;

namespace GeriBildirim.Data;

/// <summary>
/// GeriBildirimler tablosuna ait tüm veritabanı işlemlerini yönetir.
/// Gönderim ekranı ve yönetim ekranı bu sınıfı paylaşır.
/// </summary>
public class GeriBildirimRepository
{
    private static SqlConnection CreateConnection()
    {
        return new SqlConnection(App.BaglantiDizesi);
    }

    /// <summary>
    /// Yeni bir geri bildirim kaydı ekler. <c>KayitTarihi</c> sunucu saatiyle
    /// (<c>GETDATE()</c>), <c>Okundu</c>/<c>Arsivlendi</c> ise tablo
    /// default'larıyla (0) atanır.
    /// </summary>
    /// <returns>Etkilenen satır sayısı (başarılıysa 1).</returns>
    public async Task<int> GeriBildirimEkleAsync(
        string kullaniciAdSoyad, string modulAdi, string sayfaAdi, string bildirimIcerigi)
    {
        const string sql = @"
            INSERT INTO GeriBildirimler
                (KullaniciAdSoyad, ModulAdi, SayfaAdi, BildirimIcerigi, KayitTarihi)
            VALUES
                (@KullaniciAdSoyad, @ModulAdi, @SayfaAdi, @BildirimIcerigi, GETDATE())";

        using var conn = CreateConnection();
        return await conn.ExecuteAsync(sql, new
        {
            KullaniciAdSoyad = kullaniciAdSoyad,
            ModulAdi = modulAdi,
            SayfaAdi = sayfaAdi,
            BildirimIcerigi = bildirimIcerigi
        });
    }

    /// <summary>
    /// Geri bildirimleri filtreleyerek döner. Modül, durum ve tarih aralığı
    /// filtreleri dinamik uygulanır; boş bırakılan kriterler sorguya katılmaz.
    /// Sıralama her zaman <c>KayitTarihi DESC</c>'tir.
    /// </summary>
    public async Task<List<GeriBildirimSatir>> GetGeriBildirimlerAsync(GeriBildirimFilter filter)
    {
        var sql = new StringBuilder(@"
            SELECT Id, KullaniciAdSoyad, ModulAdi, SayfaAdi,
                   BildirimIcerigi, KayitTarihi, Okundu, Arsivlendi
            FROM GeriBildirimler
            WHERE 1 = 1");

        var p = new DynamicParameters();

        if (!string.IsNullOrWhiteSpace(filter.ModulAdi))
        {
            sql.Append(" AND ModulAdi = @ModulAdi");
            p.Add("@ModulAdi", filter.ModulAdi);
        }

        switch (filter.Durum)
        {
            case GeriBildirimDurum.Okunmamis:
                sql.Append(" AND Okundu = 0 AND Arsivlendi = 0");
                break;
            case GeriBildirimDurum.Okunmus:
                sql.Append(" AND Okundu = 1 AND Arsivlendi = 0");
                break;
            case GeriBildirimDurum.Arsivlenmis:
                sql.Append(" AND Arsivlendi = 1");
                break;
                // Tumu: durum filtresi uygulanmaz
        }

        if (filter.IlkTarih.HasValue)
        {
            sql.Append(" AND KayitTarihi >= @IlkTarih");
            p.Add("@IlkTarih", filter.IlkTarih.Value.Date);
        }

        if (filter.SonTarih.HasValue)
        {
            // Bitiş gününü gün sonuna kadar dahil et
            sql.Append(" AND KayitTarihi < @SonTarihUst");
            p.Add("@SonTarihUst", filter.SonTarih.Value.Date.AddDays(1));
        }

        sql.Append(" ORDER BY KayitTarihi DESC");

        using var conn = CreateConnection();
        var result = await conn.QueryAsync<GeriBildirimSatir>(sql.ToString(), p);
        return result.AsList();
    }

    /// <summary>
    /// Tek bir geri bildirimi Id ile getirir. Kayıt yoksa null döner.
    /// </summary>
    public async Task<GeriBildirimSatir?> GetGeriBildirimByIdAsync(int id)
    {
        const string sql = @"
            SELECT Id, KullaniciAdSoyad, ModulAdi, SayfaAdi,
                   BildirimIcerigi, KayitTarihi, Okundu, Arsivlendi
            FROM GeriBildirimler
            WHERE Id = @Id";

        using var conn = CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<GeriBildirimSatir>(sql, new { Id = id });
    }

    /// <summary>
    /// <c>Okundu</c> bayrağını tersine çevirir ve yeni değeri döner.
    /// Kayıt yoksa null döner.
    /// </summary>
    public async Task<bool?> ToggleOkunduAsync(int id)
    {
        const string sql = @"
            UPDATE GeriBildirimler
            SET Okundu = CASE WHEN Okundu = 1 THEN 0 ELSE 1 END
            OUTPUT inserted.Okundu
            WHERE Id = @Id";

        using var conn = CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<bool?>(sql, new { Id = id });
    }

    /// <summary>
    /// <c>Arsivlendi</c> bayrağını tersine çevirir ve yeni değeri döner.
    /// Kayıt yoksa null döner.
    /// </summary>
    public async Task<bool?> ToggleArsivAsync(int id)
    {
        const string sql = @"
            UPDATE GeriBildirimler
            SET Arsivlendi = CASE WHEN Arsivlendi = 1 THEN 0 ELSE 1 END
            OUTPUT inserted.Arsivlendi
            WHERE Id = @Id";

        using var conn = CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<bool?>(sql, new { Id = id });
    }

    /// <summary>
    /// Bir geri bildirimi veritabanından kalıcı olarak siler.
    /// </summary>
    /// <returns>Etkilenen satır sayısı (silinirse 1).</returns>
    public async Task<int> GeriBildirimSilAsync(int id)
    {
        const string sql = "DELETE FROM GeriBildirimler WHERE Id = @Id";

        using var conn = CreateConnection();
        return await conn.ExecuteAsync(sql, new { Id = id });
    }
}