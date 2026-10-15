namespace GeriBildirim.Models;

/// <summary>
/// Durum filtresi dropdown'unda gösterilen metin ve ona karşılık gelen değer.
/// </summary>
public record DurumSecenek(string Metin, GeriBildirimDurum Deger);