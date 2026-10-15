using System.DirectoryServices.AccountManagement;

namespace GeriBildirim.Services;

/// <summary>
/// Programı açan Windows kullanıcısının görünen adını verir.
/// </summary>
public class KullaniciService
{
    private const int AdSoyadMaxUzunluk = 150;

    private static string? _adSoyad;

    /// <summary>
    /// Kullanıcının ad soyadı. Önce AD DisplayName denenir, bulunamazsa
    /// Windows oturum adı döner. GeriBildirimler.KullaniciAdSoyad sütununa yazılır.
    /// </summary>
    public string AdSoyad
    {
        get
        {
            _adSoyad ??= AdSoyadiBul();
            return _adSoyad;
        }
    }

    private static string AdSoyadiBul()
    {
        string sonuc = Environment.UserName;

        if (EtkiAlanindaMi())
        {
            try
            {
                using var baglam = new PrincipalContext(ContextType.Domain);
                using var kullanici = UserPrincipal.FindByIdentity(
                    baglam, IdentityType.SamAccountName, Environment.UserName);

                if (kullanici is not null && !string.IsNullOrWhiteSpace(kullanici.DisplayName))
                {
                    sonuc = kullanici.DisplayName.Trim();
                }
            }
            catch (Exception)
            {
                // AD'ye ulaşılamadı; oturum adı kullanılır.
            }
        }

        return sonuc.Length > AdSoyadMaxUzunluk
            ? sonuc.Substring(0, AdSoyadMaxUzunluk)
            : sonuc;
    }

    /// <summary>
    /// Kullanıcı etki alanı hesabıyla mı oturum açmış, yoksa yerel hesapla mı?
    /// Yerel hesapta alan adı makine adına eşittir.
    /// </summary>
    private static bool EtkiAlanindaMi()
    {
        return !string.Equals(
            Environment.UserDomainName,
            Environment.MachineName,
            StringComparison.OrdinalIgnoreCase);
    }
}