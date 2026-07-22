CREATE TABLE IF NOT EXISTS mindmap_nodes (
    id              TEXT        NOT NULL,
    mindmap_id      TEXT        NOT NULL,
    parent_node_id  TEXT        NULL,
    header          TEXT        NOT NULL,
    body            TEXT        NULL,
    position        INTEGER     NOT NULL DEFAULT 0,
    collapsed       BOOLEAN     NOT NULL DEFAULT FALSE,
    created_by      TEXT        NOT NULL,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT now(),
    deleted_at      TIMESTAMPTZ NULL,

    CONSTRAINT pk_mindmap_nodes            PRIMARY KEY (id),
    CONSTRAINT fk_mindmap_nodes_mindmap    FOREIGN KEY (mindmap_id)     REFERENCES mindmaps (id),
    CONSTRAINT fk_mindmap_nodes_parent     FOREIGN KEY (parent_node_id) REFERENCES mindmap_nodes (id),
    CONSTRAINT fk_mindmap_nodes_created_by FOREIGN KEY (created_by)     REFERENCES users (id)
);

CREATE INDEX IF NOT EXISTS ix_mindmap_nodes_mindmap_id     ON mindmap_nodes (mindmap_id);
CREATE INDEX IF NOT EXISTS ix_mindmap_nodes_parent_node_id ON mindmap_nodes (parent_node_id);
