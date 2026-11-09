-- 003: audit log. Every data change writes one row here in the same transaction.
-- Action values: 'insert' | 'update' | 'archive' | 'restore'.
-- Changes holds {"field": {"old": ..., "new": ...}} for updates, the full record for inserts.

CREATE TABLE AuditLog (
    Id          BIGINT      NOT NULL AUTO_INCREMENT,
    OccurredAt  DATETIME(3) NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
    UserId      INT         NULL,
    MachineName VARCHAR(64) NOT NULL,
    TableName   VARCHAR(64) NOT NULL,
    RecordId    INT         NOT NULL,
    Action      VARCHAR(20) NOT NULL,
    Changes     JSON        NULL,
    PRIMARY KEY (Id),
    KEY IX_AuditLog_Record (TableName, RecordId),
    KEY IX_AuditLog_OccurredAt (OccurredAt),
    KEY IX_AuditLog_UserId (UserId),
    CONSTRAINT FK_AuditLog_User FOREIGN KEY (UserId) REFERENCES Users (Id),
    CONSTRAINT CK_AuditLog_Action CHECK (Action IN ('insert', 'update', 'archive', 'restore'))
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_0900_ai_ci;

INSERT INTO SchemaVersion (Version) VALUES (3);
