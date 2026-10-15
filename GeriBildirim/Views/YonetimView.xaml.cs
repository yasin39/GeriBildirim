using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using GeriBildirim.Data;
using GeriBildirim.Models;
using GeriBildirim.Services;
using Microsoft.Win32;

namespace GeriBildirim.Views;

/// <summary>
/// Geri bildirim yönetim ekranı: filtreleme, liste, satır işlemleri, detay,
/// PDF dışa aktarma ve özet sayıları. Filtre seçimleri bir sonraki açılış
/// için saklanır.
/// </summary>
public partial class YonetimView : UserControl
{
    private const string TumuMetni = "Tümü";

    private readonly GeriBildirimRepository _repository = new();
    private readonly FiltreAyarService _ayarService = new();
    private readonly PdfService _pdfService = new();

    /// <summary>Listeyi dolduran, son uygulanmış filtre.</summary>
    private GeriBildirimFilter _aktifFiltre;

    /// <summary>Bir satır işlemi sürerken ikinci işlemin başlamasını engeller.</summary>
    private bool _islemde;

    public YonetimView()
    {
        InitializeComponent();

        _aktifFiltre = _ayarService.Yukle();
        FiltreKontrolleriniHazirla();
    }

    /// <summary>Grid'de seçili kayıt. Seçim yoksa null.</summary>
    private GeriBildirimSatir? SeciliKayit => KayitGrid.SelectedItem as GeriBildirimSatir;

    // -----------------------------------------------------------------------
    // BAŞLANGIÇ
    // -----------------------------------------------------------------------

    /// <summary>
    /// Dropdown'ları doldurur ve kaydedilmiş filtre değerlerini kontrollere yazar.
    /// </summary>
    private void FiltreKontrolleriniHazirla()
    {
        var modulSecenekleri = new List<string> { TumuMetni };
        modulSecenekleri.AddRange(GeriBildirimMenu.ModulAdlari);
        ModulCombo.ItemsSource = modulSecenekleri;

        DurumCombo.ItemsSource = new List<DurumSecenek>
        {
            new("Tümü", GeriBildirimDurum.Tumu),
            new("Bekleyen", GeriBildirimDurum.Okunmamis),
            new("İşlem Yapılan", GeriBildirimDurum.Okunmus),
            new("Arşivlenmiş", GeriBildirimDurum.Arsivlenmis)
        };

        FiltreKontrolleriniDoldur(_aktifFiltre);
    }

    private void FiltreKontrolleriniDoldur(GeriBildirimFilter filtre)
    {
        bool modulListede = filtre.ModulAdi is not null && GeriBildirimMenu.ModulAdlari.Contains(filtre.ModulAdi);
        ModulCombo.SelectedItem = modulListede ? filtre.ModulAdi : TumuMetni;

        DurumCombo.SelectedValue = filtre.Durum;
        IlkTarihPicker.SelectedDate = filtre.IlkTarih;
        SonTarihPicker.SelectedDate = filtre.SonTarih;
    }

