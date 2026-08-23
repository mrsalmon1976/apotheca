ALTER TABLE projects ADD COLUMN IF NOT EXISTS workspace_id TEXT NULL;

-- Backfill: one workspace per user who owns an un-migrated project, named after them.
-- Guarded by WHERE workspace_id IS NULL so this is a no-op on repeat runs.
-- Matches both 'OWNER' (pre-rename) and 'ADMIN' (post-rename) since this script
-- runs before the 0026 role-rename script.
DO $$
DECLARE
    owner_row RECORD;
    new_workspace_id TEXT;
BEGIN
    FOR owner_row IN
        SELECT DISTINCT u.id AS user_id, u.display_name
        FROM users u
        INNER JOIN project_users pu ON pu.user_id = u.id AND pu.project_role IN ('OWNER', 'ADMIN')
        INNER JOIN projects p ON p.id = pu.project_id
        WHERE p.workspace_id IS NULL
    LOOP
        new_workspace_id := replace(gen_random_uuid()::text, '-', '');

        INSERT INTO workspaces (id, "name")
        VALUES (new_workspace_id, owner_row.display_name || '''s Workspace');

        INSERT INTO workspace_users (workspace_id, user_id, workspace_role)
        VALUES (new_workspace_id, owner_row.user_id, 'ADMIN');

        INSERT INTO user_settings (user_id, current_workspace_id)
        VALUES (owner_row.user_id, new_workspace_id)
        ON CONFLICT (user_id) DO NOTHING;

        UPDATE projects p
        SET workspace_id = new_workspace_id
        FROM project_users pu
        WHERE pu.project_id = p.id
          AND pu.user_id = owner_row.user_id
          AND pu.project_role IN ('OWNER', 'ADMIN')
          AND p.workspace_id IS NULL;
    END LOOP;
END;
$$;

-- Fallback: any project still without a workspace (no OWNER/ADMIN row — only
-- CONTRIBUTOR/VIEWER/USER members, or none at all) gets its own workspace so the
-- NOT NULL constraint below can be applied safely. Named after the project itself.
DO $$
DECLARE
    orphan_project RECORD;
    fallback_member RECORD;
    new_workspace_id TEXT;
BEGIN
    FOR orphan_project IN
        SELECT id, name FROM projects WHERE workspace_id IS NULL
    LOOP
        new_workspace_id := replace(gen_random_uuid()::text, '-', '');

        INSERT INTO workspaces (id, "name")
        VALUES (new_workspace_id, orphan_project.name || ' Workspace');

        SELECT user_id INTO fallback_member
        FROM project_users
        WHERE project_id = orphan_project.id
        ORDER BY created_at
        LIMIT 1;

        IF fallback_member.user_id IS NOT NULL THEN
            INSERT INTO workspace_users (workspace_id, user_id, workspace_role)
            VALUES (new_workspace_id, fallback_member.user_id, 'ADMIN')
            ON CONFLICT (workspace_id, user_id) DO NOTHING;

            INSERT INTO user_settings (user_id, current_workspace_id)
            VALUES (fallback_member.user_id, new_workspace_id)
            ON CONFLICT (user_id) DO NOTHING;
        END IF;

        UPDATE projects SET workspace_id = new_workspace_id WHERE id = orphan_project.id;
    END LOOP;
END;
$$;

ALTER TABLE projects ALTER COLUMN workspace_id SET NOT NULL;

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_projects_workspace') THEN
        ALTER TABLE projects ADD CONSTRAINT fk_projects_workspace FOREIGN KEY (workspace_id) REFERENCES workspaces (id);
    END IF;
END;
$$;

CREATE INDEX IF NOT EXISTS ix_projects_workspace_id ON projects (workspace_id);
