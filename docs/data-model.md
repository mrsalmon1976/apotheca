# Data Model

## Core Entities

| Entity | Storage | Description |
| :--- | :--- | :--- |
| **User** | Postgres | Identity, roles, and preferences. |
| **Task** | Postgres | Tasks associated with the current user and/or project. |
| **Project** | Postgres | A collection of tasks, notes, and work items. |

## Relationships
- **User (N) -> Project (N)**: Projects can be shared by many users.
- **User (1) -> Task (N)**: One user can own many tasks.
- **Project (1) -> Task (N)**: Tasks can be associated with a project.

## Entity Relationship Diagram

```mermaid
erDiagram
    users {
        TEXT id PK
        TEXT email UK
        TEXT display_name
        TEXT photo_url
        TIMESTAMPTZ created_at
        TIMESTAMPTZ updated_at
    }

    user_firebase_identities {
        TEXT firebase_uid PK
        TEXT user_id FK
        TEXT provider_id
        TIMESTAMPTZ created_at
    }

    projects {
        TEXT id PK
        TEXT name
        TIMESTAMPTZ created_at
    }

    user_projects {
        TEXT user_id PK, FK
        TEXT project_id PK, FK
        TEXT project_role
        TIMESTAMPTZ created_at
    }

    audit_project_logs {
        BIGINT id PK
        TEXT project_id
        TEXT changed_by
        TIMESTAMPTZ changed_at
        TEXT operation
        TEXT log_message
        JSONB old_data
        JSONB new_data
    }

    users ||--o{ user_firebase_identities : "has"
    users ||--o{ user_projects : "member of"
    projects ||--o{ user_projects : "has member"
    projects ||--o{ audit_project_logs : "logged in"
```
