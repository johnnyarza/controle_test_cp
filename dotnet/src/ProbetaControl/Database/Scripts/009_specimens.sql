-- 009: specimens (probetas). Age, density and strength are calculated in the app, not stored.
-- RuptureLoadTon IS NULL means "not tested yet" (the legacy app used 0).

CREATE TABLE Specimens (
    Id                INT            NOT NULL AUTO_INCREMENT,
    CompressionTestId INT            NOT NULL,
    Code              VARCHAR(100)   NULL     COMMENT 'Free description shown as Descripcion',
    Slump             DECIMAL(6, 2)  NULL     COMMENT 'cm',
    MoldDate          DATE           NOT NULL,
    RuptureDate       DATE           NOT NULL,
    DiameterCm        DECIMAL(6, 2)  NOT NULL,
    HeightCm          DECIMAL(6, 2)  NOT NULL,
    WeightKg          DECIMAL(8, 3)  NOT NULL,
    RuptureLoadTon    DECIMAL(8, 2)  NULL,
    IsLocked          TINYINT(1)     NOT NULL DEFAULT 0 COMMENT 'Set once the rupture is recorded',
    IsArchived        TINYINT(1)     NOT NULL DEFAULT 0,
    RowVersion        INT            NOT NULL DEFAULT 1,
    CreatedAt         DATETIME       NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt         DATETIME       NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (Id),
    KEY IX_Specimens_CompressionTestId (CompressionTestId, IsArchived),
    KEY IX_Specimens_Pending (RuptureLoadTon, RuptureDate),
    KEY IX_Specimens_MoldDate (MoldDate),
    CONSTRAINT FK_Specimens_CompressionTest FOREIGN KEY (CompressionTestId) REFERENCES CompressionTests (Id),
    CONSTRAINT CK_Specimens_Slump CHECK (Slump IS NULL OR Slump >= 0),
    CONSTRAINT CK_Specimens_Diameter CHECK (DiameterCm >= 0),
    CONSTRAINT CK_Specimens_Height CHECK (HeightCm >= 0),
    CONSTRAINT CK_Specimens_Weight CHECK (WeightKg >= 0),
    CONSTRAINT CK_Specimens_RuptureLoad CHECK (RuptureLoadTon IS NULL OR RuptureLoadTon >= 0)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_0900_ai_ci;

INSERT INTO SchemaVersion (Version) VALUES (9);
