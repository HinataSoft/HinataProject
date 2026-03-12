# HinataProject Frontend Specification

## Technology Stack

### Core
- **React 18+** - UI library
- **Vite** - Build tool and dev server
- **TypeScript** - Type safety

### Routing
- **React Router v6** - Client-side routing

### Data Fetching
- **TanStack Query (React Query)** - Server state management
  - Automatic caching and invalidation
  - Loading/error states handling
  - Optimistic updates support

### UI Framework
- **Material UI (MUI)** - Component library
  - Dense, information-rich components suitable for technical users
  - Comprehensive documentation
  - Easy theming
  - Data Grid for node listings

### State Management
- **Zustand** - Lightweight client state
  - Simple API, minimal boilerplate
  - Good TypeScript support

## Project Structure

```
src/
├── api/                 # API client and hooks
│   ├── client.ts        # Axios/fetch wrapper
│   ├── nodes.ts         # Node API calls
│   ├── comments.ts      # Comment API calls
│   └── useQueries.ts    # TanStack Query hooks
├── components/          # Reusable UI components
│   ├── common/          # Generic components
│   ├── nodes/           # Node-specific components
│   └── layout/          # Layout components
├── pages/               # Route pages
├── stores/              # Zustand stores
├── theme/               # MUI theme customization
├── types/               # TypeScript interfaces
├── utils/               # Helper functions
├── App.tsx
└── main.tsx
```

## API Integration

### Client Configuration
```typescript
// api/client.ts
import axios from 'axios';

const client = axios.create({
  baseURL: import.meta.env.VITE_API_URL || '/api',
});

// Add JWT token to requests
client.interceptors.request.use((config) => {
  const token = localStorage.getItem('token');
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

export default client;
```

### TanStack Query Hooks
```typescript
// api/useQueries.ts
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import client from './client';

// Node queries
export function useNode(id: string) {
  return useQuery({
    queryKey: ['node', id],
    queryFn: () => client.get(`/nodes/${id}`).then(r => r.data),
  });
}

export function useNodeChildren(id: string, skip = 0, take = 20) {
  return useQuery({
    queryKey: ['node', id, 'children', skip, take],
    queryFn: () => client.get(`/nodes/${id}/children`, { params: { skip, take } }).then(r => r.data),
  });
}

// Mutations
export function useCreateNode() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data) => client.post('/nodes', data),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['node'] }),
  });
}
```

## Key Features Implementation

### Authentication
- Store JWT token in localStorage
- Add token to requests via axios interceptor
- Show login page for unauthenticated users
- Redirect to OAuth provider for authentication

### Node Tree Navigation
- Expandable/collapsible tree view for navigation
- Lazy loading of children on expand
- Breadcrumb showing current path

### Node Detail View
- Tabbed interface:
  - Overview (properties, description)
  - Comments
  - History/Audit Log
- Edit inline or via modal dialog

### Forms
- Use MUI form components
- Validation with React Hook Form (optional, can use simple validation)
- Confirmation dialogs for destructive actions

## UI Components

### Layout
- **AppBar** - Top navigation with user menu
- **Sidebar** - Node tree navigation (collapsible)
- **MainContent** - Page content area

### Node Components
- **NodeCard** - Summary card for node listings
- **NodeTree** - Recursive tree component
- **NodeForm** - Create/edit node form
- **NodeDetail** - Full node details view

### Common Components
- **LoadingSpinner** - Loading state indicator
- **ErrorAlert** - Error message display
- **ConfirmDialog** - Confirmation for destructive actions
- **Pagination** - Reusable pagination controls

## Routing

```
/                           # Home / Dashboard
/login                      # Login page
/nodes                      # Node browser
/nodes/:id                  # Node detail
/nodes/:id/children         # Node children (optional)
// ... settings, profile, etc.
```

## State Management

