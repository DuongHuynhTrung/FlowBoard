# Database Design

## Auth DB (SQL Server)

*Target Database for the Auth Service. Manages authentication and user sessions.*

- **Users**
  - `id` (UNIQUEIDENTIFIER): Primary Key.
  - `email` (NVARCHAR(255), UNIQUE, NOT NULL): Ensures distinct accounts.
  - `passwordHash` (NVARCHAR(255), NOT NULL): Securely hashed secret.
  - `fullName` (NVARCHAR(100))
  - `avatarUrl` (NVARCHAR(MAX))
  - `isActive` (BIT): Toggles soft bans/disable.
  - `createdAt` (DATETIME2)
  - `updatedAt` (DATETIME2)

- **Roles**
  - `id` (INT): Primary Key.
  - `name` (NVARCHAR(50)): E.g., Admin, Member, Viewer.
  - `description` (NVARCHAR(255))

- **UserRoles**
  - `userId` (UNIQUEIDENTIFIER): Foreign Key to Users.
  - `roleId` (INT): Foreign Key to Roles.
  - *Composite PK on (userId, roleId)*

- **RefreshTokens**
  - `id` (UNIQUEIDENTIFIER): Primary Key.
  - `userId` (UNIQUEIDENTIFIER): Foreign Key to Users.
  - `token` (NVARCHAR(MAX)): The token itself, typically hashed in DB.
  - `expiresAt` (DATETIME2)
  - `revokedAt` (DATETIME2, NULL)
  - `createdByIp` (NVARCHAR(50))
  - `revokedByIp` (NVARCHAR(50), NULL)
  - `replacedByToken` (NVARCHAR(MAX), NULL)

## Project DB (PostgreSQL)

*Target Database for the Project Service. Manages Kanban spaces, projects, boards, and tasks.*

- **workspaces**
  - `id` (UUID): Primary Key
  - `name` (VARCHAR)
  - `description` (TEXT)
  - `ownerId` (UUID): Logical FK referencing User ID in Auth Service.
  - `createdAt` (TIMESTAMP)

- **workspace_members**
  - `workspaceId` (UUID): FK to workspaces
  - `userId` (UUID): Logical FK referencing Auth Service
  - `role` (VARCHAR): owner/admin/member
  - `joinedAt` (TIMESTAMP)
  - *Composite PK on (workspaceId, userId)*

- **projects**
  - `id` (UUID): Primary Key
  - `workspaceId` (UUID): FK to workspaces
  - `name` (VARCHAR)
  - `description` (TEXT)
  - `createdAt` (TIMESTAMP)

- **boards**
  - `id` (UUID): Primary Key
  - `projectId` (UUID): FK to projects
  - `name` (VARCHAR)
  - `createdAt` (TIMESTAMP)

- **board_columns**
  - `id` (UUID): Primary Key
  - `boardId` (UUID): FK to boards
  - `name` (VARCHAR)
  - `position` (INT): Maintains column rendering order.
  - `createdAt` (TIMESTAMP)

- **tasks**
  - `id` (UUID): Primary Key
  - `columnId` (UUID): FK to board_columns
  - `boardId` (UUID): FK to boards
  - `title` (VARCHAR)
  - `description` (TEXT)
  - `assigneeId` (UUID, NULL): Logical FK to Auth Service.
  - `dueDate` (TIMESTAMP, NULL)
  - `position` (INT): Order inside the column.
  - `createdAt` (TIMESTAMP)
  - `updatedAt` (TIMESTAMP)

- **labels**
  - `id` (UUID): Primary Key
  - `boardId` (UUID): FK to boards
  - `name` (VARCHAR)
  - `color` (VARCHAR): Hexadecimal e.g. `#FF0000`

- **task_labels**
  - `taskId` (UUID): FK to tasks
  - `labelId` (UUID): FK to labels
  - *Composite PK on (taskId, labelId)*

- **comments**
  - `id` (UUID): Primary Key
  - `taskId` (UUID): FK to tasks
  - `authorId` (UUID): Logical FK to Auth Service User
  - `content` (TEXT)
  - `createdAt` (TIMESTAMP)
  - `updatedAt` (TIMESTAMP)

- **invite_tokens**
  - `id` (UUID): Primary Key
  - `workspaceId` (UUID): FK to workspaces
  - `email` (VARCHAR)
  - `token` (VARCHAR): Hashed
  - `expiresAt` (TIMESTAMP)
  - `usedAt` (TIMESTAMP, NULL)

## Notification DB (MongoDB)

*Target Database for Notification Service. Highly performant document DB for activity logs.*

- **notifications collection**
  - `_id` (ObjectId): Document identifier.
  - `userId` (String): Owner of the notification (UUID representation).
  - `type` (String): Nature of the notification (e.g., `task_assigned`).
  - `title` (String)
  - `body` (String)
  - `data` (Object): Custom key-values depending on `type`.
  - `isRead` (Boolean)
  - `createdAt` (Date)

- **activity_logs collection**
  - `_id` (ObjectId)
  - `workspaceId` (String)
  - `projectId` (String)
  - `actor` (Object): `{ id: String, name: String }`
  - `action` (String)
  - `entityType` (String)
  - `entityId` (String)
  - `changes` (Object): Diff record.
  - `createdAt` (Date)
