using System.Text;
using Dapper;
using GeriBildirim.Models;
using Microsoft.Data.SqlClient;

namespace GeriBildirim.Data;

/// <summary>
/// Geri bildirim kayıtlarına ait veritabanı işlemleri.
/// </summary>
public class GeriBildirimRepository
{
    private static SqlConnection CreateConnection()
    {
        return new SqlConnection(App.BaglantiDizesi);
    }

    public async Task<int> GeriBildirimEkleAsync(
        int projeId,
        string kullaniciAdSoyad,
        string modulAdi,
        string sayfaAdi,
        string bildirimIcerigi)
    {
        const string sql = @"
            INSERT INTO GeriBildirimler
                (ProjeId, KullaniciAdSoyad, ModulAdi, SayfaAdi,
                 BildirimIcerigi, KayitTarihi)
            VALUES
                (@ProjeId, @KullaniciAdSoyad, @ModulAdi, @SayfaAdi,
                 @BildirimIcerigi, GETDATE())";

        using var conn = CreateConnection();
        await conn.OpenAsync();

        using var transaction = conn.BeginTransaction(
            System.Data.IsolationLevel.Serializable);

        // Kontrol ve kayıt aynı transaction'da yapılır.
        // Proje bu sırada değiştirilemez veya silinemez.
        var proje = await conn.QuerySingleOrDefaultAsync<Proje>(
            """
            SELECT Id, Ad, MenuJson, Silindi
            FROM dbo.Projeler WITH (UPDLOCK, HOLDLOCK)
            WHERE Id = @Id AND Silindi = 0
            """,
            new { Id = projeId },
            transaction);

        if (proje is null)
        {
            throw new InvalidOperationException(
                "Proje silinmiş veya bulunamıyor. Proje listesini yenileyiniz.");
        }

        bool tanimGecerli = proje.Moduller.Any(
            m => m.Modul == modulAdi && m.Sayfalar.Contains(sayfaAdi));

        if (!tanimGecerli)
        {
            throw new InvalidOperationException(
                "Projenin modül/sayfa tanımları değişmiş. " +
                "Proje listesini yenileyiniz.");
        }

        int sonuc = await conn.ExecuteAsync(sql, new
        {
            ProjeId = projeId,
            KullaniciAdSoyad = kullaniciAdSoyad,
            ModulAdi = modulAdi,
            SayfaAdi = sayfaAdi,
            BildirimIcerigi = bildirimIcerigi
        }, transaction);

        transaction.Commit();
        return sonuc;
    }

    public async Task<List<GeriBildirimSatir>> GetGeriBildirimlerAsync(
        GeriBildirimFilter filter)
    {
        // Silinmiş projelerin geçmiş bildirimleri de listelenir.
        var sql = new StringBuilder(@"
            SELECT g.Id, KullaniciAdSoyad, ModulAdi, SayfaAdi,
                   BildirimIcerigi, KayitTarihi, Okundu, Arsivlendi,
                   p.Ad AS ProjeAdi
            FROM dbo.GeriBildirimler g
            INNER JOIN dbo.Projeler p ON p.Id = g.ProjeId
            WHERE 1 = 1");

        var p = new DynamicParameters();

        if (filter.ProjeId.HasValue)
        {
            sql.Append(" AND g.ProjeId = @ProjeId");
            p.Add("@ProjeId", filter.ProjeId.Value);
        }

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
        }

        if (filter.IlkTarih.HasValue)
        {
            sql.Append(" AND KayitTarihi >= @IlkTarih");
            p.Add("@IlkTarih", filter.IlkTarih.Value.Date);
        }

        if (filter.SonTarih.HasValue)
        {
            sql.Append(" AND KayitTarihi < @SonTarihUst");
            p.Add("@SonTarihUst", filter.SonTarih.Value.Date.AddDays(1));
        }

        sql.Append(" ORDER BY KayitTarihi DESC");

        using var conn = CreateConnection();

        var result = await conn.QueryAsync<GeriBildirimSatir>(
            sql.ToString(), p);

        return result.AsList();
    }

    public async Task<GeriBildirimSatir?> GetGeriBildirimByIdAsync(int id)
    {
        const string sql = @"
            SELECT g.Id, KullaniciAdSoyad, ModulAdi, SayfaAdi,
                   BildirimIcerigi, KayitTarihi, Okundu, Arsivlendi,
                   p.Ad AS ProjeAdi
            FROM dbo.GeriBildirimler g
            INNER JOIN dbo.Projeler p ON p.Id = g.ProjeId
            WHERE g.Id = @Id";

        using var conn = CreateConnection();

        return await conn.QueryFirstOrDefaultAsync<GeriBildirimSatir>(
            sql, new { Id = id });
    }

    public async Task<bool?> ToggleOkunduAsync(int id)
    {
        const string sql = @"
            UPDATE GeriBildirimler
            SET Okundu = CASE WHEN Okundu = 1 THEN 0 ELSE 1 END
            OUTPUT inserted.Okundu
            WHERE Id = @Id";

        using var conn = CreateConnection();

        return await conn.QueryFirstOrDefaultAsync<bool?>(
            sql, new { Id = id });
    }

    public async Task<bool?> ToggleArsivAsync(int id)
    {
        const string sql = @"
            UPDATE GeriBildirimler
            SET Arsivlendi = CASE WHEN Arsivlendi = 1 THEN 0 ELSE 1 END
            OUTPUT inserted.Arsivlendi
            WHERE Id = @Id";

        using var conn = CreateConnection();

        return await conn.QueryFirstOrDefaultAsync<bool?>(
            sql, new { Id = id });
    }

    public async Task<int> GeriBildirimSilAsync(int id)
    {
        const string sql = "DELETE FROM GeriBildirimler WHERE Id = @Id";

        using var conn = CreateConnection();
        return await conn.ExecuteAsync(sql, new { Id = id });
    }
}