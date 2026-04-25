CREATE TABLE IF NOT EXISTS document_labels (
    document_id TEXT NOT NULL,
    label_id    TEXT NOT NULL,

    CONSTRAINT pk_document_labels              PRIMARY KEY (document_id, label_id),
    CONSTRAINT fk_document_labels_document     FOREIGN KEY (document_id) REFERENCES documents (id),
    CONSTRAINT fk_document_labels_label        FOREIGN KEY (label_id)    REFERENCES labels (id)
);

CREATE INDEX IF NOT EXISTS ix_document_labels_label_id ON document_labels (label_id);
