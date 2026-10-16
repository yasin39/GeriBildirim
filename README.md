# Geri Bildirim

WPF uygulaması .NET 10 üzerinde SQLite kullanır. Uygulamanın çalışması için SQL Server gerekmez.

## Veritabanı

`appsettings.json` içindeki varsayılan bağlantı, `%LOCALAPPDATA%\GeriBildirim\GeriBildirim.db` dosyasını kullanır. Ortam değişkenleri başlangıçta genişletilir. Göreli bir dosya yolu verilirse uygulamanın bulunduğu klasöre göre çözülür; seçilen klasörün yazılabilir olması gerekir.

İlk açılışta `Data/GeriBildirim.seed.db` bu konuma kopyalanır. Mevcut kullanıcı veritabanının üzerine yazılmaz; sonraki açılışlar ve kurulum güncellemeleri kullanıcı verilerini korur. Başlangıçta bütünlük ve yabancı anahtar kontrolleri yapılır. Kaynak dosya yoksa boş veritabanı oluşturulmaz.

16 Ekim 2026 tarihinde `YASIN/Portal` veritabanından, bu uygulamanın kullandığı iki tablonun tamamı aktarıldı:

| Tablo | Kayıt sayısı | Korunan kimlik sayacı |
| --- | ---: | ---: |
| Projeler | 7 | 7 |
| GeriBildirimler | 45 | 1037 |

Her kaydın tüm alanları kaynakla birebir karşılaştırıldı; tarih/saatler, Türkçe metinler, menü JSON içerikleri, silinme/okunma/arşiv durumları ve proje ilişkileri korundu. İlgisiz Portal tabloları kapsam dışındadır. Kaynak SQL Server verileri değiştirilmedi. Aktarım sonrasında SQL Server ile eşitleme yapılmaz.

Metin arama ve proje adı benzersizliği, bağlantılarda kaydedilen `SQLSERVER_CI_AS` karşılaştırıcısını kullanır: `en-US` kültürüyle büyük/küçük harf duyarsız, aksan duyarlı karşılaştırma ve sondaki boşlukları yok sayma. Proje güncellemelerinin eşzamanlılık kontrolü ise birebir metin karşılaştırması yapar. SQLite dosyasını başka bir araçla değiştirirken aynı karşılaştırıcının kaydedilmesi gerekir.

Varsayılan dosya her Windows kullanıcısı için ayrıdır. Yedek almak veya başka bilgisayara taşımak için uygulamanın tüm örneklerini kapatıp kullanıcı veritabanı dosyasını kopyalayın; `seed` dosyası sonradan eklenen kullanıcı verilerini içermez.

## Derleme ve doğrulama

```powershell
dotnet build GeriBildirim.slnx
dotnet run --project tests/DatabaseChecks
```

Veritabanı kontrolleri aktarılan dosyanın geçici bir kopyasında çalışır. Kayıt okuma/ekleme/güncelleme/silme, tarih/proje/modül/durum filtreleri, kimlik sayaçları, Türkçe metinler, eşzamanlı yazma, benzersizlik, JSON ve yabancı anahtar kısıtları ile yeniden açılışta verilerin korunması doğrulanır.

## Aktarımı yeniden çalıştırma

SQL Server bağımlılığı yalnızca bağımsız `tools/DatabaseMigration` aracındadır. Araç kaynaktan salt okuma yapar ve yeni bir SQLite dosyası oluşturur; var olan hedefin üzerine yazmayı reddeder. Kaynak bağlantı dizesi ortam değişkeniyle verilir:

```powershell
$env:GERIBILDIRIM_SOURCE_CONNECTION = 'Server=YASIN;Database=Portal;Trusted_Connection=True;TrustServerCertificate=True;Connect Timeout=8'
dotnet run --project tools/DatabaseMigration -- migrate C:\aktarim\GeriBildirim.db
dotnet run --project tools/DatabaseMigration -- verify C:\aktarim\GeriBildirim.db
Remove-Item Env:\GERIBILDIRIM_SOURCE_CONNECTION
```

İki tablo tek SQL Server işlemi içinde tutarlı olarak okunur. Hedefe ekleme tek SQLite işlemiyle yapılır; tüm alanlar, kimlik sayaçları ve veritabanı bütünlüğü doğrulanmadan nihai dosya oluşturulmaz. `verify` komutu hedefi değiştirmeden kaynakla karşılaştırır. Çıktı, kayıt sayılarını ve içerik SHA-256 özetlerini gösterir.

## Dağıtım

Kurulum betiğinin kullandığı yayın klasörünü depo kökünde şu komutla hazırlayın, ardından `GeriBildirim/installer/setup.iss` dosyasını Inno Setup ile derleyin:

```powershell
dotnet publish GeriBildirim/GeriBildirim.csproj -c Release --self-contained false -o artifacts/sqlite-publish
```

`dotnet publish` çıktısı, aktarılan `Data/GeriBildirim.seed.db` dosyasını ve SQLite yerel kitaplıklarını içerir. `installer/setup.iss` sürümü 1.0.4'tür; güncellemede SQL Server bağlantısı yerine SQLite ayarını kurar. Kullanıcı veritabanı kurulum klasörünün dışında kaldığından güncellemede değiştirilmez.
