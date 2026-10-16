using System.Globalization;
using System.IO;
using Microsoft.Data.Sqlite;

namespace GeriBildirim.Data;

public static class SqliteDatabase
{
    public static string ResolveConnectionString(string connectionString)
    {
        var builder = new SqliteConnectionStringBuilder(
            Environment.ExpandEnvironmentVariables(connectionString));
        if (string.IsNullOrWhiteSpace(builder.DataSource) || builder.DataSource == ":memory:")
            throw new InvalidOperationException("Kalıcı bir SQLite dosyası belirtilmelidir.");

        builder.DataSource = Path.GetFullPath(builder.DataSource, AppContext.BaseDirectory);
        builder.ForeignKeys = true;
        builder.Mode = SqliteOpenMode.ReadWrite;
        return builder.ToString();
    }

    public static SqliteConnection CreateConnection(string connectionString)
    {
        var connection = new SqliteConnection(connectionString);
        // Kaynaktaki SQL_Latin1_General_CP1_CI_AS gibi büyük/küçük harf
        // duyarsız, aksan duyarlı karşılaştırma; sondaki boşluklar yok sayılır.
        connection.CreateCollation("SQLSERVER_CI_AS", (left, right) =>
            CultureInfo.GetCultureInfo("en-US").CompareInfo.Compare(
                left?.TrimEnd(' '), right?.TrimEnd(' '),
                CompareOptions.IgnoreCase | CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth));
        return connection;
    }

    public static void Initialize(string connectionString, string? seedPath = null)
    {
        string target = new SqliteConnectionStringBuilder(connectionString).DataSource;
        seedPath ??= Path.Combine(AppContext.BaseDirectory, "Data", "GeriBildirim.seed.db");

        if (!File.Exists(target))
        {
            // Eksik kaynak halinde boş bir veritabanıyla açılıp veri kaybı izlenimi verme.
            Validate(new SqliteConnectionStringBuilder
            {
                DataSource = seedPath,
                Mode = SqliteOpenMode.ReadOnly,
                ForeignKeys = true
            }.ToString());
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            string temporary = target + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                File.Copy(seedPath, temporary);
                try
                {
                    File.Move(temporary, target, overwrite: false);
                }
                catch (IOException) when (File.Exists(target))
                {
                    // Aynı anda açılan diğer uygulama kopyalamayı tamamlamış olabilir.
                }
            }
            finally
            {
                if (File.Exists(temporary)) File.Delete(temporary);
            }
        }

        Validate(connectionString);
    }

    public static void Validate(string connectionString)
    {
        using var connection = CreateConnection(connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA user_version";
        if (Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture) != 1)
            throw new InvalidOperationException("SQLite veritabanının şema sürümü desteklenmiyor.");

        command.CommandText = "PRAGMA integrity_check";
        if (!string.Equals(command.ExecuteScalar() as string, "ok", StringComparison.Ordinal))
            throw new InvalidOperationException("SQLite veritabanının bütünlük kontrolü başarısız.");

        command.CommandText = "PRAGMA foreign_key_check";
        using var reader = command.ExecuteReader();
        if (reader.Read())
            throw new InvalidOperationException("SQLite veritabanında geçersiz proje ilişkisi var.");
    }
}
