using System.Windows;
using GeriBildirim.Data;
using GeriBildirim.Models;

namespace GeriBildirim.Views;

public partial class ProjeEkleWindow : Window
{
    public int EklenenProjeId { get; private set; }

    private readonly Proje? _proje;
    private bool _kaydediliyor;

    public ProjeEkleWindow() : this(null)
    {
    }

    public ProjeEkleWindow(Proje? proje)
    {
        InitializeComponent();
        _proje = proje;

        if (proje is not null)
        {
            Title = "Proje Düzenle";
            AdText.Text = proje.Ad;

            MenuText.Text = string.Join(
                Environment.NewLine,
                proje.Moduller.SelectMany(
                    m => m.Sayfalar.Select(s => $"{m.Modul} | {s}")));
        }

        Closing += (_, e) => e.Cancel = _kaydediliyor;
        Loaded += (_, _) => AdText.Focus();
    }

    private async void Kaydet_Click(object sender, RoutedEventArgs e)
    {
        if (_kaydediliyor) return;

        _kaydediliyor = true;
        KaydetButton.IsEnabled = IptalButton.IsEnabled = false;
        AdText.IsEnabled = MenuText.IsEnabled = false;
        HataText.Text = string.Empty;

        try
        {
            var repository = new ProjeRepository();

            if (_proje is null)
            {
                EklenenProjeId = await repository.EkleAsync(
                    AdText.Text, MenuText.Text);
            }
            else
            {
                await repository.GuncelleAsync(
                    _proje, AdText.Text, MenuText.Text);

                EklenenProjeId = _proje.Id;
            }

            _kaydediliyor = false;
            DialogResult = true;
        }
        catch (Exception ex)
        {
            HataText.Text = $"Proje kaydedilemedi: {ex.Message}";
        }
        finally
        {
            _kaydediliyor = false;
            KaydetButton.IsEnabled = IptalButton.IsEnabled = true;
            AdText.IsEnabled = MenuText.IsEnabled = true;
        }
    }
}