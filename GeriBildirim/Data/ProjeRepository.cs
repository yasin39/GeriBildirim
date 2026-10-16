using Dapper;
using GeriBildirim.Models;
using Microsoft.Data.Sqlite;

namespace GeriBildirim.Data;

public class ProjeRepository
{
    private readonly string _connectionString;

    public ProjeRepository(string? connectionString = null)
    {
        _connectionString = connectionString ?? App.BaglantiDizesi;
    }

    public async Task<List<Proje>> GetProjelerAsync(bool silinenleriDahilEt = false)
    {
        const string sql = """
            SELECT Id, Ad, MenuJson, Silindi
            FROM Projeler
            WHERE Silindi = 0 OR @SilinenleriDahilEt = 1
            ORDER BY Silindi, Ad
            """;

        using var conn = SqliteDatabase.CreateConnection(_connectionString);

        return (await conn.QueryAsync<Proje>(
            sql,
            new { SilinenleriDahilEt = silinenleriDahilEt })).AsList();
    }

    public async Task<int> EkleAsync(string ad, string tanimlar)
    {
        var tanim = ProjeTanimi.Hazirla(ad, tanimlar);

        using var conn = SqliteDatabase.CreateConnection(_connectionString);

        try
        {
            return await conn.QuerySingleAsync<int>(
                """
                INSERT INTO Projeler (Ad, MenuJson)
                VALUES (@Ad, @MenuJson)
                RETURNING Id
                """,
                new { tanim.Ad, tanim.MenuJson });
        }
        catch (SqliteException ex) when (ex.SqliteExtendedErrorCode is 2067 or 1555)
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
            UPDATE Projeler
            SET Ad = @Ad, MenuJson = @MenuJson
            WHERE Id = @Id AND Silindi = 0
              AND Ad COLLATE BINARY = @EskiAd
              AND MenuJson COLLATE BINARY = @EskiMenuJson
            """;

        using var conn = SqliteDatabase.CreateConnection(_connectionString);

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
        catch (SqliteException ex) when (ex.SqliteExtendedErrorCode is 2067 or 1555)
        {
            throw new InvalidOperationException(
                "Bu ad başka bir projede kullanılıyor. " +
                "Silinmiş projelerin adları da korunur.",
                ex);
        }
    }

    public async Task SilAsync(int id)
    {
        using var conn = SqliteDatabase.CreateConnection(_connectionString);

        int sonuc = await conn.ExecuteAsync(
            """
            UPDATE Projeler
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
