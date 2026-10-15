using Dapper;
using GeriBildirim.Models;
using Microsoft.Data.SqlClient;

namespace GeriBildirim.Data;

public class ProjeRepository
{
    public async Task<List<Proje>> GetProjelerAsync(bool silinenleriDahilEt = false)
    {
        const string sql = """
            SELECT Id, Ad, MenuJson, Silindi
            FROM dbo.Projeler
            WHERE Silindi = 0 OR @SilinenleriDahilEt = 1
            ORDER BY Silindi, Ad
            """;

        using var conn = new SqlConnection(App.BaglantiDizesi);

        return (await conn.QueryAsync<Proje>(
            sql,
            new { SilinenleriDahilEt = silinenleriDahilEt })).AsList();
    }

    public async Task<int> EkleAsync(string ad, string tanimlar)
    {
        var tanim = ProjeTanimi.Hazirla(ad, tanimlar);

        using var conn = new SqlConnection(App.BaglantiDizesi);

        try
        {
            return await conn.QuerySingleAsync<int>(
                """
                INSERT INTO dbo.Projeler (Ad, MenuJson)
                OUTPUT inserted.Id
                VALUES (@Ad, @MenuJson)
                """,
                new { tanim.Ad, tanim.MenuJson });
        }
        catch (SqlException ex) when (ex.Number is 2601 or 2627)
        {
            throw new InvalidOperationException(
                "Bu ad başka bir projede kullanılıyor. " +
                "Silinmiş projelerin adları da korunur.",
                ex);
        }
    }

    public async Task GuncelleAsync(
        Proje eskiProje,
        string ad,
        string tanimlar)
    {
        var tanim = ProjeTanimi.Hazirla(ad, tanimlar);

        const string sql = """
            UPDATE dbo.Projeler
            SET Ad = @Ad, MenuJson = @MenuJson
            WHERE Id = @Id AND Silindi = 0
              AND CONVERT(varbinary(max), Ad) =
                  CONVERT(varbinary(max), @EskiAd)
              AND CONVERT(varbinary(max), MenuJson) =
                  CONVERT(varbinary(max), @EskiMenuJson)
            """;

        using var conn = new SqlConnection(App.BaglantiDizesi);

        try
        {
            int sonuc = await conn.ExecuteAsync(sql, new
            {
                eskiProje.Id,
                tanim.Ad,
                tanim.MenuJson,
                EskiAd = eskiProje.Ad,
                EskiMenuJson = eskiProje.MenuJson
            });

            if (sonuc == 0)
            {
                throw new InvalidOperationException(
                    "Proje başka bir kullanıcı tarafından değiştirilmiş " +
                    "veya silinmiş olabilir. " +
                    "Pencereyi kapatıp proje listesini yenileyiniz.");
            }
        }
        catch (SqlException ex) when (ex.Number is 2601 or 2627)
        {
            throw new InvalidOperationException(
                "Bu ad başka bir projede kullanılıyor. " +
                "Silinmiş projelerin adları da korunur.",
                ex);
        }
    }

    public async Task SilAsync(int id)
    {
        using var conn = new SqlConnection(App.BaglantiDizesi);

        int sonuc = await conn.ExecuteAsync(
            """
            UPDATE dbo.Projeler
            SET Silindi = 1
            WHERE Id = @Id AND Silindi = 0
            """,
            new { Id = id });

        if (sonuc == 0)
        {
            throw new InvalidOperationException(
                "Proje bulunamadı veya zaten silinmiş. Listeyi yenileyiniz.");
        }
    }
}