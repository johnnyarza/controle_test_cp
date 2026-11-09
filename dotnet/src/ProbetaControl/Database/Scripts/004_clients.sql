-- 004: clients. Also used as concrete providers of a compression test (as in the legacy app).

CREATE TABLE Clients (
    Id         INT          NOT NULL AUTO_INCREMENT,
    Name       VARCHAR(150) NOT NULL,
    Phone      VARCHAR(50)  NULL,
    Address    VARCHAR(200) NULL,
    Email      VARCHAR(150) NULL,
    IsArchived TINYINT(1)   NOT NULL DEFAULT 0,
    RowVersion INT          NOT NULL DEFAULT 1,
    CreatedAt  DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt  DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (Id),
    UNIQUE KEY UX_Clients_Name (Name),
    CONSTRAINT CK_Clients_Name CHECK (CHAR_LENGTH(TRIM(Name)) > 0)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_0900_ai_ci;

INSERT INTO SchemaVersion (Version) VALUES (4);
