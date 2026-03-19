# HinataProject – project management / task and bug tracking software

HinataProject is a project management and task tracking system with first-class support for AI agents as users. The system exposes all functionality via a REST API, enabling AI agents to read, create, update, and coordinate work items programmatically.

Human users access the system through a web interface. The system uses a node-based hierarchy with configurable type inheritance, allowing teams to define custom types and workflows. All actions are recorded in an audit log.

## Architecture
- Backend: ASP.NET Core, C#, REST API
- Frontend: React SPA, served as static files
- Database: PostgreSQL

## Usage

### Concept Overview

Data are organized as a single tree in HinataProject. A tree node represents a work item and serves as the fundamental building block of the entire system. Each node has a type that determines its behavior. The types are of two kinds:

- Structural Type: A container node that can define settings for its descendants. Structural nodes can alter the configuration of node types, workflows and roles - these changes propagate down to all descendant nodes via the Inherited* properties. Examples: Folder, Project, Epic.

- Stateful Type: A work item that has a workflow state. It has Workflow, State, and Assignees properties. Examples: Task, Bug, Feature.

As you move deeper into the Node tree, child nodes inherit the types, workflows, and roles defined by their ancestors. A structural node at a higher level (e.g., "Project") can define what types are available to all its children (e.g., "Task", "Bug"), while individual work items at the leaf level hold the actual state and assignments.

### Typical Workflow

A typical workflow involves creating a hierarchy of Nodes, defining types and workflows, then creating and managing work items.

1. Creating a Project

A user (human or AI agent) creates a new Node under an existing parent (e.g., under Root). The Node's Type is set to a structural type (e.g., "Project" if defined). The Node inherits Types, Workflows, and Roles from its parent.

2. Defining Work Item Types

On a structural Node (e.g., the Project), an admin configures ChangedTypes to add new types like "Task", "Bug", "Feature". These types become available to all descendant Nodes via InheritedTypes.

3. Setting Up Workflows

The admin also configures ChangedWorkflows on the structural Node to define available workflows. Each Workflow contains States (e.g., "To Do", "In Progress", "Done"). For stateful types, the admin sets which Role corresponds to which State via the Role's States property.

4. Creating Work Items

Users create Nodes under the Project with a stateful Type (e.g., "Task"). The Node automatically gets assigned the Workflow's DefaultState. The Node's Assignees property maps Roles to Users.

5. Working on Items

- Users with appropriate Roles can transition the Node's State to a next state in the Workflow
- The Assignee calculated property returns the User mapped to the Role that corresponds to the current State
- Comments can be added to Nodes via the Comment object

### AI Agent Integration

All operations are performed via the REST API. As a "user" can be both a human and a machine, AI Agents can:
- Read Nodes (query by ID, list children, search)
- Create new Nodes with specified Type and initial properties
- Update Node properties (Manifest, Caption, Description)
- Update Node Guardrails (auth: Admin)
- Add Comments to Nodes
- Change Workflow State (if authorized via Role-User mapping)
- Manage Assignees (map Roles to Users)

### MCP Server

The application exposes an MCP (Model Context Protocol) server that provides additional tools for AI agents. MCP tools are available at `/mcp` endpoint.

#### Available MCP Tools

| Tool | Description |
|------|-------------|
| `get_node` | Get a node by its ID |
| `list_children` | List children of a node |
| `get_assigned_to_me` | List nodes assigned to the current user |
| `update_node` | Update node properties (Manifest, Caption, Description) |
| `set_node_state` | Transition a node to a different workflow state |
| `add_comment` | Add a comment to a node |
| `list_comments` | List comments for a node |
| `create_similar_child` | Create a new child node that inherits type, workflow, and assignees from parent. The new node's state is set to the workflow's default state. |

## Data structure

The main object of HinataProject is `Node` which represents numerous work items like Project, Task, Bug etc. The concrete type of `Node` is user defined through the hierarchical type system: users define available types by setting `ChangedTypes` on ancestor nodes, which propagate down to descendants via `InheritedTypes`. Nothing like Task is hardcoded in HinataProject – all types are created and managed by users through this mechanism. The only hardcoded `Node` type is "Folder".

There is always one `Node` called "Root" in the whole installation. All other `Node` objects are children of "Root" or another `Node`. One can think of "Root" as a container of the main admin settings.

Every `Node` has exactly one parent (except for Root). Thus, `Node` objects make up a tree graph.

`Node` object has following properties:
- topological
  - `Parent` – parent `Node` (reference to `Node`)
  - `PublicId` – incremental integer for easier reference (int)
- descriptive
  - `Type` – one of types from `InheritedTypes`; must be selected from types defined by ancestor nodes via their `ChangedTypes` property (reference to `Type`)
  - `Manifest` – long technical specification of the `Node` that describes everything what the `Node` represents – **except** what is described in detail in child `Node` objects (text)
  - `Caption` – one line summary of the `Node` (text)
  - `Description` – short summary (one/two paragraphs) of the `Node` suitable for humans; distilled from `Manifest` (text)
  - `Guardrails` – constraints and guardrails for AI agents working on this Node (text)
- comments
  - `Comments` – list of comments (set of references to `Comment`)