### Auth Store (Zustand)
```typescript
// stores/authStore.ts
import { create } from 'zustand';

interface AuthState {
  user: User | null;
  token: string | null;
  isAuthenticated: boolean;
  login: (token: string, user: User) => void;
  logout: () => void;
}

export const useAuthStore = create<AuthState>((set) => ({
  user: null,
  token: localStorage.getItem('token'),
  isAuthenticated: !!localStorage.getItem('token'),
  login: (token, user) => {
    localStorage.setItem('token', token);
    set({ token, user, isAuthenticated: true });
  },
  logout: () => {
    localStorage.removeItem('token');
    set({ token: null, user: null, isAuthenticated: false });
  },
}));
```

## Testing

### Framework
- **Vitest** - Test runner (pairs well with Vite)
- **React Testing Library** - Component testing
- **MSW** - Mock service worker for API mocking

### Test Structure
```
tests/
├── components/       # Component tests
├── hooks/            # Custom hook tests
├── pages/            # Page tests
└── setup.ts          # Test setup
```

## Build & Deployment

### Development
```bash
npm run dev          # Start dev server
npm run build        # Build for production
npm run preview      # Preview production build
```

### Static File Serving
The built frontend can be served as static files:
- `npm run build` outputs to `dist/` folder
- Configure backend to serve `index.html` for all non-API routes
- Or use nginx/CDN for frontend hosting

## Configuration

### Environment Variables
```
VITE_API_URL=http://localhost:5000/api
VITE_OAUTH_PROVIDER_URL=...
```

## Recommended Dependencies

```json
{
  "dependencies": {
    "react": "^18.2.0",
    "react-dom": "^18.2.0",
    "react-router-dom": "^6.x",
    "@tanstack/react-query": "^5.x",
    "@mui/material": "^5.x",
    "@mui/icons-material": "^5.x",
    "@emotion/react": "^11.x",
    "@emotion/styled": "^11.x",
    "zustand": "^4.x",
    "axios": "^1.x"
  },
  "devDependencies": {
    "vite": "^5.x",
    "typescript": "^5.x",
    "@types/react": "^18.x",
    "@types/react-dom": "^18.x",
    "@testing-library/react": "^14.x",
    "msw": "^2.x"
  }
}
```

## Development Workflow

1. **Setup**: `npm install`
2. **Dev**: `npm run dev` - starts Vite dev server
3. **Build**: `npm run build` - creates production build (outputs to `../wwwroot/`)
4. **Test**: `npm run test` - runs Vitest tests

### Building for Production

To build the frontend and generate static files:

```bash
# From the project root
npm --prefix "Api/FrontEnd" run build

# Or from the FrontEnd directory
cd Api/FrontEnd
npm run build
```

The build output is placed in `Api/wwwroot/`:
- `index.html` - Main HTML file
- `assets/index-*.js` - Bundled JavaScript

**Note**: The build command uses the Vite configuration and outputs directly to the wwwroot folder for serving by the backend.

## Style Guidelines

- Use MUI components with default theme (can customize colors later)
- Prefer functional components with hooks
- Use TypeScript for all components
- Keep components small and focused
- Extract reusable logic into custom hooks
- Use TanStack Query for all server state

## nginx Config Example

``nginx
server {
    listen 80;
    root /var/www/hinata-frontend;
    index index.html;

    # Serve static files
    location / {
        try_files $uri $uri/ /index.html;
    }

    # Proxy API calls to backend
    location /api {
        proxy_pass http://localhost:5000;
    }
}
`
**Important**: The try_files $uri $uri/ /index.html` ensures client-side routing works - any URL falls back to index.html, then React Router handles it.

## Page Layout

### Node

The main view is Node View. It will show one node at a time with full details. Here's the page layout in ASCII art:

```
< Parent link
[TYPE] [State] Caption                 [...]

[Node children]                 |----------|
                                | comment  |
Description edit field          |----------|
                                | comment  |
Manifest edit field             |----------|
                                | etc.     |
List of assigness:              |          |
|------------------|            |----------|
| [Role] | [User]  |            | add new  |
|------------------|            | comment  |
                                |----------|
```

