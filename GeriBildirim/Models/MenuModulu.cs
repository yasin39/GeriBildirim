namespace GeriBildirim.Models;

/// <summary>
/// Bir modül ve o modüle ait sayfa adları. menu.json dosyasındaki
/// her kayıt bu sınıfa okunur.
/// </summary>
public class MenuModulu
{
    public string Modul { get; set; } = string.Empty;

    public List<string> Sayfalar { get; set; } = [];
}