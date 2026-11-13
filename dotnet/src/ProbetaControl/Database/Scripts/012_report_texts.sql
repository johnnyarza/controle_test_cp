-- 012: the lab details printed on reports (shared by every PC, changed in Configuración).
-- They start with the text the legacy reports had built in.

INSERT INTO AppSettings (SettingKey, TextValue) VALUES
    ('ReportLabName', 'Servicios de Ingeniería HJVA Ltda.'),
    ('ReportFooter', 'Pto. Quijarro, Av. Bolivia, 210 • TEL: 3-978-2323 / CEL: 689-21153 • E-mail: henry.hjva@gmail.com • Prov. Germán Busch - Santa Cruz'),
    ('ReportNote', 'NOTA: Los datos de este formulario y sus resultados se obtienen únicamente de las muestras presentadas por el cliente. Las reimpresiones tendrán costo adicional.'),
    ('ReportPaperSize', 'A4');

INSERT INTO SchemaVersion (Version) VALUES (12);
