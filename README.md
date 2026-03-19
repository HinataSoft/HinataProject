# HinataProject

> A project management and task tracking system designed for both humans and AI agents

## What is HinataProject?

HinataProject is a flexible project management tool that helps teams organize work, track tasks, and manage bugs. What makes it special is that it treats AI agents as first-class users—meaning both humans and machines can create, update, and coordinate work items through the same system.

Think of it as a smart whiteboard where you can:
- Create projects and organize work in a tree structure
- Define custom task types (like Tasks, Bugs, Features)
- Set up workflows (like "To Do" → "In Progress" → "Done")
- Assign work to team members
- Let AI agents help automate and track progress

## Quick Start

### How it Works

Everything in HinataProject is organized as a **tree**. At the top is a special "Root" node, and all your projects, tasks, and bugs branch off from there.

```
Root
├── Project: Website Redesign
│   ├── Epic: User Authentication
│   │   ├── Task: Implement login form
│   │   ├── Task: Add password reset
│   │   └── Bug: Session timeout issue
│   └── Epic: Homepage
│       ├── Task: Design new hero section
│       └── Feature: Animated carousel
└── Project: Mobile App
    └── ...
```

### Key Concepts

#### Nodes – The Building Blocks

Every item in the system is called a **Node**. A Node can represent:
- A **Folder** (container for organizing)
- A **Project** (big picture work)
- An **Epic** (large feature area)
- A **Task** (specific work item)
- A **Bug** (something to fix)
- Or any custom type you define!

#### Structural vs. Stateful Nodes

**Structural nodes** are containers that hold settings. They define what types of work items are available below them. Examples: Folder, Project, Epic.

**Stateful nodes** are actual work items with a status. They have a workflow state (like "In Progress") and assignees. Examples: Task, Bug, Feature.

#### Inheritance – Settings That Flow Down

Here's a powerful feature: when you set up types, workflows, or roles on a structural node (like a Project), all its children automatically inherit those settings. You don't need to configure each task individually.

For example:
1. Create a Project node
2. Add "Task" and "Bug" types to it
3. Now every item under that project can be a Task or Bug

#### Workflows – Defining Progress

A **Workflow** is a sequence of states that work items go through. Common example:

```
To Do → In Progress → Code Review → Done
         ↓
      Blocked
```

You can define custom workflows for each project, with as many states as you need.

#### Guardrails – Rules for AI Agents

Admins can set **Guardrails** on any node. Guardrails are constraints and instructions that guide AI agents working on that node — for example, coding standards, review checklists, or boundaries on what changes are acceptable. Only Admins can edit guardrails, ensuring consistent governance over automated work.

#### Roles & Assignments

**Roles** define what people (or AI agents) can do:
- **Admin** – Full control, can delete nodes and manage settings
- **Active** – Can create and update work items
- **Passive** – Can view, change states and comment

Each work item can have **Assignees** – specific people mapped to roles. When a task moves to a new state, the system automatically knows who should be responsible based on the role-state mapping.

## For Users

### Getting Started

1. **Sign in** using OAuth (your organization likely has this set up)
2. You'll see the **Root** node – this is your starting point
3. Create a new **Project** under Root
4. Configure what types of work items are available (Tasks? Bugs? Features?)
5. Add team members and assign roles
6. Start creating work items!

### Working with Tasks

1. **Create a task** – Under your project, create a new node and select "Task"
2. **Assign it** – Map a role to a team member
3. **Update progress** – Move it through your workflow states
4. **Comment** – Add notes, questions, or updates
5. **Track history** – See who changed what and when (audit log)

### Finding Your Work

Use "Assigned To Me" to quickly see all tasks where you're the current responsible person based on the workflow state.

## For Developers

### Tech Stack

- **Backend**: ASP.NET Core (C#)
- **Frontend**: React (Single Page App)
- **Database**: PostgreSQL
- **API**: RESTful JSON API

### API Overview

All operations are available via REST API. This means AI agents can programmatically:
- Read and search for nodes
- Create new work items
- Update properties
- Change workflow states
- Add and list comments
- Manage assignments

#### Example: Creating a Task

```bash
POST /api/nodes
{
  "parentId": "project-uuid",
  "type": "task-type-uuid",
  "workflow": "workflow-uuid",
  "caption": "Implement user login",
  "description": "Add login functionality with email and password"
}
```

#### Example: Moving a Task Forward

```bash
PUT /api/nodes/task-uuid/state
{
  "targetStateId": "in-progress-state-uuid"
}
```

### Authentication

The system uses JWT tokens via OAuth. Both humans and AI agents need valid tokens to interact with the API.

## Architecture Highlights

### Database Schema

The system uses a clean entity relationship:
- **Nodes** – The main work items (hierarchical, tree structure)
- **Types** – Defines if a node is Structural or Stateful
- **Workflows** – Collection of states
- **States** – Individual states in a workflow
- **Roles** – Permissions (Admin/Active/Passive)
- **Users** – People or machines
- **Comments** – Discussion on nodes
- **AuditLog** – Tracks all changes

### Key Design Decisions

1. **Flexible Types** – Nothing is hardcoded. You define what "Task" or "Bug" means for your team.
2. **Inheritance** – Settings flow down the tree, reducing repetive work.
3. **Audit Trail** – Every change is logged: who, what, when.
4. **AI-Friendly** – Full API access means AI agents can integrate and automate workflows.

## Installation & Setup

### First-Time Setup

1. Configure your database connection
2. Set up OAuth provider settings
3. The system creates a **Superadmin** user automatically from your OAuth subject
4. Sign in as Superadmin to create the first projects

### Configuration

Settings are loaded from:
- `appsettings.json` (or environment variables)
- Environment variables prefixed with `HinataProject_`

Required settings:
- Database connection string
- OAuth provider details (issuer, audience, connect URI)
- First user's OAuth subject (for Superadmin)

## Common Tasks

### Creating a New Project

1. Get the Root node (`GET /api/nodes/root`)
2. Create a new node under Root with a structural type (e.g., "Project")
3. On the Project node, configure:
   - **ChangedTypes**: Add "Task", "Bug", "Feature"
   - **ChangedWorkflows**: Add your workflow
   - **ChangedRoles**: Add team roles

### Adding a Team Member

1. Create a User (or they authenticate via OAuth)
2. On a structural node, add the role to ChangedRoles
3. Assign the user to that role on specific work items

### Moving Work Forward

1. Find a task (or list "Assigned To Me")
2. Check the current state and available next states
3. Call the state transition endpoint
4. The Assignee automatically updates based on role-state mapping

## Support

For technical details, API reference, and development guidelines, see the full [MANIFEST.md](./MANIFEST.md).

---

Built with 🐱🐱 for teams that work with both humans and AI agents
