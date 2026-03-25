-- search table
-- usage:
--  SELECT * FROM search
--      WHERE reference_type = 'NOTE'
--      AND search_vector @@ websearch_to_tsquery('english', 'your search terms');
CREATE TABLE IF NOT EXISTS search (
    id               BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    reference_id     TEXT        NOT NULL,
    reference_type   TEXT        NOT NULL,
    text_title       TEXT        NOT NULL DEFAULT '',
    text_body        TEXT        NOT NULL DEFAULT '',
    search_language  TEXT        NOT NULL DEFAULT 'english',
    search_vector    TSVECTOR,
    updated_at       TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE OR REPLACE FUNCTION search_vector_update() RETURNS trigger AS $$
BEGIN
    NEW.search_vector := to_tsvector(NEW.search_language::regconfig, NEW.text_title || ' ' || NEW.text_body);
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'trg_search_vector_update') THEN
        CREATE TRIGGER trg_search_vector_update
            BEFORE INSERT OR UPDATE ON search
            FOR EACH ROW EXECUTE FUNCTION search_vector_update();
    END IF;
END;
$$;

CREATE INDEX IF NOT EXISTS ix_search_vector ON search USING GIN (search_vector);