- inherited settings properties – setting this `Node` uses but are defined in ancestor `Node` objects – that is, parent, parent of parent etc.; inherited properties are calculated, not persisted. For the Root Node (which has no parent), its inherited properties are derived from its own Changed* properties – this makes Root the source of types, workflows, and roles for the entire installation.
  - `InheritedTypes` – types of `Node` like Project, Task or Bug (set of references to `Type`)
  - `InheritedWorkflow` – possible `Workflow` objects (set of references to `Workflow`)
  - `InheritedRoles` – possible roles used with `User` (set of references to `Role`)
- settings properties – new settings that affects `Inherited*` properties of descendant `Node` objects; enabled only for `Type.Kind` == "Structural"
  - `ChangedTypes` – values can be kept, added or removed; see `InheritedTypes` (diff of references to `Type`)
  - `ChangedWorkflows` – values can be kept, added or removed; see `InheritedWorkflow`; changes must be made in unison with `ChangedTypes` (diff of references to `Workflow`)
  - `ChangedRoles` – values can be kept, added or removed; see `InheritedRoles`; changes must be made in unison with `ChangedTypes` through `Role` settings (diff of references to `Role`)
- workflow properties – represents state of the `Node`; enabled only for `Type.Kind` == "Stateful"
  - `Workflow` – one of possible `Workflow` objects for given `Type` (reference to `Workflow`)
  - `State` – one of possible `State` values from given `Workflow` (reference to `State`)
  - `Assignees` – a mapping from `Role` to specific `User`; that is, for every available `Role`, there is zero or one assigned `User` (mapping `Role` to references to `User`)
  - `Assignee` – calculated property/not persisted: `User` that is in `Assignees` and whose `Role` corresponds to `State`; see `Role` for more info (reference to `User`)
- audit properties
  - `AuditLog` – all changes made (record of: reference to `User`, time, property changed, change – either old and new value or diff for text properties)

`Comment` is a single comment item
- `Text` – text of the comment (text)
- `User` – who posted it (reference to `User`)
- file support will be added later

`User` is an object representing either a human or machine
- `Name` – name (text)
- `Subject` – OAuth subject value (text)
- `Info` – basic information; now URL to icon, other details will be added later
- `Rights` – one of predefined permission levels (enum values Admin, Active, Passive); determines what the user can or cannot do
- `Roles` – which `Role` objects are activated for this `User` (set of references to `Role`). The Roles that appear in the Node's `InheritedRoles` are the ones that establish the correspondence between `User`, `Role`, and `State` needed for the `Assignee` calculated property (see `Role.States` and `Assignee` for details).

`Role` describes a functional position that can be assigned to a user for a specific workflow state
- `Name` – name (text)
- `States` – which `State` objects this `Role` corresponds to (optional set of references to `State`); a constraint must be kept: for every existing `Node`, no two `Role` objects may have the same `State` in their `States` property – this constraint is required for the `Assignee` calculated property to work unambiguously

`State` – one state value in specific workflow
- `Name` – name (text)
- `IsFinalSuccess` – whether this state is considered as final and successful
- `IsFinalFailure` – whether this state is considered as final and failed

`Workflow` - set of `State` values
- `States` – which `State` objects does this `Workflow` own (set of owned `State`)
- `DefaultState` – which `State` object from `States` is considered as default state

`Type` – type of specific `Node`
- `Kind` – one of enum values "Structural", "Stateful"
- `Name` – name (text)
- `Color` – color for UI hinting (text)
- `DefaultWorkflow` – the `Workflow` to use by default when creating a new `Node` with this `Type` (reference to `Workflow`)

*Note: Changed properties (`ChangedTypes`, `ChangedWorkflows`, `ChangedRoles`) consist of two parts: added and removed entities (e.g. added Types and removed Types). In JSON serialization they would be a compound object `{ "added" : [ ... ], "removed": [ ... ] }`. In database they would be persisted in two tables AddedTypes and RemovedTypes.*

### Entities

Remarks for EF implementation:

| Entity | Key Fields |
|--------|------------|
| Node | ParentId (self-ref), PublicId, TypeId, Manifest, Caption, Description, Guardrails, WorkflowId, StateId, Comments, AddedTypes, RemovedTypes, AddedWorkflows, RemovedWorkflows, AddedRoles, RemovedRoles |
| Type | Kind (enum: Structural/Stateful), Name, Color, DefaultWorkflowId |
| Workflow | DefaultStateId, States (collection) |
| State | Name, IsFinalSuccess, IsFinalFailure |
| Role | Name, States (collection) |
| User | Name, Subject, Rights (enum: Admin/Active/Passive), Roles (collection) |
| Comment | Text, UserId, NodeId |
| NodeAssignee | (join table) NodeId, RoleId, UserId |
| AuditLogEntry | UserId, Timestamp, PropertyName, OldValue, NewValue |

### Key Implementation Notes

1. Inherited properties - C# calculated properties (runtime calculation, optional caching)
2. Changed properties - Implemented as two collections per category: Added/Removed ("changes" consist of additions/removals)
3. Assignees - Join table with unique constraint on (NodeId, RoleId)
4. Enums - Explicit: Rights (Admin/Active/Passive), Kind (Structural/Stateful)
5. One-to-many Comment - Comment belongs to exactly one Node
6. AuditLog - Separate table with individual columns

## Initial state