    /// <summary>
    /// Sekme her görüntülendiğinde listeyi son uygulanan filtreyle yeniler
    /// ve odağı grid'e verir (F5 ve Delete kısayolları için).
    /// </summary>
    private async void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        await ListeyiYukleAsync();
        KayitGrid.Focus();
    }

    // -----------------------------------------------------------------------
    // FİLTRE BUTONLARI
    // -----------------------------------------------------------------------

    private async void FiltreleButton_Click(object sender, RoutedEventArgs e)
    {
        _aktifFiltre = FiltreyiKontrollerdenOku();
        _ayarService.Kaydet(_aktifFiltre);
        DurumMesajiText.Text = string.Empty;
        await ListeyiYukleAsync();
    }

    private async void TemizleButton_Click(object sender, RoutedEventArgs e)
    {
        _aktifFiltre = new GeriBildirimFilter();
        FiltreKontrolleriniDoldur(_aktifFiltre);
        _ayarService.Kaydet(_aktifFiltre);
        DurumMesajiText.Text = string.Empty;
        await ListeyiYukleAsync();
    }

    // -----------------------------------------------------------------------
    // SATIR İŞLEMLERİ
    // -----------------------------------------------------------------------

    private void KayitGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ButonlariGuncelle();
    }

    private async void DetayButton_Click(object sender, RoutedEventArgs e)
    {
        await DetayAcAsync();
    }

    /// <summary>
    /// Yalnızca bir satıra çift tıklanınca detay açar; sütun başlığı veya
    /// boş alana çift tıklama yok sayılır.
    /// </summary>
    private async void KayitGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        var kaynak = e.OriginalSource as DependencyObject;

        if (kaynak is not null && ItemsControl.ContainerFromElement(KayitGrid, kaynak) is DataGridRow)
        {
            await DetayAcAsync();
        }
    }

    private async void OkunduButton_Click(object sender, RoutedEventArgs e)
    {
        await IslemYapAsync(OkunduDegistirAsync);
    }

    private async void ArsivButton_Click(object sender, RoutedEventArgs e)
    {
        await IslemYapAsync(ArsivDegistirAsync);
    }

    private async void SilButton_Click(object sender, RoutedEventArgs e)
    {
        await KesinSilAsync();
    }

    /// <summary>
    /// F5 listeyi yeniler. Delete, yalnızca odak grid'deyken seçili kaydı siler;
    /// böylece filtre kutularında yazarken yanlışlıkla silme olmaz.
    /// </summary>
    private async void UserControl_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.F5)
        {
            e.Handled = true;
            await ListeyiYukleAsync(SeciliKayit?.Id);
        }
        else if (e.Key == Key.Delete && KayitGrid.IsKeyboardFocusWithin)
        {
            e.Handled = true;
            await KesinSilAsync();
        }
    }

    /// <summary>
    /// Seçili kaydı veritabanından yeniden okuyup detay penceresinde gösterir.
    /// Kaydın durumunu değiştirmez.
    /// </summary>
    private async Task DetayAcAsync()
    {
        var secili = SeciliKayit;
        if (secili is null || _islemde)
        {
            return;
        }

        _islemde = true;

        try
        {
            var kayit = await _repository.GetGeriBildirimByIdAsync(secili.Id);

            if (kayit is null)
            {
                await ListeyiYukleAsync();
                DurumMesajiText.Text = "Geri bildirim bulunamadı.";
                return;
            }

            var pencere = new DetayWindow(kayit) { Owner = Window.GetWindow(this) };
            pencere.ShowDialog();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Detay açılırken bir hata oluştu:\n{ex.Message}",
                "Geri Bildirim", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            _islemde = false;
        }
    }

    /// <summary>
    /// Seçili kaydı, onay aldıktan sonra kalıcı olarak siler.
    /// Yalnızca arşivlenmiş kayıtlar silinebilir.
    /// </summary>
    private async Task KesinSilAsync()
    {
        var kayit = SeciliKayit;
        if (kayit is null)
        {
            return;
        }

        if (!kayit.Arsivlendi)
        {
            MessageBox.Show(
                "Kalıcı silme yalnızca arşivlenmiş kayıtlar için yapılabilir.\nÖnce kaydı arşivleyiniz.",
                "Geri Bildirim", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var onay = MessageBox.Show(
            $"#{kayit.Id} numaralı geri bildirim kalıcı olarak silinecek. Bu işlem geri alınamaz.\n\nDevam etmek istiyor musunuz?",
            "Kalıcı Sil", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No);

        if (onay != MessageBoxResult.Yes)
        {
            return;
        }

        await IslemYapAsync(KesinSilIsleminiYapAsync);
    }

    private async Task<string> OkunduDegistirAsync(int id)
    {
        bool? yeniDurum = await _repository.ToggleOkunduAsync(id);

        if (yeniDurum is null)
        {
            return "Geri bildirim bulunamadı.";
        }

        return yeniDurum.Value
            ? "Geri bildirim 'İşlem Yapıldı' olarak işaretlendi."
            : "Geri bildirim tekrar 'Bekliyor' durumuna alındı.";
    }

    private async Task<string> ArsivDegistirAsync(int id)
    {
        bool? yeniDurum = await _repository.ToggleArsivAsync(id);

        if (yeniDurum is null)
        {
            return "Geri bildirim bulunamadı.";
        }

        return yeniDurum.Value
            ? "Geri bildirim arşivlendi."
            : "Geri bildirim arşivden çıkarıldı.";
    }

    private async Task<string> KesinSilIsleminiYapAsync(int id)
    {
        int sonuc = await _repository.GeriBildirimSilAsync(id);
        return sonuc > 0 ? "Kayıt kalıcı olarak silindi." : "Geri bildirim bulunamadı.";
    }

    /// <summary>
    /// Seçili kayıt üzerinde verilen işlemi çalıştırır, sonucu durum çubuğunda
    /// gösterir ve listeyi yenileyip aynı kaydı yeniden seçer.
    /// </summary>
    private async Task IslemYapAsync(Func<int, Task<string>> islem)
    {
        var kayit = SeciliKayit;
        if (kayit is null || _islemde)
        {
            return;
        }

        _islemde = true;

        try
        {
            string mesaj = await islem(kayit.Id);
            await ListeyiYukleAsync(kayit.Id);
            DurumMesajiText.Text = mesaj;
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"İşlem sırasında bir hata oluştu:\n{ex.Message}",
                "Geri Bildirim", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            _islemde = false;
        }
    }

    /// <summary>
    /// Seçili kaydın durumuna göre işlem butonlarının metnini ve etkinliğini ayarlar.
    /// </summary>
    private void ButonlariGuncelle()
    {
        var kayit = SeciliKayit;

        DetayButton.IsEnabled = kayit is not null;

        if (kayit is null)
        {
            OkunduButton.Content = "İşlem Yapıldı Yap";
            ArsivButton.Content = "Arşivle";
            OkunduButton.IsEnabled = false;
            ArsivButton.IsEnabled = false;
            SilButton.IsEnabled = false;
            return;
        }

        OkunduButton.Content = kayit.Okundu ? "Bekliyor Yap" : "İşlem Yapıldı Yap";
        ArsivButton.Content = kayit.Arsivlendi ? "Arşivden Çıkar" : "Arşivle";
        OkunduButton.IsEnabled = !kayit.Arsivlendi;
        ArsivButton.IsEnabled = true;
        SilButton.IsEnabled = kayit.Arsivlendi;
    }

    // -----------------------------------------------------------------------
    // PDF
    // -----------------------------------------------------------------------

    /// <summary>
    /// Son uygulanan filtreyle listelenen kayıtları PDF olarak kaydeder.
    /// Sorgu export anında yeniden çalışır.
    /// </summary>
    private async void PdfButton_Click(object sender, RoutedEventArgs e)
    {
        if (_islemde)
        {
            return;
        }

        _islemde = true;
        PdfButton.IsEnabled = false;

        try
        {
            var kayitlar = await _repository.GetGeriBildirimlerAsync(_aktifFiltre);

            if (kayitlar.Count == 0)
            {
                DurumMesajiText.Text = "Görüntülenen listede geri bildirim bulunmuyor.";
                return;
            }

            var dialog = new SaveFileDialog
            {
                Title = "Geri Bildirim Listesini Kaydet",
                Filter = "PDF dosyası (*.pdf)|*.pdf",
                DefaultExt = ".pdf",
                AddExtension = true,
                FileName = $"GeriBildirimler_{DateTime.Now:yyyyMMdd_HHmmss}.pdf"
            };

            if (dialog.ShowDialog(Window.GetWindow(this)) != true)
            {
                return;
            }

            var satirlar = kayitlar
                .Select((k, i) => new List<string>
                {
                    (i + 1).ToString(),
                    k.ModulAdi,
                    k.SayfaAdi,
                    k.BildirimIcerigi
                })
                .ToList();

            byte[] pdf = await Task.Run(() => _pdfService.GenerateGeriBildirimReport(satirlar));
            await File.WriteAllBytesAsync(dialog.FileName, pdf);

            DurumMesajiText.Text = $"{kayitlar.Count} kayıt PDF olarak kaydedildi.";
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"PDF oluşturulurken bir hata oluştu:\n{ex.Message}",
                "Geri Bildirim", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            _islemde = false;
            PdfButton.IsEnabled = true;
        }
    }

    // -----------------------------------------------------------------------
    // LİSTE
    // -----------------------------------------------------------------------

    private GeriBildirimFilter FiltreyiKontrollerdenOku()
    {
        return new GeriBildirimFilter
        {
            ModulAdi = ModulCombo.SelectedIndex > 0 ? (string)ModulCombo.SelectedItem : null,
            Durum = DurumCombo.SelectedValue is GeriBildirimDurum durum ? durum : GeriBildirimDurum.Tumu,
            IlkTarih = IlkTarihPicker.SelectedDate,
            SonTarih = SonTarihPicker.SelectedDate
        };
    }

    /// <summary>
    /// Son uygulanan filtreyle kayıtları çeker, grid'i ve durum çubuğunu günceller.
    /// </summary>
    /// <param name="seciliId">Yenilemeden sonra yeniden seçilecek kaydın Id'si (varsa).</param>
    private async Task ListeyiYukleAsync(int? seciliId = null)
    {
        try
        {
            var kayitlar = await _repository.GetGeriBildirimlerAsync(_aktifFiltre);

            KayitGrid.ItemsSource = kayitlar;
            BosMesaj.Visibility = kayitlar.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

            ToplamText.Text = $"Toplam: {kayitlar.Count}";
            BekleyenText.Text = $"Bekleyen: {kayitlar.Count(k => !k.Okundu && !k.Arsivlendi)}";
            IslemYapilanText.Text = $"İşlem Yapılan: {kayitlar.Count(k => k.Okundu && !k.Arsivlendi)}";
            ArsivText.Text = $"Arşiv: {kayitlar.Count(k => k.Arsivlendi)}";

            if (seciliId.HasValue)
            {
                var kayit = kayitlar.FirstOrDefault(k => k.Id == seciliId.Value);
                if (kayit is not null)
                {
                    KayitGrid.SelectedItem = kayit;
                    KayitGrid.ScrollIntoView(kayit);
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Geri bildirimler yüklenirken bir hata oluştu:\n{ex.Message}",
                "Geri Bildirim", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}