Where
- `[TYPE]` is a Type name (in capitals and in color taken from type)
- `[State]` is state name in a box
- `[...]` is a hamburger menu with dropdown items:
  - Copy link (copies link to clipboard to this page)
  - Edit types (opens dialog to manage node types - see below)
  - Edit workflows (opens dialog to manage workflows - see below)
  - Edit roles (opens dialog to manage roles - see below)
  - Edit users (opens dialog to manage users - see below; available only for Admin users)
- [Node children] is a compact clickable list of children; on click it would lead to node detail page like this
- List of assignees is a table where roles from current workflow is listed and with users to choose for each role; available only if workflow is set; editable for Admin, readonly for Active and Passive

### Edit Types Dialog

The "Edit types" menu item opens a dialog with two sections:

**Added Section**
- Lists newly added types specific to this node
- Each type shows: colored chip with name, kind (Structural/Stateful)
- Delete button (X) to remove the type from the node
- "Add new type" row to create brand new types:
  - Text input for type name
  - Dropdown for Kind (Structural/Stateful)
  - Color picker
  - Add button

**Disabled Section**
- Lists all types from `InheritedTypes` (types inherited from parent nodes)
- Checkbox for each type - checking it adds the type to `ChangedTypes.removed`
- Disabled types will NOT be available for children of this node

This allows nodes to:
1. Add new types that can be used by their children
2. Disable (remove) inherited types so children cannot use them

### Edit Workflows Dialog

The "Edit workflows" menu item opens a dialog with two sections:

**Added Section**
- Lists newly added workflows specific to this node
- Each workflow is displayed as a card with:
  - Workflow name in header with delete button (X)
  - States list showing all states in this workflow
  - "Add State" button to create new states
  - Default State selector (click a state to make it default)
  - Default For Types selector (click types to mark as default)

**Creating New States**
- Click "Add State" to reveal an inline form
- Enter state name
- Select Final Status: None, Success, or Failure
- Click "Add" to create the state (not saved until global "Save")
- States display with icons: green checkmark (Success), red checkmark (Failure)

**Disabled Section**
- Lists all workflows from `InheritedWorkflows` (workflows inherited from parent nodes)
- Checkbox for each workflow - checking it adds the workflow to `ChangedWorkflows.removed`
- Disabled workflows will NOT be available for children of this node

This allows nodes to:
1. Create brand new workflows with custom states
2. Set default state for new nodes using this workflow
3. Mark workflow as default for specific types
4. Disable (remove) inherited workflows so children cannot use them

### Edit Roles Dialog

The "Edit roles" menu item opens a dialog with two sections:

**Added Section**
- Lists newly added roles specific to this node
- Each role shows: name, associated states
- Delete button (X) to remove the role from the node
- "Add new role" row to create brand new roles:
  - Text input for role name
  - Multi-select for States (from inherited workflows)
  - Add button

**Disabled Section**
- Lists all roles from `InheritedRoles` (roles inherited from parent nodes)
- Checkbox for each role - checking it adds the role to `ChangedRoles.removed`
- Disabled roles will NOT be available for children of this node

This allows nodes to:
1. Add new roles that can be used with their children
2. Define which states each role corresponds to
3. Disable (remove) inherited roles so children cannot use them

### Edit Users Dialog

The "Edit users" menu item opens a dialog for managing users in the system. This dialog is available only for users with Admin rights.

**User List Section**
- Lists all existing users in the system
- Each user shows: name, subject (OAuth subject), rights level (Admin/Active/Passive), associated roles
- Delete button (X) to remove a user from the system
- Confirmation dialog appears before deleting a user

**Add New User Section**
- Text input for user name (required)
- Text input for subject (OAuth subject value, required)
- Dropdown for Rights (Admin/Active/Passive)
- Add button to create the new user

**Behavior**
- Users can be created with Name, Subject, and Rights properties
- Rights determines what the user can or cannot do (Admin/Active/Passive)
- Users start with no roles assigned (roles are assigned separately through node assignees)
- Cannot delete the last Admin user (system protection)
- Creating/deleting users is recorded in the audit log

This allows admins to:
1. View all users in the system
2. Add new users (for AI agents or additional human users)
3. Set user rights levels
4. Remove users who no longer need access
