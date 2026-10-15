namespace GeriBildirim.Models;

/// <summary>
/// Yönetim ekranındaki durum filtresinin değerleri.
/// </summary>
public enum GeriBildirimDurum
{
    /// <summary>Tüm kayıtlar (filtre yok).</summary>
    Tumu = -1,

    /// <summary>Okunmamış ve arşivlenmemiş (bekleyen).</summary>
    Okunmamis = 0,

    /// <summary>Okunmuş ve arşivlenmemiş (işlem yapılmış).</summary>
    Okunmus = 1,

    /// <summary>Arşivlenmiş.</summary>
    Arsivlenmis = 2
}