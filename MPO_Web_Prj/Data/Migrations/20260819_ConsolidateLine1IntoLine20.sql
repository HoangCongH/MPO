BEGIN;

-- Production reports reference master_machines by machine_id, so canonicalizing
-- the master line value reassigns all existing report, feeder, and nozzle data
-- without rewriting their foreign keys.
CREATE OR REPLACE FUNCTION mpo_canonical_line_name(input_line text)
RETURNS text
LANGUAGE sql
IMMUTABLE
AS $$
    SELECT CASE
        WHEN regexp_replace(lower(btrim(COALESCE(input_line, ''))), '[^a-z0-9]', '', 'g')
             IN ('line1', '1', 'line20', '20')
            THEN '20'
        ELSE NULLIF(btrim(input_line), '')
    END;
$$;

UPDATE master_machines
SET line = mpo_canonical_line_name(line)
WHERE line IS DISTINCT FROM mpo_canonical_line_name(line);

-- Normalize future master-data imports even when the upstream parser sends
-- Line 1, Line1, 1, Line 20, Line20, or 20.
CREATE OR REPLACE FUNCTION mpo_normalize_master_machine_line()
RETURNS trigger
LANGUAGE plpgsql
AS $$
BEGIN
    NEW.line := mpo_canonical_line_name(NEW.line);
    RETURN NEW;
END;
$$;

DROP TRIGGER IF EXISTS trg_master_machines_normalize_line ON master_machines;
CREATE TRIGGER trg_master_machines_normalize_line
BEFORE INSERT OR UPDATE OF line ON master_machines
FOR EACH ROW
EXECUTE FUNCTION mpo_normalize_master_machine_line();

COMMIT;
