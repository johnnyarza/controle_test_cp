-- 013: QR labels. Each probeta gets a random code for the link in its QR (not its number, so nobody can
-- guess the address of other results). The app makes the code when it saves a new probeta; existing
-- probetas get one here. 16 hex digits = 64 random bits.

ALTER TABLE Specimens
    ADD COLUMN PublicCode CHAR(16) NULL AFTER CompressionTestId;

UPDATE Specimens SET PublicCode = UPPER(HEX(RANDOM_BYTES(8))) WHERE PublicCode IS NULL;

ALTER TABLE Specimens
    MODIFY COLUMN PublicCode CHAR(16) NOT NULL COMMENT 'Random code in the QR label link',
    ADD UNIQUE KEY UX_Specimens_PublicCode (PublicCode);

-- Label settings, shared by every PC (Configuración → Etiquetas). The server name starts empty: labels
-- can't be printed until an administrator sets it. The sheet starts as A4, 3 × 8 labels of 70 × 37 mm.
INSERT INTO AppSettings (SettingKey, TextValue) VALUES
    ('LabelServerName', NULL),
    ('LabelSheet', '3;8;70;37;0.5;0;0;0');

INSERT INTO SchemaVersion (Version) VALUES (13);