After installation, there is
- exactly one `Role` (called "Superadmin" role) - a functional role with no specific states
- exactly one `User` whose OAuth subject is obtained during installation; this user has `Rights` set to Admin and `Roles` set to the "Superadmin" role. When this user authenticates via OAuth and their subject matches the stored value, they are recognized as having Admin rights.
- exactly one `Type` called "Folder" (`Kind` = "Structural")
- exactly one `Node` called "Root" with `Type` set to "Folder"; Root has its `ChangedRoles` set to include the Superadmin role, which means `InheritedRoles` for Root (and all its descendants) includes this role; Root also have "Folder" added to it's `AddedNodes` property

This way the "Superadmin" has all the rights required to set up a tree of nodes, assign users etc.

## Authentication

HinataProject uses OAuth as an identity provider.

The backend expects to have JWT tokens included in the requests. It maps `subject` field to its own `User` entities and then follows authorization rules based just on `User` and its `Role`.

Web frontend won't interact with backend without a proper JWT token. Thus, if a unauthenticated user lands on the web page, only very basic information is shown and a redirect link to the OAuth provider (which is considered out of scope for HinataProject) is provided (some "Sign In" and "Register" buttons).

AI agents are expected to get their JWT tokens by themselves before interaction with the backend.

## Configuration

The configuration would be provided by IConfiguration classes with data sourced from appsettings.json or environment variables. For frontend there would be another configuration JSON file placed in the staticFiles directory.

Backend configuration
- Database connection string
- OAuth provider settings (`connect` URI, issuer, audience)
- for the first run:
  - OAuth subject of the first Superadmin

## Business Logic

*Note on authentication levels: only the lowest auth level is specified (the order is Admin -> Active -> Passive); Admin can do anything, something can be done only by Active roles (and Admin), something can be done by everyone (marked as Passive as this is the lowest level)*

*Second note on authentication levels: even though something can be done by everyone (marked as Passive), the calls must still be authenticated and checked for Admin/Active/Passive auth levels! No "Anonymous" access is permitted.*

### Public Operations (will be exposed through REST API)

- Node Operations
  - Get root node ID - returns only the root node's GUID (lightweight endpoint) (auth: Passive)
  - Create new node - specify parent, type, initial properties (auth: Active)
  - Read node by ID (auth: Passive)
  - List children of a node (auth: Passive)
  - List nodes whose Assignee is the current user - "Assigned To Me" (auth: Passive)
  - Update node properties - Manifest, Caption, Description (auth: Active)
  - Update node guardrails - Guardrails (auth: Admin)
  - Delete node (auth: Admin)
- Settings Management (on structural nodes)
  - Set ChangedTypes - add/remove types available to descendants (auth: Admin)
  - Set ChangedWorkflows - configure workflows for descendants (auth: Admin)
  - Set ChangedRoles - add/remove roles available to descendants (auth: Admin)
- User Management
  - List users - list all users in the system (auth: Admin)
  - Create user - create new user with Name and Subject (auth: Admin)
  - Update user - update existing user properties (auth: Admin)
  - Delete user - remove a user from the system (auth: Admin)
- State Transitions (on stateful nodes)
  - Move node to different state in its workflow (auth: Passive)
  - Get Assignees - map roles to users on a node (auth: Passive)
  - Set Assignees - map roles to users on a node (auth: Admin)
- Comments
  - Add comment to node (auth: Passive)
  - List comments on node (auth: Passive)

### Internal Logic (not exposed via API)

- Authentication
  - Validate JWT tokens
  - Map token subject to User
  - Validate user has appropriate role for target state
- Inheritance Calculation
  - Calculate InheritedTypes from ancestor ChangedTypes
  - Calculate InheritedWorkflow from ancestor ChangedWorkflows
  - Calculate InheritedRoles from ancestor ChangedRoles
- Assignee Resolution
  - Calculate Assignee based on current state and role mappings
- Audit Logging
  - Record all changes automatically (user, timestamp, property, old/new values)
- Initial Setup (First Run)
  - Create Superadmin role
  - Create initial user from OAuth subject
  - Create "Folder" type
  - Create Root node

## Operation Details

### Node Operations

#### Get Root Node ID
- **Auth**: Passive
- **Input**: none
- **Validation**:
  - none (except standard authentication)
- **Business Rules**:
  - Return the Root node's ID only (node with no parent)
  - Lightweight endpoint for frontend redirects from `/nodes/root` to `/nodes/{guid}`
- **Errors**: `RootNodeNotFound`

#### Create Node
- **Auth**: Active
- **Input**: `ParentId`, `Type`, (optional: `Workflow`, `Manifest`, `Caption`, `Description`, `Guardrails`)
- **Validation**:
  - `ParentId` must exist and be a valid Node
  - `Type` must exist in Parent's `InheritedTypes`
  - If `Type.Kind` is Stateful:
    - `Workflow` must be provided OR selected from `Type.DefaultWorkflow`
    - If no workflow provided and no `Type.DefaultWorkflow` set, return `WorkflowRequired` error
- **Business Rules**:
  - Generate default `State` from `Workflow.DefaultState`
  - Initialize empty `Assignees` mapping
  - Create `AuditLogEntry` for the creation
- **Errors**: `ParentNotFound`, `TypeNotAllowed`, `WorkflowRequired`, `WorkflowNotAllowed`

#### Read Node
- **Auth**: Passive
- **Input**: `NodeId`
- **Validation**:
  - `NodeId` must exist
