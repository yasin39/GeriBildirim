namespace GeriBildirim.Models;

/// <summary>
/// Yönetim ekranındaki filtre kriterleri. Boş bırakılan alanlar
/// SQL sorgusuna dahil edilmez.
/// </summary>
public class GeriBildirimFilter
{
    public string? ModulAdi { get; set; }

    public GeriBildirimDurum Durum { get; set; } = GeriBildirimDurum.Tumu;

    public DateTime? IlkTarih { get; set; }

    public DateTime? SonTarih { get; set; }
}