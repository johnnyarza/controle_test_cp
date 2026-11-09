-- =====================================================================
-- Development / test accounts for Probeta Control.
-- Run ONCE as root on EACH development server:
--   MySQL 8.4  (port 3306)
--   MySQL 8.0  (port 3308)
-- Replace every CHANGE_ME_* with a password of your choice first, and put the
-- same values in dotnet/tests/test.env. Nothing here needs root afterwards.
--
--   "C:\Program Files\MySQL\MySQL Server 8.4\bin\mysql.exe" -u root -p --port=3306 < create-dev-accounts.sql
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

-- Test runner: may only create/drop databases named probeta_test_<something>,
-- and grant rights on them to specimen_app_test.
CREATE USER IF NOT EXISTS 'specimen_test'@'localhost' IDENTIFIED BY 'CHANGE_ME_TEST';
GRANT ALL PRIVILEGES ON `probeta\_test\_%`.* TO 'specimen_test'@'localhost' WITH GRANT OPTION;

-- Restricted account the tests run the app's queries as. It has no rights of its own:
-- each test run grants it SELECT, INSERT, UPDATE on its throwaway database, then revokes.
CREATE USER IF NOT EXISTS 'specimen_app_test'@'localhost' IDENTIFIED BY 'CHANGE_ME_APP_TEST';
