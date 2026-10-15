using System.Windows;
using System.Windows.Controls;
using GeriBildirim.Data;
using GeriBildirim.Models;

namespace GeriBildirim.Views;

public partial class ProjeleriYonetWindow : Window
{
    private readonly ProjeRepository _repository = new();
    private bool _islemde;

    private Proje? SeciliProje => ProjeGrid.SelectedItem as Proje;

    public ProjeleriYonetWindow()
    {
        InitializeComponent();
        Closing += (_, e) => e.Cancel = _islemde;
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
        => await ListeyiYukleAsync();

    private async void Yenile_Click(object sender, RoutedEventArgs e)
        => await ListeyiYukleAsync();

    private void ProjeGrid_SelectionChanged(
        object sender, SelectionChangedEventArgs e)
        => ButonlariGuncelle();

    private void ButonlariGuncelle()
    {
        EkleButton.IsEnabled = YenileButton.IsEnabled = !_islemde;

        DuzenleButton.IsEnabled = SilButton.IsEnabled =
            !_islemde && SeciliProje is not null;

        ProjeGrid.IsEnabled = !_islemde;
    }

    private async Task ListeyiYukleAsync(int? seciliId = null)
    {
        if (_islemde) return;

        seciliId ??= SeciliProje?.Id;
        _islemde = true;
        ButonlariGuncelle();

        try
        {
            var projeler = await _repository.GetProjelerAsync();

            ProjeGrid.ItemsSource = projeler;
            ProjeGrid.SelectedItem = projeler.FirstOrDefault(
                p => p.Id == seciliId);

            DurumText.Text = $"{projeler.Count} aktif proje.";
        }
        catch (Exception ex)
        {
            ProjeGrid.ItemsSource = null;
            DurumText.Text = $"Projeler yüklenemedi: {ex.Message}";
        }
        finally
        {
            _islemde = false;
            ButonlariGuncelle();
        }
    }

    private async void Ekle_Click(object sender, RoutedEventArgs e)
    {
        if (_islemde) return;

        var pencere = new ProjeEkleWindow { Owner = this };

        if (pencere.ShowDialog() == true)
        {
            await ListeyiYukleAsync(pencere.EklenenProjeId);
        }
    }

    private async void Duzenle_Click(object sender, RoutedEventArgs e)
    {
        if (_islemde || SeciliProje is not Proje proje) return;

        var pencere = new ProjeEkleWindow(proje) { Owner = this };

        if (pencere.ShowDialog() == true)
        {
            await ListeyiYukleAsync(proje.Id);
        }
    }

    private async void Sil_Click(object sender, RoutedEventArgs e)
    {
        if (_islemde || SeciliProje is not Proje proje) return;

        var onay = MessageBox.Show(
            this,
            $"'{proje.Ad}' projesi silinsin mi?\n\n" +
            "Yeni bildirimlerde seçilemeyecek. Mevcut geri bildirimler korunacak.",
            "Projeyi Sil",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning,
            MessageBoxResult.No);

        if (onay != MessageBoxResult.Yes) return;

        _islemde = true;
        ButonlariGuncelle();

        bool silindi = false;

        try
        {
            await _repository.SilAsync(proje.Id);
            silindi = true;
        }
        catch (Exception ex)
        {
            DurumText.Text = $"Proje silinemedi: {ex.Message}";
        }
        finally
        {
            _islemde = false;
            ButonlariGuncelle();
        }

        if (silindi)
        {
            await ListeyiYukleAsync();
        }
    }
}