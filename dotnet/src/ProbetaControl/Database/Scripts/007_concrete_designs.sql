-- 007: concrete designs (dosificaciones) and their materials.
-- A design has up to 8 material slots. Rows are never deleted: emptying a slot sets
-- MaterialId and QuantityKgPerM3 to NULL (the app user has no DELETE privilege).

CREATE TABLE ConcreteDesigns (
    Id          INT           NOT NULL AUTO_INCREMENT,
    Description VARCHAR(150)  NOT NULL,
    Fck         DECIMAL(8, 2) NOT NULL COMMENT 'Design strength, kg/cm2',
    Slump       DECIMAL(6, 2) NULL     COMMENT 'cm',
    IsArchived  TINYINT(1)    NOT NULL DEFAULT 0,
    RowVersion  INT           NOT NULL DEFAULT 1,
    CreatedAt   DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt   DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (Id),
    UNIQUE KEY UX_ConcreteDesigns_Description (Description),
    CONSTRAINT CK_ConcreteDesigns_Description CHECK (CHAR_LENGTH(TRIM(Description)) > 0),
    CONSTRAINT CK_ConcreteDesigns_Fck CHECK (Fck >= 0),
    CONSTRAINT CK_ConcreteDesigns_Slump CHECK (Slump IS NULL OR Slump >= 0)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_0900_ai_ci;

CREATE TABLE ConcreteDesignMaterials (
    ConcreteDesignId INT            NOT NULL,
    Position         TINYINT        NOT NULL COMMENT 'Slot 1..8, order shown on screen and reports',
    MaterialId       INT            NULL,
    QuantityKgPerM3  DECIMAL(10, 3) NULL,
    UpdatedAt        DATETIME       NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (ConcreteDesignId, Position),
    KEY IX_ConcreteDesignMaterials_MaterialId (MaterialId),
    CONSTRAINT FK_ConcreteDesignMaterials_Design FOREIGN KEY (ConcreteDesignId) REFERENCES ConcreteDesigns (Id),
    CONSTRAINT FK_ConcreteDesignMaterials_Material FOREIGN KEY (MaterialId) REFERENCES Materials (Id),
    CONSTRAINT CK_ConcreteDesignMaterials_Position CHECK (Position BETWEEN 1 AND 8),
    CONSTRAINT CK_ConcreteDesignMaterials_Quantity CHECK (QuantityKgPerM3 IS NULL OR QuantityKgPerM3 >= 0),
    CONSTRAINT CK_ConcreteDesignMaterials_Slot CHECK ((MaterialId IS NULL) = (QuantityKgPerM3 IS NULL))
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_0900_ai_ci;

INSERT INTO SchemaVersion (Version) VALUES (7);
