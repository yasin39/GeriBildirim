using System.Data;
using System.Globalization;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using GeriBildirim.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;

if (args.Length != 2 || args[0] is not ("migrate" or "verify") ||
    string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("GERIBILDIRIM_SOURCE_CONNECTION")))
{
    Console.Error.WriteLine("GERIBILDIRIM_SOURCE_CONNECTION ortam değişkenini ayarlayın.\n" +
        "Kullanım: DatabaseMigration <migrate|verify> <sqlite-dosyası>");
    return 1;
}

string target = Path.GetFullPath(args[1]);
string temporary = target + "." + Guid.NewGuid().ToString("N") + ".tmp";
string[] tables = ["Projeler", "GeriBildirimler"];
string[][] columns =
[
    ["Id", "Ad", "MenuJson", "Silindi"],
    ["Id", "KullaniciAdSoyad", "ModulAdi", "SayfaAdi", "BildirimIcerigi", "KayitTarihi", "Okundu", "Arsivlendi", "ProjeId"]
];

try
{
    if (args[0] == "migrate" && File.Exists(target))
        throw new IOException("Hedef dosya zaten var; mevcut verilerin üzerine yazılmaz: " + target);
    if (args[0] == "verify" && !File.Exists(target))
        throw new FileNotFoundException("Doğrulanacak SQLite dosyası yok.", target);

    using var source = new SqlConnection(Environment.GetEnvironmentVariable("GERIBILDIRIM_SOURCE_CONNECTION"));
    source.Open();
    // Her iki tablo tek tutarlı görünümden okunur. Kaynakta INSERT/UPDATE/DELETE yapılmaz.
    using var sourceTransaction = source.BeginTransaction(IsolationLevel.Serializable);
    var data = new List<object?[]>[tables.Length];
    var sequences = new long[tables.Length];
    for (int i = 0; i < tables.Length; i++)
    {
        using var command = source.CreateCommand();
        command.Transaction = sourceTransaction;
        command.CommandText = $"SELECT {string.Join(", ", columns[i].Select(c => $"[{c}]"))} " +
            $"FROM dbo.[{tables[i]}] WITH (HOLDLOCK) ORDER BY Id";
        data[i] = [];
        using (var reader = command.ExecuteReader())
        {
            while (reader.Read())
            {
                var row = new object?[reader.FieldCount];
                for (int c = 0; c < row.Length; c++) row[c] = Normalize(reader.GetValue(c));
                data[i].Add(row);
            }
        }
        command.CommandText = $"SELECT CONVERT(bigint, IDENT_CURRENT('dbo.{tables[i]}'))";
        sequences[i] = Convert.ToInt64(command.ExecuteScalar(), CultureInfo.InvariantCulture);
    }

    if (args[0] == "migrate")
    {
        Directory.CreateDirectory(Path.GetDirectoryName(target)!);
        using var destination = SqliteDatabase.CreateConnection(new SqliteConnectionStringBuilder
        {
            DataSource = temporary, ForeignKeys = true, Pooling = false
        }.ToString());
        destination.Open();
        using var transaction = destination.BeginTransaction();
        using var command = destination.CreateCommand();
        command.Transaction = transaction;
        using var schema = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream("Schema.sql")!);
        command.CommandText = schema.ReadToEnd();
        command.ExecuteNonQuery();
        for (int i = 0; i < tables.Length; i++)
        {
            command.CommandText = $"INSERT INTO {tables[i]} ({string.Join(", ", columns[i])}) " +
                $"VALUES ({string.Join(", ", columns[i].Select((_, n) => "$p" + n))})";
            command.Parameters.Clear();
            for (int c = 0; c < columns[i].Length; c++) command.Parameters.AddWithValue("$p" + c, DBNull.Value);
            foreach (var row in data[i])
            {
                for (int c = 0; c < row.Length; c++) command.Parameters[c].Value = row[c] ?? DBNull.Value;
                command.ExecuteNonQuery();
            }
            // Silinmiş son kimliklerin yeniden kullanılmasını da önler.
            command.Parameters.Clear();
            command.CommandText = "DELETE FROM sqlite_sequence WHERE name = $table; " +
                "INSERT INTO sqlite_sequence (name, seq) VALUES ($table, $sequence)";
            command.Parameters.AddWithValue("$table", tables[i]);
            command.Parameters.AddWithValue("$sequence", sequences[i]);
            command.ExecuteNonQuery();
        }
        transaction.Commit();
    }

    string verifyPath = args[0] == "migrate" ? temporary : target;
    var verifyConnection = new SqliteConnectionStringBuilder
    {
        DataSource = verifyPath, Mode = SqliteOpenMode.ReadOnly, ForeignKeys = true, Pooling = false
    }.ToString();
    SqliteDatabase.Validate(verifyConnection);
    using (var destination = SqliteDatabase.CreateConnection(verifyConnection))
    {
        destination.Open();
        for (int i = 0; i < tables.Length; i++)
        {
            using var command = destination.CreateCommand();
            command.CommandText = $"SELECT {string.Join(", ", columns[i])} FROM {tables[i]} ORDER BY Id";
            using (var reader = command.ExecuteReader())
            {
                int index = 0;
                while (reader.Read())
                {
                    if (index >= data[i].Count) throw new InvalidDataException($"{tables[i]}: fazla kayıt.");
                    for (int c = 0; c < reader.FieldCount; c++)
                    {
                        if (!Equals(data[i][index][c], Normalize(reader.GetValue(c))))
                            throw new InvalidDataException($"{tables[i]}: {index + 1}. kayıtta {columns[i][c]} farklı.");
                    }
                    index++;
                }
                if (index != data[i].Count) throw new InvalidDataException($"{tables[i]}: eksik kayıt.");
            }
            command.CommandText = "SELECT seq FROM sqlite_sequence WHERE name = $table";
            command.Parameters.AddWithValue("$table", tables[i]);
            if (Convert.ToInt64(command.ExecuteScalar(), CultureInfo.InvariantCulture) != sequences[i])
                throw new InvalidDataException($"{tables[i]}: kimlik sayacı farklı.");
            string hash = Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(data[i])));
            Console.WriteLine($"{tables[i]}: {data[i].Count} kayıt; tüm alanlar eşit; identity={sequences[i]}; SHA256={hash}");
        }
    }
    sourceTransaction.Commit();
    if (args[0] == "migrate") File.Move(temporary, target, overwrite: false);
    Console.WriteLine("Bütünlük ve yabancı anahtar kontrolleri başarılı: " + target);
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine(ex.Message);
    return 1;
}
finally
{
    if (File.Exists(temporary)) File.Delete(temporary);
}

static object? Normalize(object value) => value switch
{
    DBNull => null,
    bool flag => flag ? 1L : 0L,
    int number => (long)number,
    DateTime date => date.ToString("yyyy-MM-dd HH:mm:ss.fffffff", CultureInfo.InvariantCulture),
    _ => value
};
