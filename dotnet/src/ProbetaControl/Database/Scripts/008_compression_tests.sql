-- 008: compression tests (ensayos): one per client project and pour, holding many specimens.

CREATE TABLE CompressionTests (
    Id                 INT          NOT NULL AUTO_INCREMENT,
    ClientId           INT          NOT NULL,
    ConcreteProviderId INT          NOT NULL COMMENT 'Supplier of the concrete; a row of Clients',
    ConcreteDesignId   INT          NOT NULL,
    ProjectName        VARCHAR(200) NOT NULL COMMENT 'Obra / proyecto',
    Address            VARCHAR(200) NOT NULL,
    TestDate           DATE         NOT NULL,
    IsArchived         TINYINT(1)   NOT NULL DEFAULT 0,
    RowVersion         INT          NOT NULL DEFAULT 1,
    CreatedAt          DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt          DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (Id),
    KEY IX_CompressionTests_ClientId (ClientId),
    KEY IX_CompressionTests_ConcreteProviderId (ConcreteProviderId),
    KEY IX_CompressionTests_ConcreteDesignId (ConcreteDesignId),
    KEY IX_CompressionTests_TestDate (TestDate),
    CONSTRAINT FK_CompressionTests_Client FOREIGN KEY (ClientId) REFERENCES Clients (Id),
    CONSTRAINT FK_CompressionTests_ConcreteProvider FOREIGN KEY (ConcreteProviderId) REFERENCES Clients (Id),
    CONSTRAINT FK_CompressionTests_ConcreteDesign FOREIGN KEY (ConcreteDesignId) REFERENCES ConcreteDesigns (Id)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_0900_ai_ci;

INSERT INTO SchemaVersion (Version) VALUES (8);
