-- 011: material type, so a design can show its water/cement ratio (a/c).

ALTER TABLE Materials
    ADD COLUMN MaterialType VARCHAR(20) NOT NULL DEFAULT 'other' COMMENT 'cement | water | aggregate | admixture | other' AFTER Name,
    ADD CONSTRAINT CK_Materials_Type CHECK (MaterialType IN ('cement', 'water', 'aggregate', 'admixture', 'other'));

-- Best guess for existing materials from the start of their name (case and accents ignored);
-- anything else stays 'other' and can be corrected in Materiales.
UPDATE Materials SET MaterialType = 'cement' WHERE Name LIKE 'cemento%';
UPDATE Materials SET MaterialType = 'water' WHERE Name LIKE 'agua%';
UPDATE Materials SET MaterialType = 'aggregate'
WHERE Name LIKE 'arena%' OR Name LIKE 'grava%' OR Name LIKE 'gravilla%' OR Name LIKE 'piedra%'
   OR Name LIKE 'agregado%' OR Name LIKE 'arido%';
UPDATE Materials SET MaterialType = 'admixture'
WHERE Name LIKE 'aditivo%' OR Name LIKE 'plastificante%' OR Name LIKE 'superplastificante%'
   OR Name LIKE 'acelerante%' OR Name LIKE 'retardante%' OR Name LIKE 'retardador%';

INSERT INTO SchemaVersion (Version) VALUES (11);
