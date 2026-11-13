-- =====================================================================
-- Demo database for Probeta Control: run ONCE as root on each PC's MySQL.
--
--   "C:\Program Files\MySQL\MySQL Server 8.4\bin\mysql.exe" -u root -p --port=3306 < create-demo.sql
--
-- Then fill it:   dotnet run --project dotnet/tools/DemoSeed
-- The passwords below are PUBLIC (this repository is public). They only work from this PC
-- (@'localhost') and only on probeta_demo. Never reuse them anywhere else.
-- See DEMO-CREDENTIALS.md.
-- =====================================================================

CREATE DATABASE IF NOT EXISTS probeta_demo
    CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci;

-- What the program connects with: read, insert, update (same rights as specimen_app).
CREATE USER IF NOT EXISTS 'demo_app'@'localhost' IDENTIFIED BY 'Demo-App-2026';
GRANT SELECT, INSERT, UPDATE ON probeta_demo.* TO 'demo_app'@'localhost';

-- Builds the schema and lets DemoSeed --reset drop and recreate probeta_demo.
CREATE USER IF NOT EXISTS 'demo_admin'@'localhost' IDENTIFIED BY 'Demo-Admin-2026';
GRANT ALL PRIVILEGES ON probeta_demo.* TO 'demo_admin'@'localhost';
