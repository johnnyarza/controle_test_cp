-- 014: photos of probetas (phase 9 phone page, and the desktop probeta dialog). Made now so the accounts and the
-- installer don't change later; nothing in the app uses it yet.
--
-- Photos are stored here (not as files) and resized to about 1600 px JPEG (~300 KB) before saving. The image is
-- kept out of Specimens so lists stay fast: never SELECT Photo in a list.
-- Deleting a photo really deletes the row, to save server space (an exception to "nothing is deleted"). The app
-- accounts have no DELETE right: they call DeleteSpecimenPhoto, which writes the audit row and deletes, as its
-- definer (the schema owner). Call it inside the caller's transaction.
--
-- Apply it with the program ("Actualizar base de datos") or DemoSeed, which send the whole script at once. Editor
-- SQL runners and the mysql client split at every ';' and break the procedure's BEGIN … END.

CREATE TABLE SpecimenPhotos (
    Id          INT          NOT NULL AUTO_INCREMENT,
    SpecimenId  INT          NOT NULL,
    Photo       MEDIUMBLOB   NOT NULL,
    ContentType VARCHAR(20)  NOT NULL DEFAULT 'image/jpeg',
    SizeBytes   INT          AS (OCTET_LENGTH(Photo)) STORED,
    WidthPx     INT          NOT NULL,
    HeightPx    INT          NOT NULL,
    CreatedAt   DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CreatedBy   INT          NOT NULL COMMENT 'Users.Id (the phone page signs in with the same users)',
    PRIMARY KEY (Id),
    KEY IX_SpecimenPhotos_Specimen (SpecimenId),
    CONSTRAINT FK_SpecimenPhotos_Specimen FOREIGN KEY (SpecimenId) REFERENCES Specimens (Id),
    CONSTRAINT FK_SpecimenPhotos_User FOREIGN KEY (CreatedBy) REFERENCES Users (Id),
    CONSTRAINT CK_SpecimenPhotos_ContentType CHECK (ContentType IN ('image/jpeg', 'image/png')),
    CONSTRAINT CK_SpecimenPhotos_Size CHECK (SizeBytes BETWEEN 1 AND 2097152),
    CONSTRAINT CK_SpecimenPhotos_Pixels CHECK (WidthPx BETWEEN 1 AND 10000 AND HeightPx BETWEEN 1 AND 10000)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_0900_ai_ci;

-- The audit log gets a 'delete' action (only photos are ever deleted).
ALTER TABLE AuditLog DROP CHECK CK_AuditLog_Action;
ALTER TABLE AuditLog ADD CONSTRAINT CK_AuditLog_Action CHECK (Action IN ('insert', 'update', 'archive', 'restore', 'delete'));

-- Deletes one photo and records who, when, from which PC / phone, which probeta and the size (not the image).
CREATE PROCEDURE DeleteSpecimenPhoto(IN p_photo_id INT, IN p_user_id INT, IN p_machine_name VARCHAR(64))
    MODIFIES SQL DATA
    SQL SECURITY DEFINER
    COMMENT 'The only way to delete a photo: writes an AuditLog row, then deletes'
BEGIN
    DECLARE v_record JSON DEFAULT NULL;

    SELECT JSON_OBJECT(
               'SpecimenId', p.SpecimenId,
               'ContentType', p.ContentType,
               'SizeBytes', p.SizeBytes,
               'WidthPx', p.WidthPx,
               'HeightPx', p.HeightPx,
               'CreatedAt', DATE_FORMAT(p.CreatedAt, '%Y-%m-%d %H:%i:%s'),
               'CreatedBy', p.CreatedBy)
      INTO v_record
      FROM SpecimenPhotos p
     WHERE p.Id = p_photo_id
       FOR UPDATE;

    IF v_record IS NULL THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'La foto ya no existe.';
    END IF;

    INSERT INTO AuditLog (UserId, MachineName, TableName, RecordId, Action, Changes)
    VALUES (p_user_id, p_machine_name, 'SpecimenPhotos', p_photo_id, 'delete', v_record);

    DELETE FROM SpecimenPhotos WHERE Id = p_photo_id;
END;

INSERT INTO SchemaVersion (Version) VALUES (14);
