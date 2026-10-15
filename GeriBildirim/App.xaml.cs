using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Threading;
using GeriBildirim.Models;

namespace GeriBildirim;

/// <summary>
/// Uygulama giriş noktası. Pencere açılmadan önce kültürü ayarlar,
/// exe'nin yanındaki appsettings.json ve menu.json dosyalarını okur.
/// </summary>
public partial class App : Application
{
    private const string AyarDosyasi = "appsettings.json";
    private const string UygulamaAdi = "Geri Bildirim";

    /// <summary>SQL Server bağlantı dizesi. Başlangıçta bir kez okunur.</summary>
    public static string BaglantiDizesi { get; private set; } = string.Empty;

    protected override void OnStartup(StartupEventArgs e)
    {
        KulturuAyarla();
        DispatcherUnhandledException += BeklenmeyenHata;

        if (!AyarlariYukle())
        {
            Shutdown(1);
            return;
        }

        string? menuHata = GeriBildirimMenu.Yukle();
        if (menuHata is not null)
        {
            MessageBox.Show(menuHata, UygulamaAdi, MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
            return;
        }

        base.OnStartup(e);
    }

    /// <summary>
    /// Uygulamayı tr-TR kültürüne alır. WPF kontrolleri (DatePicker gibi)
    /// kendi dilini ayrıca belirlediği için LanguageProperty de ezilir.
    /// </summary>
    private static void KulturuAyarla()
    {
        var kultur = new CultureInfo("tr-TR");

        CultureInfo.DefaultThreadCurrentCulture = kultur;
        CultureInfo.DefaultThreadCurrentUICulture = kultur;
        CultureInfo.CurrentCulture = kultur;
        CultureInfo.CurrentUICulture = kultur;

        FrameworkElement.LanguageProperty.OverrideMetadata(
            typeof(FrameworkElement),
            new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(kultur.IetfLanguageTag)));
    }

    /// <summary>
    /// Hiçbir yerde yakalanmayan hatalarda uygulamanın çökmesini engeller,
    /// hatayı kullanıcıya gösterir ve çalışmaya devam eder.
    /// </summary>
    private static void BeklenmeyenHata(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        MessageBox.Show(
            $"Beklenmeyen bir hata oluştu:\n{e.Exception.Message}",
            UygulamaAdi, MessageBoxButton.OK, MessageBoxImage.Error);

        e.Handled = true;
    }

    /// <summary>
    /// Ayar dosyasını okur. Dosya yoksa ya da geçersizse kullanıcıya
    /// bilgi verir ve false döner.
    /// </summary>
    private static bool AyarlariYukle()
    {
        string yol = Path.Combine(AppContext.BaseDirectory, AyarDosyasi);

        if (!File.Exists(yol))
        {
            MessageBox.Show(
                $"Ayar dosyası bulunamadı:\n{yol}",
                UygulamaAdi, MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }

        try
        {
            using var belge = JsonDocument.Parse(File.ReadAllText(yol));
            string? baglanti = belge.RootElement
                .GetProperty("ConnectionStrings")
                .GetProperty("DefaultConnection")
                .GetString();

            if (string.IsNullOrWhiteSpace(baglanti))
            {
                MessageBox.Show(
                    "appsettings.json içinde bağlantı dizesi boş.",
                    UygulamaAdi, MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            BaglantiDizesi = baglanti;
            return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Ayar dosyası okunamadı:\n{ex.Message}",
                UygulamaAdi, MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }
    }
}