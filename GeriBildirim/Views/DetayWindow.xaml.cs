using System.Windows;
using GeriBildirim.Models;

namespace GeriBildirim.Views;

/// <summary>
/// Seçilen geri bildirimin tüm alanlarını salt okunur gösteren pencere.
/// Kaydın durumunu değiştirmez.
/// </summary>
public partial class DetayWindow : Window
{
    public DetayWindow(GeriBildirimSatir kayit)
    {
        InitializeComponent();

        Title = $"Geri Bildirim Detayı — #{kayit.Id}";
        KullaniciText.Text = kayit.KullaniciAdSoyad;
        TarihText.Text = kayit.KayitTarihi.ToString("dd.MM.yyyy HH:mm");
        ModulText.Text = kayit.ModulAdi;
        SayfaText.Text = kayit.SayfaAdi;
        DurumText.Text = kayit.DurumMetni;
        IcerikText.Text = kayit.BildirimIcerigi;
    }
}