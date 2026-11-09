-- 010: settings shared by every PC (report logo and stamp images).
-- Rows are created here so the app only ever needs UPDATE to change them.

CREATE TABLE AppSettings (
    SettingKey  VARCHAR(64)  NOT NULL,
    TextValue   VARCHAR(500) NULL,
    BinaryValue MEDIUMBLOB   NULL COMMENT 'Image bytes, max 16 MB',
    ContentType VARCHAR(100) NULL COMMENT 'e.g. image/png',
    RowVersion  INT          NOT NULL DEFAULT 1,
    UpdatedAt   DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (SettingKey)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_0900_ai_ci;

INSERT INTO AppSettings (SettingKey) VALUES ('ReportLogo'), ('ReportStamp');

INSERT INTO SchemaVersion (Version) VALUES (10);