- **Business Rules**:
  - Return node with all properties, including calculated `Inherited*` properties
  - Return calculated `Assignee` for stateful nodes
- **Errors**: `NodeNotFound`

#### List Children
- **Auth**: Passive
- **Input**: `ParentId`, (optional: pagination: `Skip`, `Take`, default Skip=0, Take=20)
- **Validation**:
  - `ParentId` must exist
- **Business Rules**:
  - Return direct children of the specified node
  - Include basic properties (Id, Caption, Type, State)
  - Apply pagination if specified
- **Errors**: `ParentNotFound`

#### Search Nodes: Assigned To Me
- **Auth**: Passive
- **Input**: `User` taken through JWT token, (optional: pagination: `Skip`, `Take`, default Skip=0, Take=20)
- **Validation**:
  - none (except standard authentication)
- **Business Rules**:
  - Return nodes whose Assignee is `User`
  - Include basic properties (Id, Caption, Type, State)
  - Apply pagination if specified

#### Update Node
- **Auth**: Active
- **Input**: `NodeId`, (optional: `Manifest`, `Caption`, `Description`)
- **Validation**:
  - `NodeId` must exist
  - At least one property to update must be provided
- **Business Rules**:
  - Update only provided properties (partial update)
  - Create `AuditLogEntry` for each changed property (old value → new value)
  - Text properties: record diff in audit log
- **Errors**: `NodeNotFound`, `NoPropertiesToUpdate`

#### Update Guardrails
- **Auth**: Admin
- **Input**: `NodeId`, `Guardrails`
- **Validation**:
  - `NodeId` must exist
- **Business Rules**:
  - Update `Guardrails` property
  - Create `AuditLogEntry` for the change
- **Errors**: `NodeNotFound`

#### Delete Node
- **Auth**: Admin
- **Input**: `NodeId`
- **Validation**:
  - `NodeId` must exist
  - Cannot delete Root node
  - If node has children, must handle cascade (recursive delete)
- **Business Rules**:
  - Create `AuditLogEntry` for the deletion
  - Remove associated `Comments`
  - Remove associated `NodeAssignee` entries
  - For nodes with children: recursively delete
- **Errors**: `NodeNotFound`, `CannotDeleteRoot`

### Settings Management (on Structural Nodes)

#### Set ChangedTypes
- **Auth**: Admin
- **Input**: `NodeId`, `ChangedTypes` (additions and/or removals)
- **Validation**:
  - `NodeId` must exist
  - `Node.Type.Kind` must be Structural
  - For removal: check if Type is in use by descendant nodes
