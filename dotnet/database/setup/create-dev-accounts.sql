-- =====================================================================
-- Development / test accounts for Probeta Control.
-- Run ONCE as root on the project's MySQL instance (port 3308), and on any other
-- server the tests should cover (e.g. an 8.0 instance before a release).
-- Replace every CHANGE_ME_* with a password of your choice first, and put the
-- same values in dotnet/tests/test.env. Nothing here needs root afterwards.
--
--   "C:\Program Files\MySQL\MySQL Server 8.4\bin\mysql.exe" -u root -p --port=3308 < create-dev-accounts.sql
-- =====================================================================

-- Development database used by the app while developing.
CREATE DATABASE IF NOT EXISTS probeta_control
    CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci;

-- The app itself: read, insert, update. No DELETE, no DDL.
CREATE USER IF NOT EXISTS 'specimen_app'@'localhost' IDENTIFIED BY 'CHANGE_ME_APP';
GRANT SELECT, INSERT, UPDATE ON probeta_control.* TO 'specimen_app'@'localhost';

-- Schema owner: applies the numbered scripts (001_...sql) to probeta_control.
CREATE USER IF NOT EXISTS 'specimen_admin'@'localhost' IDENTIFIED BY 'CHANGE_ME_ADMIN';
GRANT ALL PRIVILEGES ON probeta_control.* TO 'specimen_admin'@'localhost';

-- Test runner: may only create, migrate and drop databases named probeta_test_<something>.
CREATE USER IF NOT EXISTS 'specimen_test'@'localhost' IDENTIFIED BY 'CHANGE_ME_TEST';
GRANT ALL PRIVILEGES ON `probeta\_test\_%`.* TO 'specimen_test'@'localhost';

-- Restricted account the tests run the app's queries as: the same rights the app has,
-- but only on the throwaway probeta_test_* databases.
CREATE USER IF NOT EXISTS 'specimen_app_test'@'localhost' IDENTIFIED BY 'CHANGE_ME_APP_TEST';
GRANT SELECT, INSERT, UPDATE ON `probeta\_test\_%`.* TO 'specimen_app_test'@'localhost';
