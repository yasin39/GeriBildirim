using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using GeriBildirim.Data;
using GeriBildirim.Models;
using GeriBildirim.Services;

namespace GeriBildirim.Views;

/// <summary>
/// Yeni geri bildirim gönderim formu. Modül seçimine göre sayfa listesi
/// doldurulur; kullanıcı adı Windows hesabından alınır.
/// </summary>
public partial class GonderView : UserControl
{
    private const int MaxKarakter = 3000;

    private readonly GeriBildirimRepository _repository = new();
    private readonly KullaniciService _kullaniciService = new();

    public GonderView()
    {
        InitializeComponent();
        ModulCombo.ItemsSource = GeriBildirimMenu.ModulAdlari.ToList();
    }

    // -----------------------------------------------------------------------
    // SEÇİM VE METİN OLAYLARI
    // -----------------------------------------------------------------------

    /// <summary>Modül değişince sayfa listesini o modüle göre doldurur.</summary>
    private void ModulCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        MesajKutu.Visibility = Visibility.Collapsed;

        if (ModulCombo.SelectedItem is string modul)
        {
            SayfaCombo.ItemsSource = GeriBildirimMenu.SayfalariGetir(modul);
            SayfaCombo.SelectedIndex = -1;
            SayfaCombo.IsEnabled = true;
        }
        else
        {
            SayfaCombo.ItemsSource = null;
            SayfaCombo.IsEnabled = false;
        }
    }

    private void SayfaCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        MesajKutu.Visibility = Visibility.Collapsed;
    }

    private void BildirimText_TextChanged(object sender, TextChangedEventArgs e)
    {
        MesajKutu.Visibility = Visibility.Collapsed;
        SayacText.Text = $"{BildirimText.Text.Length} / {MaxKarakter}";
    }

    // -----------------------------------------------------------------------
    // BUTONLAR
    // -----------------------------------------------------------------------

    private async void GonderButton_Click(object sender, RoutedEventArgs e)
    {
        if (!FormuDogrula())
        {
            return;
        }

        string modul = (string)ModulCombo.SelectedItem;
        string sayfa = (string)SayfaCombo.SelectedItem;
        string icerik = BildirimText.Text.Trim();

        GonderButton.IsEnabled = false;

        try
        {
            string adSoyad = await Task.Run(() => _kullaniciService.AdSoyad);
            int sonuc = await _repository.GeriBildirimEkleAsync(adSoyad, modul, sayfa, icerik);

            if (sonuc > 0)
            {
                FormuTemizle();
                MesajGoster("Geri bildiriminiz başarıyla kaydedildi. Teşekkür ederiz!", true);
            }
            else
            {
                MesajGoster("Geri bildirim kaydedilemedi. Lütfen tekrar deneyiniz.", false);
            }
        }
        catch (Exception ex)
        {
            MesajGoster($"Geri bildirim kaydedilirken bir hata oluştu: {ex.Message}", false);
        }
        finally
        {
            GonderButton.IsEnabled = true;
        }
    }

    private void TemizleButton_Click(object sender, RoutedEventArgs e)
    {
        FormuTemizle();
    }

    // -----------------------------------------------------------------------
    // YARDIMCILAR
    // -----------------------------------------------------------------------

    /// <summary>
    /// Zorunlu alanları kontrol eder. İlk eksik alanda mesaj gösterir ve
    /// odağı o alana verir.
    /// </summary>
    private bool FormuDogrula()
    {
        if (ModulCombo.SelectedItem is null)
        {
            MesajGoster("Lütfen modül seçiniz.", false);
            ModulCombo.Focus();
            return false;
        }

        if (SayfaCombo.SelectedItem is null)
        {
            MesajGoster("Lütfen sayfa seçiniz.", false);
            SayfaCombo.Focus();
            return false;
        }

        if (string.IsNullOrWhiteSpace(BildirimText.Text))
        {
            MesajGoster("Lütfen bildiriminizi yazınız.", false);
            BildirimText.Focus();
            return false;
        }

        return true;
    }

    private void FormuTemizle()
    {
        ModulCombo.SelectedIndex = -1;
        BildirimText.Clear();
        MesajKutu.Visibility = Visibility.Collapsed;
    }

    private void MesajGoster(string metin, bool basarili)
    {
        MesajKutu.Background = (Brush)FindResource(basarili ? "BasariZeminBrush" : "HataZeminBrush");
        MesajText.Foreground = (Brush)FindResource(basarili ? "BasariMetinBrush" : "HataMetinBrush");
        MesajText.Text = metin;
        MesajKutu.Visibility = Visibility.Visible;
    }
}