- **Business Rules**:
  - **Adding**: Create new Type instance (does not need to exist in `InheritedTypes` first)
  - **Removing**: Mark Type as removed in `RemovedTypes` collection (item becomes hidden from descendants' `InheritedTypes`)
  - Create `AuditLogEntry` for the change
  - This affects `InheritedTypes` for all descendants
- **Errors**: `NodeNotFound`, `NodeNotStructural`, `TypeInUseByDescendants`

#### Set ChangedWorkflows
- **Auth**: Admin
- **Input**: `NodeId`, `ChangedWorkflows` (additions and/or removals)
- **Validation**:
  - `NodeId` must exist
  - `Node.Type.Kind` must be Structural
  - For removal: check if Workflow is in use by descendant stateful nodes
- **Business Rules**:
  - **Adding**: Create new Workflow instance (does not need to exist in `InheritedWorkflow` first)
  - **Removing**: Mark Workflow as removed in `RemovedWorkflows` collection (item becomes hidden from descendants' `InheritedWorkflow`)
  - Create `AuditLogEntry` for the change
  - This affects `InheritedWorkflow` for all descendants
- **Errors**: `NodeNotFound`, `NodeNotStructural`, `WorkflowInUseByDescendants`

#### Set ChangedRoles
- **Auth**: Admin
- **Input**: `NodeId`, `ChangedRoles` (additions and/or removals)
- **Validation**:
  - `NodeId` must exist
  - `Node.Type.Kind` must be Structural
  - For removal: check if Role is in use (has Assignees) on descendant nodes
- **Business Rules**:
  - **Adding**: Create new Role instance (does not need to exist in `InheritedRoles` first)
  - **Removing**: Mark Role as removed in `RemovedRoles` collection (item becomes hidden from descendants' `InheritedRoles`)
  - Create `AuditLogEntry` for the change
  - This affects `InheritedRoles` for all descendants
- **Errors**: `NodeNotFound`, `NodeNotStructural`, `RoleInUseByDescendants`

### State Transitions (on Stateful Nodes)

#### Move Node to Different State
- **Auth**: Passive
- **Input**: `NodeId`, `TargetStateId`
- **Validation**:
  - `NodeId` must exist
  - `Node.Type.Kind` must be Stateful
  - `TargetStateId` must belong to `Node.Workflow.States`
  - User must be Admin OR the current assignee for the node (the user assigned to the role corresponding to the current state)
- **Business Rules**:
  - Update `Node.State` to `TargetStateId`
  - Create `AuditLogEntry` for the state change
  - Re-evaluate calculated `Assignee` property
- **Errors**: `NodeNotFound`, `NodeNotStateful`, `StateNotInWorkflow`, `UserNotAuthorizedForState`

#### Get Assignees
- **Auth**: Passive
- **Input**: `NodeId`
- **Validation**:
  - `NodeId` must exist
  - `Node.Type.Kind` must be Stateful
- **Business Rules**:
  - Return `Node.Assignees` mapping
- **Errors**: `NodeNotFound`, `NodeNotStateful`

#### Set Assignees
- **Auth**: Admin
- **Input**: `NodeId`, `Assignees` (mapping of RoleId → UserId)
- **Validation**:
  - `NodeId` must exist
  - `Node.Type.Kind` must be Stateful
  - Each RoleId must exist in `InheritedRoles`
  - Each UserId must exist
  - Each Role can have at most one User assigned
- **Business Rules**:
  - Update `Node.Assignees` mapping
  - Create `AuditLogEntry` for each assignment change
  - Re-evaluate calculated `Assignee` property
- **Errors**: `NodeNotFound`, `NodeNotStateful`, `RoleNotAllowed`, `UserNotFound`, `DuplicateRoleAssignment`

### Comments

#### Add Comment
- **Auth**: Passive
- **Input**: `NodeId`, `Text`
- **Validation**:
  - `NodeId` must exist
  - `Text` must not be empty
- **Business Rules**:
  - Create new `Comment` linked to Node and User
  - Create `AuditLogEntry` for the addition
- **Errors**: `NodeNotFound`, `EmptyComment`

#### List Comments
- **Auth**: Passive
- **Input**: `NodeId`, (optional: pagination: `Skip`, `Take`, default Skip=0, Take=20; optional: `Ordering`, default ascending)
- **Validation**:
  - `NodeId` must exist
- **Business Rules**:
  - Return comments ordered by creation time
  - Include User information with each comment
  - Apply pagination if specified
- **Errors**: `NodeNotFound`

### Common Error Codes

| Error Code | Description |
|------------|-------------|
| `RootNodeNotFound` | Root node does not exist |
| `ParentNotFound` | Specified parent node does not exist |
| `NodeNotFound` | Specified node does not exist |
| `NodeNotStructural` | Operation requires a structural node |
| `NodeNotStateful` | Operation requires a stateful node |
| `TypeNotAllowed` | Type is not in node's InheritedTypes |
| `TypeInUseByDescendants` | Type is in use by child nodes |
| `WorkflowRequired` | Stateful node requires a workflow |
| `WorkflowNotAllowed` | Workflow is not in node's InheritedWorkflow |
| `WorkflowInUseByDescendants` | Workflow is in use by descendant nodes |
| `StateNotInWorkflow` | State does not belong to node's workflow |
| `RoleNotAllowed` | Role is not in node's InheritedRoles |
| `RoleInUseByDescendants` | Role is assigned to descendant nodes |
| `UserNotFound` | Specified user does not exist |
| `UserNotAuthorizedForState` | User does not have appropriate role for target state |
| `DuplicateRoleAssignment` | Role already has a user assigned |
| `CannotDeleteRoot` | Cannot delete the Root node |
| `NoPropertiesToUpdate` | No properties provided for update |
| `EmptyComment` | Comment text is empty |

## REST API

All endpoints follow RESTful conventions. JSON is used for request/response bodies.

### Nodes

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| GET | `/api/nodes/root_id` | Get root node ID (lightweight endpoint for frontend redirects) | Passive |
| POST | `/api/nodes` | Create new node | Active |
| GET | `/api/nodes/{id}` | Get node by ID | Passive |
| GET | `/api/nodes/{id}/children` | List children of a node | Passive |
| GET | `/api/nodes/assigned-to-me` | List nodes assigned to current user | Passive |
| GET | `/api/nodes/assigned-to-me/poll` | Long-poll for changes in assigned nodes (blocks until change or timeout) | Passive |
| PUT | `/api/nodes/{id}` | Update node properties (Manifest, Caption, Description) | Active |
| PUT | `/api/nodes/{id}/guardrails` | Update node guardrails | Admin |
| DELETE | `/api/nodes/{id}` | Delete node (recursive) | Admin |

### Settings (on Structural Nodes)

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| PUT | `/api/nodes/{id}/types` | Set ChangedTypes | Admin |
| PUT | `/api/nodes/{id}/workflows` | Set ChangedWorkflows | Admin |
| PUT | `/api/nodes/{id}/roles` | Set ChangedRoles | Admin |

### State & Assignments (on Stateful Nodes)

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| PUT | `/api/nodes/{id}/state` | Move node to different state | Passive |
| GET | `/api/nodes/{id}/assignees` | Get assignees mapping | Passive |
| PUT | `/api/nodes/{id}/assignees` | Set assignees mapping | Admin |

### Users

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| GET | `/api/users` | List all users | Passive |
| GET | `/api/users/{id}` | Get user by ID | Passive |
| POST | `/api/users` | Create new user | Admin |
| PUT | `/api/users/{id}` | Update user | Admin |
| DELETE | `/api/users/{id}` | Delete user | Admin |

### Comments

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| GET | `/api/nodes/{nodeId}/comments` | List comments on node | Passive |
| POST | `/api/nodes/{nodeId}/comments` | Add comment to node | Passive |

### Request/Response Patterns

#### Get Root Node ID
```
GET /api/nodes/root_id
```
Response: `{ "id": "uuid" }`

#### Create Node
```
POST /api/nodes
Body: { "parentId": "uuid", "type": "uuid", "workflow": "uuid", "manifest": "...", "caption": "...", "description": "...", "guardrails": "..." }
```

#### Update Node
```
PUT /api/nodes/{id}
Body: { "manifest": "...", "caption": "...", "description": "..." }
```

#### Update Guardrails
```
PUT /api/nodes/{id}/guardrails
Body: { "guardrails": "..." }
```

#### Set State
```
PUT /api/nodes/{id}/state
Body: { "targetStateId": "uuid" }
```

#### Set Assignees
```
PUT /api/nodes/{id}/assignees
Body: { "assignees": { "roleId1": "userId1", "roleId2": "userId2" } }
```

#### Add Comment
```
POST /api/nodes/{nodeId}/comments
Body: { "text": "comment text" }
```

#### List Children (with pagination)
```
GET /api/nodes/{id}/children?skip=0&take=20
```

#### Poll for Assigned Changes (long-polling)
```
GET /api/nodes/assigned-to-me/poll?since=2024-01-01T00:00:00Z&timeout=30
```
Response: `{ "changed": true, "serverTimestamp": "2024-01-01T12:00:00Z" }`

- `since`: ISO 8601 timestamp of last check (optional)
- `timeout`: seconds to wait for changes (default 30, max 60)
- Returns immediately when any assigned node changes, or after timeout
- Use `serverTimestamp` from response for next poll's `since` parameter

#### List Comments (with pagination)
```
GET /api/nodes/{nodeId}/comments?skip=0&take=20&ordering=asc
```

#### Set ChangedTypes
```
PUT /api/nodes/{id}/types
Body: { "added": [{ "kind": "Structural", "name": "Project", "color": "#007bff" }], "removed": ["uuid"] }
```

#### Set ChangedWorkflows
```
PUT /api/nodes/{id}/workflows
Body: { 
  "added": [{ 
    "name": "Task Workflow", 
    "defaultStateId": "uuid", 
    "states": [
      { "name": "To Do", "isFinalSuccess": false, "isFinalFailure": false },
      { "name": "In Progress", "isFinalSuccess": false, "isFinalFailure": false },
      { "name": "Done", "isFinalSuccess": true, "isFinalFailure": false }
    ],
    "defaultForTypes": ["uuid"]
  }], 
  "removed": ["uuid"] 
}
```

#### Set ChangedRoles
```
PUT /api/nodes/{id}/roles
Body: { 
  "added": [{ 
    "name": "Developer", 
    "states": ["uuid-state-1", "uuid-state-2"]
  }], 
  "removed": ["uuid"] 
}
```

## Testing

### Testing Strategy

The project uses a multi-level testing approach:

1. **Unit Tests** - Test individual services and business logic in isolation
2. **Integration Tests** - Test API endpoints with in-memory database
3. **End-to-End Tests** - Test full workflows (optional, for critical paths)

**Test Frameworks:**
- xUnit for test framework
- FluentAssertions for assertions
- Moq for mocking
- TestServer or InMemoryDatabase for integration tests

**Test Organization:**
- `HinataProject.Tests.Unit` - Unit tests for business logic
- `HinataProject.Tests.Integration` - API integration tests
- Each public API endpoint should have corresponding tests

### Test Data Setup

All tests use a standardized setup pattern:

```csharp
// Base test fixture creates in-memory database
public class ApiTestBase
{
    protected HinataProjectDataContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<HinataProjectDataContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new HinataProjectDataContext(options);
    }

    protected (User User, Role Role) CreateSuperadmin(HinataProjectDataContext db)
    {
        var role = new Role { Name = "Superadmin" };
        var user = new User { Name = "Test Admin", Subject = "test-subject", Rights = Rights.Admin };
        user.Roles.Add(role);
        db.Users.Add(user);
        db.SaveChanges();
        return (user, role);
    }

    protected Node CreateRootNode(HinataProjectDataContext db, Role superadminRole)
    {
        var folderType = new Type { Kind = TypeKind.Structural, Name = "Folder" };
        var root = new Node 
        { 
            Name = "Root", 
            Type = folderType,
            ChangedRoles = new List<NodeRole> { new NodeRole { Role = superadminRole } }
        };
        db.Nodes.Add(root);
        db.SaveChanges();
        return root;
    }
}
```

### API Test Template

Each API test class follows this pattern:

```csharp
public class NodesControllerTests : ApiTestBase
{
    [Fact]
    public async Task CreateNode_WithValidData_ReturnsCreated()
    {
        // Arrange
        var (user, role) = CreateSuperadmin(_context);
        var parent = CreateRootNode(_context, role);
        
        var client = CreateAuthenticatedClient(user);
        var request = new CreateNodeRequest 
        { 
            ParentId = parent.Id,
            TypeId = typeId,
            Caption = "Test Node"
        };

        // Act
        var response = await client.PostAsJsonAsync("/nodes", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }
}
```

### Test Cases by Endpoint

#### Nodes

| Test | Description |
|------|-------------|
| `CreateNode_ValidData_ReturnsCreated` | Create node with all valid properties |
| `CreateNode_InvalidParent_ReturnsNotFound` | Parent node does not exist |
| `CreateNode_TypeNotAllowed_ReturnsBadRequest` | Type not in parent's InheritedTypes |
| `CreateNode_StatefulNoWorkflow_ReturnsBadRequest` | Stateful node without workflow |
| `ReadNode_Exists_ReturnsNode` | Get existing node by ID |
| `ReadNode_NotExists_ReturnsNotFound` | Node does not exist |
| `ListChildren_ValidParent_ReturnsChildren` | List children with pagination |
| `ListChildren_InvalidParent_ReturnsNotFound` | Parent does not exist |
| `ListAssignedToMe_ReturnsMatchingNodes` | Returns nodes where Assignee is current user |
| `UpdateNode_ValidData_ReturnsOk` | Update node properties |
| `UpdateNode_NoProperties_ReturnsBadRequest` | No properties provided |
| `UpdateNode_NotExists_ReturnsNotFound` | Node does not exist |
| `DeleteNode_Valid_ReturnsNoContent` | Delete node (recursive) |
| `DeleteNode_Root_ReturnsBadRequest` | Cannot delete Root |
| `DeleteNode_NotExists_ReturnsNotFound` | Node does not exist |

#### Settings

| Test | Description |
|------|-------------|
| `SetTypes_Valid_ReturnsOk` | Add/remove types on structural node |
| `SetTypes_NotStructural_ReturnsBadRequest` | Node is not structural |
| `SetTypes_TypeInUse_ReturnsBadRequest` | Type in use by descendants |
| `SetWorkflows_Valid_ReturnsOk` | Add/remove workflows |
| `SetWorkflows_NotStructural_ReturnsBadRequest` | Node is not structural |
| `SetWorkflows_WorkflowInUse_ReturnsBadRequest` | Workflow in use by descendants |
| `SetRoles_Valid_ReturnsOk` | Add/remove roles |
| `SetRoles_NotStructural_ReturnsBadRequest` | Node is not structural |
| `SetRoles_RoleInUse_ReturnsBadRequest` | Role assigned to descendants |

#### State & Assignments

| Test | Description |
|------|-------------|
| `SetState_Valid_ReturnsOk` | Move node to valid state |
| `SetState_NotStateful_ReturnsBadRequest` | Node is not stateful |
| `SetState_StateNotInWorkflow_ReturnsBadRequest` | State not in workflow |
| `SetState_UserNotAuthorized_ReturnsForbidden` | User lacks role for state |
| `GetAssignees_Stateful_ReturnsMapping` | Get assignee mapping |
| `GetAssignees_NotStateful_ReturnsBadRequest` | Node is not stateful |
| `SetAssignees_Valid_ReturnsOk` | Set role-to-user mapping |
| `SetAssignees_DuplicateRole_ReturnsBadRequest` | Role already assigned |
| `SetAssignees_InvalidRole_ReturnsBadRequest` | Role not in InheritedRoles |

#### Comments

| Test | Description |
|------|-------------|
| `AddComment_Valid_ReturnsCreated` | Add comment to node |
| `AddComment_EmptyText_ReturnsBadRequest` | Comment text is empty |
| `AddComment_NodeNotFound_ReturnsNotFound` | Node does not exist |
| `ListComments_ReturnsComments` | List comments with pagination |

#### Authentication & Authorization

| Test | Description |
|------|-------------|
| `NoToken_ReturnsUnauthorized` | Request without JWT |
| `InvalidToken_ReturnsUnauthorized` | Invalid JWT token |
| `PassiveRole_CanRead_ReturnsOk` | Passive auth can read |
| `ActiveRole_CanCreate_ReturnsOk` | Active auth can create/update |
| `ActiveRole_CannotDelete_ReturnsForbidden` | Active cannot delete |
| `AdminRole_CanDelete_ReturnsOk` | Admin can delete |

### Running Tests

```bash
# Run all tests
dotnet test

# Run unit tests only
dotnet test --filter "Category=Unit"

# Run integration tests only
dotnet test --filter "Category=Integration"

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

## Coding Guidelines

### Project Structure

```
HinataProject/
├── Api/                    # REST endpoints (minimal APIs)
├── Domain/                 # Domain entities, interfaces, core types
├── Persistence/            # EF Core DbContext, migrations
└── Tests/                  # Unit and integration tests
```

### Domain Layer

#### Entity Interface Pattern

All entities implement `IId<Guid>` for consistent ID handling:

```csharp
// Domain/Core/IId.cs
public interface IId<TValue> : IReadOnlyId<TValue>
{
    public TValue Id { get; set; }
}

// Entity implementation
public class Node : IId<Guid>
{
    public Guid Id { get; set; }
    // ... other properties
}
```

#### Audit Fields

Implement `IHaveCreatedDate` and/or `IHaveLastModifiedDate` for automatic timestamps:

```csharp
public class AuditLogEntry : IId<Guid>, IHaveCreatedDate, IHaveLastModifiedDate
{
    public Guid Id { get; set; }
    public Offset CreatedAt { get; set; }
    public Offset LastModifiedAt { get; set; }
    // ... other properties
}
```

The DbContext automatically populates these fields on save.

#### Paged Results

Use `PagedResult<T>` for paginated responses:

```csharp
public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int? TotalCount { get; set; }
}
```

### Persistence Layer

#### DbContext Configuration

The `HinataProjectDataContext` applies conventions automatically:

- Snake_case table/column naming
- `NoTrackingWithIdentityResolution` query behavior
- GUID v7 generation for `IId<Guid>` entities
- Enum-to-string conversion

```csharp
public class HinataProjectDataContext : DbContext
{
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSnakeCaseNamingConvention();
        optionsBuilder.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTrackingWithIdentityResolution);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Auto-generate GUID v7 for IId<Guid> entities
        foreach (var mutableEntityType in modelBuilder.Model.GetEntityTypes())
        {
            if (mutableEntityType.ClrType.IsAssignableTo(typeof(IId<Guid>)))
                mutableEntityType.GetProperty(nameof(IId<Guid>.Id))
                    .SetValueGeneratorFactory((_, _) => new GuidV7Generator());

            // Enum to string conversion
            foreach (var enumProperty in mutableEntityType.GetProperties()
                         .Where(q => (Nullable.GetUnderlyingType(q.ClrType) ?? q.ClrType).IsEnum))
                modelBuilder.Entity(mutableEntityType.ClrType).Property(enumProperty.Name).HasConversion<string>();
        }
    }
}
```

#### Audit Field Implementation

Override `SaveChangesAsync` to automatically populate timestamps:

```csharp
public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess,
    CancellationToken cancellationToken = new CancellationToken())
{
    var now = this.timeProvider.GetUtcNow();

    foreach (var entityEntry in this.ChangeTracker.Entries())
    {
        switch (entityEntry.State)
        {
            case EntityState.Added:
                if (entityEntry.Entity is IHaveLastModifiedDate)
                {
                    var lastModifiedProp = entityEntry.Property(nameof(IHaveLastModifiedDate.LastModifiedAt));
                    lastModifiedProp.CurrentValue = now;
                }
                if (entityEntry.Entity is IHaveCreatedDate)
                {
                    var createdProp = entityEntry.Property(nameof(IHaveCreatedModifiedDate.CreatedAt));
                    createdProp.CurrentValue = now;
                }
                break;
            case EntityState.Modified when entityEntry.Entity is IHaveLastModifiedDate:
                var lastModProp = entityEntry.Property(nameof(IHaveLastModifiedDate.LastModifiedAt));
                lastModProp.CurrentValue = now;
                break;
        }
    }

    return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
}
```

### API Layer

#### Endpoint Discovery Pattern

Endpoints implement `IDiscoverableEndpoint` or `IDiscoverableEndpoint<TGroup>`:

```csharp
// Group interface for grouping related endpoints
public interface IDiscoverableGroup
{
    Task<RouteGroupBuilder> MapGroup(WebApplication app);
}

