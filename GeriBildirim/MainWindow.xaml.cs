using System.Windows;
using GeriBildirim.Services;

namespace GeriBildirim;

/// <summary>
/// Ana pencere. Üstte kullanıcı şeridi, altta Gönder ve Yönetim sekmelerini barındırır.
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Pencere açıldıktan sonra kullanıcı adını arka planda okur ve şeritte gösterir.
    /// </summary>
    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        string adSoyad = await Task.Run(() => new KullaniciService().AdSoyad);
        KullaniciText.Text = adSoyad;
    }
}