-- 006: materials used in concrete designs.

CREATE TABLE Materials (
    Id         INT          NOT NULL AUTO_INCREMENT,
    Name       VARCHAR(150) NOT NULL,
    ProviderId INT          NULL,
    IsArchived TINYINT(1)   NOT NULL DEFAULT 0,
    RowVersion INT          NOT NULL DEFAULT 1,
    CreatedAt  DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt  DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (Id),
    KEY IX_Materials_ProviderId (ProviderId),
    CONSTRAINT FK_Materials_Provider FOREIGN KEY (ProviderId) REFERENCES Providers (Id),
    CONSTRAINT CK_Materials_Name CHECK (CHAR_LENGTH(TRIM(Name)) > 0)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_0900_ai_ci;

INSERT INTO SchemaVersion (Version) VALUES (6);