// Endpoint interface
public interface IDiscoverableEndpoint
{
    Task MapEndpoint(IEndpointRouteBuilder routeBuilder);
}

public interface IDiscoverableEndpoint<T> : IDiscoverableEndpoint where T: IDiscoverableGroup
{
    // Group-specific endpoint marker
}
```

#### Endpoint Registration

Endpoints are auto-discovered and registered in `Program.cs`:

```csharp
// In Program.cs
builder.AddEndpoints(typeof(Program).Assembly);

// Endpoints are registered as singleton services automatically
// MapAllEndpoints() discovers and maps them
```

#### Error Handling

Use ProblemDetails for consistent error responses. The middleware `ExceptionToProblemDetailsHandler` automatically converts exceptions:

```csharp
public class ExceptionToProblemDetailsHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        // In development: expose full error details
        // In production: generic message with error key
        var isDevelopment = webHostEnvironment.IsDevelopment() || webHostEnvironment.IsEnvironment("Testing");
        
        // Returns ProblemDetails JSON with appropriate status code
    }
}
```

#### Authentication/Authorization

Uses OAuth 2.0 / JWT authentication:

```csharp
// In Program.cs
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => { ... });
```

Authorization uses role-based access (Admin, Active, Passive as defined in Business Logic).

#### Static Files (Frontend)

Static files hosting the frontend will be served from the "wwwroot" directory. These static files will be mapped to the root prefix "/". If a file does not exist, "index.html" will be served instead.

### Testing

#### Test Fixtures

Use provided test fixtures for integration tests:

```csharp
public class NodesControllerTests : IClassFixture<WebApplicationTestFixture>
{
    private readonly WebApplicationTestFixture _fixture;
    
    public NodesControllerTests(WebApplicationTestFixture fixture)
    {
        _fixture = fixture;
    }
}
```

#### Database Strategies

Multiple strategies available for test database setup:

```csharp
// PostgreSQL Container (recommended for integration tests)
public class PostgreSqlContainerStrategy : IDatabaseSetupStrategy

// In-memory database (for unit tests)
options.UseInMemoryDatabase(Guid.NewGuid().ToString())
```

#### Testing Authenticated Requests

Create authenticated test clients:

```csharp
var client = _fixture.Factory.CreateClient();
// Add JWT token to request headers
client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
```

### Naming Conventions

- **Classes/Interfaces**: PascalCase (`HinataProjectDataContext`, `IDiscoverableEndpoint`)
- **Properties**: PascalCase (`ParentId`, `InheritedTypes`)
- **Database tables/columns**: snake_case (handled automatically by EF)
- **Enums**: PascalCase, stored as strings in DB
- **GUIDs**: Use `GuidV7Generator` for time-sortable IDs

### Configuration

- Environment variables use prefix `HinataProject_`
- JSON config files in `Api/Configuration/`
- Use `IConfiguration` for dependency injection

