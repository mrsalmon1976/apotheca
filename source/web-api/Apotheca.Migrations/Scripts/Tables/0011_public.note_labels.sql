CREATE TABLE IF NOT EXISTS note_labels (
    note_id     TEXT NOT NULL,
    label_id    TEXT NOT NULL,

    CONSTRAINT pk_note_labels           PRIMARY KEY (note_id, label_id),
    CONSTRAINT fk_note_labels_note      FOREIGN KEY (note_id)  REFERENCES notes (id),
    CONSTRAINT fk_note_labels_label     FOREIGN KEY (label_id) REFERENCES labels (id)
);

CREATE INDEX IF NOT EXISTS ix_note_labels_label_id ON note_labels (label_id);
