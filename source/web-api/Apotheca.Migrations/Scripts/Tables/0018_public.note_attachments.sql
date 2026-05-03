CREATE TABLE IF NOT EXISTS note_attachments (
    id              TEXT        NOT NULL,
    project_id      TEXT        NOT NULL,
    note_id         TEXT        NOT NULL,
    blob_reference  TEXT        NOT NULL,
    file_name       TEXT        NOT NULL,
    mimetype        TEXT        NOT NULL,
    file_length     BIGINT      NOT NULL,
    created_by      TEXT        NOT NULL,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT now(),
    deleted_at      TIMESTAMPTZ NULL,
    CONSTRAINT pk_note_attachments PRIMARY KEY (id)
);
