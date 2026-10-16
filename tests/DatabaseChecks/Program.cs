using System.Globalization;
using System.IO;
using System.Text.Json;
using Dapper;
using GeriBildirim.Data;
using GeriBildirim.Models;
using Microsoft.Data.Sqlite;

CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
string seed = Path.Combine(AppContext.BaseDirectory, "Data", "GeriBildirim.seed.db");

if (args is ["--initialize-config"])
{
    using var settings = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "appsettings.json")));
    string configured = settings.RootElement.GetProperty("ConnectionStrings").GetProperty("DefaultConnection").GetString()!;
    string resolved = SqliteDatabase.ResolveConnectionString(configured);
    SqliteDatabase.Initialize(resolved);
    Console.WriteLine("SQLite hazır: " + new SqliteConnectionStringBuilder(resolved).DataSource);
    Console.WriteLine("Projeler: " + (await new ProjeRepository(resolved).GetProjelerAsync(true)).Count);
    Console.WriteLine("GeriBildirimler: " + (await new GeriBildirimRepository(resolved).GetGeriBildirimlerAsync(new())).Count);
    return;
}

string directory = Path.Combine(AppContext.BaseDirectory, "checks-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(directory);
int passed = 0;
try
{
    string target = Path.Combine(directory, "test.db");
    string connectionString = SqliteDatabase.ResolveConnectionString($"Data Source={target};Pooling=False;Default Timeout=1");
    SqliteDatabase.Initialize(connectionString, seed);
    var projects = new ProjeRepository(connectionString);
    var feedback = new GeriBildirimRepository(connectionString);
    var initialProjects = await projects.GetProjelerAsync(true);
    var initialFeedback = await feedback.GetGeriBildirimlerAsync(new());
    Check(initialProjects.Count > 0 && initialFeedback.Count > 0, "Aktarılmış veriler ve Dapper tür dönüşümleri");
    foreach (var row in initialFeedback)
        Check(await feedback.GetGeriBildirimByIdAsync(row.Id) == row, "Tekil kayıt eşleşmesi");

    DateTime sampleDate = initialFeedback[0].KayitTarihi.Date;
    var dated = await feedback.GetGeriBildirimlerAsync(new() { IlkTarih = sampleDate, SonTarih = sampleDate });
    Check(dated.All(r => r.KayitTarihi.Date == sampleDate) && dated.Count > 0, "Tarih aralığı filtresi");
    Check((await feedback.GetGeriBildirimlerAsync(new() { Durum = GeriBildirimDurum.Okunmamis }))
        .All(r => !r.Okundu && !r.Arsivlendi), "Bekleyen kayıt filtresi");
    Check((await feedback.GetGeriBildirimlerAsync(new() { Durum = GeriBildirimDurum.Okunmus }))
        .All(r => r.Okundu && !r.Arsivlendi), "İşlem yapılan kayıt filtresi");
    Check((await feedback.GetGeriBildirimlerAsync(new() { Durum = GeriBildirimDurum.Arsivlenmis }))
        .All(r => r.Arsivlendi), "Arşiv filtresi");

    int projectId = await projects.EkleAsync("SQLite Deneme Çağ", "Modül | Sayfa");
    Check(projectId > initialProjects.Max(p => p.Id), "Proje kimlik sayacı");
    await Reject<InvalidOperationException>(() => projects.EkleAsync("SQLITE DENEME ÇAĞ", "Modül | Sayfa"), "Unicode büyük/küçük harf benzersizliği");
    var project = (await projects.GetProjelerAsync()).Single(p => p.Id == projectId);
    await projects.GuncelleAsync(project, "sqlite Deneme Çağ", "Modül | Sayfa\nModül | Diğer");
    await Reject<InvalidOperationException>(() => projects.GuncelleAsync(project, "Çakışma", "Modül | Sayfa"), "Harf değişikliğinde iyimser eşzamanlılık");
    project = (await projects.GetProjelerAsync()).Single(p => p.Id == projectId);

    using var connection = SqliteDatabase.CreateConnection(connectionString);
    connection.Open();
    long previousSequence = connection.QuerySingle<long>("SELECT seq FROM sqlite_sequence WHERE name='GeriBildirimler'");
    Check(await feedback.GeriBildirimEkleAsync(projectId, "Çağrı Şen", "Modül", "Sayfa", "İçerik\r\nİkinci satır") == 1, "Geri bildirim ekleme");
    var inserted = (await feedback.GetGeriBildirimlerAsync(new() { ProjeId = projectId })).Single();
    Check(inserted.Id > previousSequence && inserted.KullaniciAdSoyad == "Çağrı Şen" &&
        inserted.BildirimIcerigi == "İçerik\r\nİkinci satır" && !inserted.Okundu && !inserted.Arsivlendi,
        "Kimlik, Türkçe metin ve varsayılan durumlar");
    Check(Math.Abs((DateTime.Now - inserted.KayitTarihi).TotalSeconds) < 10, "Yerel tarih ve saat");
    Check((await feedback.GetGeriBildirimlerAsync(new() { ProjeId = projectId, ModulAdi = "MODÜL" })).Count == 1, "Unicode modül filtresi");
    Check(await feedback.ToggleOkunduAsync(inserted.Id) == true && await feedback.ToggleOkunduAsync(inserted.Id) == false, "Okundu durumu geçişleri");
    Check(await feedback.ToggleArsivAsync(inserted.Id) == true && await feedback.ToggleArsivAsync(inserted.Id) == false, "Arşiv durumu geçişleri");
    Check(await feedback.ToggleOkunduAsync(int.MaxValue) is null && await feedback.ToggleArsivAsync(int.MaxValue) is null, "Olmayan kayıt durum geçişleri");

    await Reject<InvalidOperationException>(() => feedback.GeriBildirimEkleAsync(int.MaxValue, "Ad", "Modül", "Sayfa", "Metin"), "Olmayan projeye bildirim engeli");
    await Reject<InvalidOperationException>(() => feedback.GeriBildirimEkleAsync(projectId, "Ad", "Modül", "Kaldırılmış", "Metin"), "Geçersiz sayfaya bildirim engeli");
    Check((await feedback.GetGeriBildirimlerAsync(new() { ProjeId = projectId })).Count == 1, "Başarısız işlemlerde geri alma");
    await Reject<SqliteException>(() => connection.ExecuteAsync("UPDATE GeriBildirimler SET ProjeId=2147483647 WHERE Id=@Id", new { inserted.Id }), "Yabancı anahtar kısıtı");
    await Reject<SqliteException>(() => connection.ExecuteAsync("UPDATE Projeler SET MenuJson='bozuk' WHERE Id=@Id", new { Id = projectId }), "JSON kısıtı");
    await Reject<SqliteException>(() => connection.ExecuteAsync("UPDATE GeriBildirimler SET Okundu=2 WHERE Id=@Id", new { inserted.Id }), "Boolean kısıtı");

    using (var transaction = connection.BeginTransaction(deferred: false))
    using (var other = SqliteDatabase.CreateConnection(connectionString))
    {
        other.Open();
        try
        {
            using var conflicting = other.BeginTransaction(deferred: false);
            throw new Exception("Eşzamanlı yazma engellenmedi.");
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == 5)
        {
            Check(true, "İşlem boyunca eşzamanlı yazma engeli");
        }
        transaction.Rollback();
    }

    await projects.SilAsync(projectId);
    Check(!(await projects.GetProjelerAsync()).Any(p => p.Id == projectId), "Silinen projenin gizlenmesi");
    Check((await projects.GetProjelerAsync(true)).Single(p => p.Id == projectId).Silindi, "Silinen projelerin korunması");
    Check(await feedback.GetGeriBildirimByIdAsync(inserted.Id) is not null, "Silinen projenin bildirim geçmişi");
    await Reject<InvalidOperationException>(() => projects.EkleAsync(project.Ad, "Modül | Sayfa"), "Silinmiş proje adının korunması");
    await Reject<InvalidOperationException>(() => feedback.GeriBildirimEkleAsync(projectId, "Ad", "Modül", "Sayfa", "Metin"), "Silinen projeye bildirim engeli");
    SqliteDatabase.Initialize(connectionString, seed);
    Check((await projects.GetProjelerAsync(true)).Any(p => p.Id == projectId), "Tekrar başlangıçta mevcut dosyanın korunması");
    Check(await feedback.GeriBildirimSilAsync(inserted.Id) == 1 && await feedback.GetGeriBildirimByIdAsync(inserted.Id) is null, "Bildirim silme");
    Check(await feedback.GeriBildirimSilAsync(inserted.Id) == 0, "Olmayan bildirim silme");
    Check((await feedback.GetGeriBildirimlerAsync(new())).SequenceEqual(initialFeedback), "Başlangıçtaki tüm geri bildirimlerin korunması");
    SqliteDatabase.Validate(connectionString);

    string missingTarget = Path.Combine(directory, "missing.db");
    string missingConnection = SqliteDatabase.ResolveConnectionString($"Data Source={missingTarget};Pooling=False");
    await Reject<SqliteException>(() => { SqliteDatabase.Initialize(missingConnection, Path.Combine(directory, "absent.db")); return Task.CompletedTask; }, "Kaynak dosya olmadan boş veritabanı açılmaması");
    Check(!File.Exists(missingTarget), "Başarısız başlangıçta hedef oluşturulmaması");
    Console.WriteLine($"{passed} kontrol başarılı.");
}
finally
{
    SqliteConnection.ClearAllPools();
    // Yalnızca bu çalışmanın oluşturduğu, doğrulanmış geçici alt klasör temizlenir.
    string fullDirectory = Path.GetFullPath(directory);
    if (Path.GetDirectoryName(fullDirectory) == Path.TrimEndingDirectorySeparator(AppContext.BaseDirectory))
        Directory.Delete(fullDirectory, recursive: true);
}

void Check(bool condition, string name)
{
    if (!condition) throw new Exception("Kontrol başarısız: " + name);
    passed++;
}

async Task Reject<T>(Func<Task> action, string name) where T : Exception
{
    try { await action(); }
    catch (T) { Check(true, name); return; }
    throw new Exception("Beklenen hata oluşmadı: " + name);
}
