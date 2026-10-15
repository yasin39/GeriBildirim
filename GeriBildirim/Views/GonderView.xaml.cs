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
        ModulCombo.IsEnabled = false;
    }

    private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        => await ProjeleriYukleAsync();

    private async void ProjeleriYenile_Click(object sender, RoutedEventArgs e)
        => await ProjeleriYukleAsync();

    private async Task ProjeleriYukleAsync()
    {
        int? seciliId = (ProjeCombo.SelectedItem as Proje)?.Id;

        try
        {
            var projeler = await new ProjeRepository().GetProjelerAsync();
            ProjeCombo.ItemsSource = projeler;
            ProjeCombo.SelectedItem = projeler.FirstOrDefault(p => p.Id == seciliId)
                ?? (projeler.Count == 1 ? projeler[0] : null);
        }
        catch (Exception ex)
        {
            ProjeCombo.ItemsSource = null;
            MesajGoster(
                $"Projeler yüklenemedi. Veritabanı güncellemesini ve bağlantıyı kontrol ediniz: {ex.Message}",
                false);
        }
    }

    private void ProjeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ModulCombo.ItemsSource = (ProjeCombo.SelectedItem as Proje)?
            .Moduller.Select(m => m.Modul).ToList();

        ModulCombo.IsEnabled = ProjeCombo.SelectedItem is Proje;
        SayfaCombo.ItemsSource = null;
        SayfaCombo.IsEnabled = false;
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
            SayfaCombo.ItemsSource = (ProjeCombo.SelectedItem as Proje)?
                .Moduller.FirstOrDefault(m => m.Modul == modul)?.Sayfalar;

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

        int projeId = ((Proje)ProjeCombo.SelectedItem).Id;
        string modul = (string)ModulCombo.SelectedItem;
        string sayfa = (string)SayfaCombo.SelectedItem;
        string icerik = BildirimText.Text.Trim();

        GonderButton.IsEnabled = false;

        try
        {
            string adSoyad = await Task.Run(() => _kullaniciService.AdSoyad);
            int sonuc = await _repository.GeriBildirimEkleAsync(
                projeId, adSoyad, modul, sayfa, icerik);

            if (sonuc > 0)
            {
                FormuTemizle();
                MesajGoster(
                    "Geri bildiriminiz başarıyla kaydedildi. Teşekkür ederiz!", true);
            }
            else
            {
                MesajGoster(
                    "Geri bildirim kaydedilemedi. Lütfen tekrar deneyiniz.", false);
            }
        }
        catch (Exception ex)
        {
            MesajGoster(
                $"Geri bildirim kaydedilirken bir hata oluştu: {ex.Message}", false);
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
        if (ProjeCombo.SelectedItem is not Proje)
        {
            MesajGoster(
                "Lütfen proje seçiniz. Proje yoksa Yönetim ekranından ekleyebilirsiniz.",
                false);
            ProjeCombo.Focus();
            return false;
        }

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
        MesajKutu.Background = (Brush)FindResource(
            basarili ? "BasariZeminBrush" : "HataZeminBrush");

        MesajText.Foreground = (Brush)FindResource(
            basarili ? "BasariMetinBrush" : "HataMetinBrush");

        MesajText.Text = metin;
        MesajKutu.Visibility = Visibility.Visible;
    }
}