-- 002: application users (login). Passwords are BCrypt hashes.
-- Role values are stored in English: 'admin' | 'operator'.

CREATE TABLE Users (
    Id                 INT          NOT NULL AUTO_INCREMENT,
    UserName           VARCHAR(50)  NOT NULL,
    FullName           VARCHAR(100) NULL,
    PasswordHash       CHAR(60)     NOT NULL,
    Role               VARCHAR(20)  NOT NULL,
    MustChangePassword TINYINT(1)   NOT NULL DEFAULT 0,
    IsArchived         TINYINT(1)   NOT NULL DEFAULT 0,
    RowVersion         INT          NOT NULL DEFAULT 1,
    CreatedAt          DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt          DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (Id),
    UNIQUE KEY UX_Users_UserName (UserName),
    CONSTRAINT CK_Users_Role CHECK (Role IN ('admin', 'operator'))
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_0900_ai_ci;

-- First administrator: admin / admin, must choose a new password at first login.
INSERT INTO Users (UserName, FullName, PasswordHash, Role, MustChangePassword)
VALUES ('admin', 'Administrador', '$2a$11$amif/zwXId9DjYCv0ONKhe0cg5aWo9e2YA67ffBGaMYP9Fd.uzTJm', 'admin', 1);

INSERT INTO SchemaVersion (Version) VALUES (2);
