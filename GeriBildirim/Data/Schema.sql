CREATE TABLE Projeler (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Ad TEXT COLLATE SQLSERVER_CI_AS NOT NULL CHECK (length(Ad) <= 150),
    MenuJson TEXT COLLATE SQLSERVER_CI_AS NOT NULL CHECK (json_valid(MenuJson)),
    Silindi INTEGER NOT NULL DEFAULT 0 CHECK (Silindi IN (0, 1)),
    CONSTRAINT UQ_Projeler_Ad UNIQUE (Ad)
);

CREATE TABLE GeriBildirimler (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    KullaniciAdSoyad TEXT COLLATE SQLSERVER_CI_AS NOT NULL CHECK (length(KullaniciAdSoyad) <= 150),
    ModulAdi TEXT COLLATE SQLSERVER_CI_AS NOT NULL CHECK (length(ModulAdi) <= 100),
    SayfaAdi TEXT COLLATE SQLSERVER_CI_AS NOT NULL CHECK (length(SayfaAdi) <= 100),
    BildirimIcerigi TEXT COLLATE SQLSERVER_CI_AS NOT NULL CHECK (length(BildirimIcerigi) <= 3000),
    KayitTarihi TEXT NOT NULL DEFAULT (strftime('%Y-%m-%d %H:%M:%f', 'now', 'localtime')),
    Okundu INTEGER NOT NULL DEFAULT 0 CHECK (Okundu IN (0, 1)),
    Arsivlendi INTEGER NOT NULL DEFAULT 0 CHECK (Arsivlendi IN (0, 1)),
    ProjeId INTEGER NOT NULL DEFAULT 1 REFERENCES Projeler(Id)
);

CREATE INDEX IX_GeriBildirimler_Durum_Tarih ON GeriBildirimler (Arsivlendi, Okundu, KayitTarihi DESC);
CREATE INDEX IX_GeriBildirimler_ProjeId_KayitTarihi ON GeriBildirimler (ProjeId, KayitTarihi DESC);
PRAGMA user_version = 1;
