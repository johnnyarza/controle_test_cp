-- 001: schema version tracking.
-- Every script ends by inserting its own number here. The app compares MAX(Version)
-- with the version it was built for and refuses to start on a mismatch.

CREATE TABLE SchemaVersion (
    Version   INT      NOT NULL,
    AppliedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (Version)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_0900_ai_ci;

INSERT INTO SchemaVersion (Version) VALUES (1);
