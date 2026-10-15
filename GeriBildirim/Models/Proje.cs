using System.Text.Json;

namespace GeriBildirim.Models;

public class Proje
{
    public int Id { get; set; }
    public string Ad { get; set; } = string.Empty;
    public string MenuJson { get; set; } = "[]";

    public bool Silindi { get; set; }

    public string GorunenAd => Silindi ? $"{Ad} (Silindi)" : Ad;

    public List<MenuModulu> Moduller => JsonSerializer.Deserialize<List<MenuModulu>>(
        MenuJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];
}