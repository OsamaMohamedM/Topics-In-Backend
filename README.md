# 📋 TaskBoard API

**A hands-on reference project demonstrating Clean Architecture + CQRS + MediatR in .NET 10**

![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4?style=flat&logo=dotnet&logoColor=white)
![ASP.NET Core](https://img.shields.io/badge/ASP.NET%20Core-10.0-512BD4?style=flat&logo=dotnet&logoColor=white)
![EF Core](https://img.shields.io/badge/EF%20Core-10.0-512BD4?style=flat&logo=dotnet&logoColor=white)
![MediatR](https://img.shields.io/badge/MediatR-12.x-blue?style=flat)
![FluentValidation](https://img.shields.io/badge/FluentValidation-11.x-blue?style=flat)
![SQL Server](https://img.shields.io/badge/SQL%20Server-LocalDB-CC2927?style=flat&logo=microsoftsqlserver&logoColor=white)
---

## 🔭 Overview

TaskBoard API is a project management REST API that lets you create projects, add tasks to them, and track task completion. The real goal is not the task board itself — it is the **architecture behind it**.

**Who is this for?** Mid-level .NET developers who understand C# and ASP.NET Core basics but want to learn how to structure a real-world application using proven architectural patterns.

**What I will learn:**

- How to implement **Clean Architecture** with strict layer separation and the dependency rule
- How to apply **CQRS** to separate read operations from write operations
- How to use the **Mediator pattern** (via MediatR) to decouple controllers from business logic
- How to build a **pipeline of cross-cutting concerns** (logging, validation, transactions, performance monitoring) that runs automatically on every request
- How to enforce **domain invariants** inside aggregate roots
- How to handle errors globally with structured, consistent API responses

## 🏛️ Architectural Concepts Covered

### Clean Architecture

Clean Architecture organizes code into concentric layers where **dependencies point inward** — outer layers depend on inner layers, never the reverse. The innermost layer (Domain) contains business rules and has zero knowledge of databases, frameworks, or HTTP. The outermost layers (Infrastructure and API) deal with real-world concerns like SQL, JSON serialization, and HTTP routing.

The key insight: your business logic is protected from change. You can swap SQL Server for PostgreSQL, replace REST with gRPC, or switch from MediatR to another library — and the Domain layer never changes. This is the **Dependency Rule** in action.

Dependencies flow INWARD →  (outer layers depend on inner layers)
```

---

### CQRS (Command Query Responsibility Segregation)

CQRS splits your application's operations into two distinct categories: **Commands** that change state and **Queries** that read state. Instead of a single service class doing both reads and writes, each operation gets its own dedicated request object and handler. This separation makes the code easier to reason about, test, and optimize independently.

The practical benefit: you can optimize your read path (e.g., using raw SQL or read replicas) without affecting your write path (which needs to go through domain logic and validation).

| Aspect | Command | Query |
|---|---|---|
| **Purpose** | Modify data (create, update, delete) | Retrieve data (read) |
| **Returns** | Confirmation (ID, Unit) | Data (DTOs) |
| **Side Effects** | Yes — changes database state | No — pure read operation |
| **Validation** | FluentValidation runs in pipeline | Typically none |
| **Transaction** | Wrapped in DB transaction | Skipped by TransactionBehavior |
| **Example** | `CreateProjectCommand` → returns `Guid` | `GetProjectDetailsQuery` → returns `ProjectDetailsDto` |

---

### Mediator Pattern (MediatR)

The Mediator pattern introduces a middleman between the sender of a request and its handler. In this project, controllers never call business logic directly — they create a command or query object and send it through MediatR. MediatR resolves the correct handler at runtime via dependency injection and routes the request to it. This eliminates tight coupling between the API layer and the Application layer.

**How it connects Commands/Queries to Handlers:**

```
Controller                       MediatR                         Handler
    │                               │                               │
    │  Send(CreateProjectCommand)   │                               │
    │──────────────────────────────▶│                               │
    │                               │  resolve IRequestHandler<>   │
    │                               │──────────────────────────────▶│
    │                               │                               │
    │                               │      return Guid (projectId)  │
    │                               │◀──────────────────────────────│
    │      Guid (projectId)         │                               │
    │◀──────────────────────────────│                               │
```

The pipeline behaviors intercept every request **before and after** the handler executes. They form a chain of responsibility — each behavior wraps the next one, creating a Russian‑nesting‑doll structure:

```
1. Request enters the pipeline
2.   └─▶ LoggingBehavior starts timer, logs "[START]"
3.       └─▶ ValidationBehavior checks FluentValidation rules
4.           └─▶ TransactionBehavior begins DB transaction (commands only)
5.               └─▶ PerformanceBehavior starts stopwatch
6.                   └─▶ HANDLER EXECUTES (business logic runs here)
7.               └─◀ PerformanceBehavior warns if > 300ms
8.           └─◀ TransactionBehavior commits (or rolls back on exception)
9.       └─◀ (validation already complete)
10.  └─◀ LoggingBehavior logs "[END] Handled in {X}ms"
11. Response returns to controller
```

---

## ⚙️ Pipeline Behaviors

Every request that passes through MediatR traverses a pipeline of four behaviors. The order is determined by their registration order in `DependencyInjection.cs`. Each behavior wraps the next, so the first registered is the outermost layer:



| Behavior | Applies To | What It Does | On Failure |
|---|---|---|---|
| **LoggingBehavior** | All requests | Logs start/end timestamps and total elapsed time using `ILogger` and `Stopwatch` | Logs normally; exception propagates up |
| **ValidationBehavior** | Requests with registered `IValidator<T>` | Runs all FluentValidation validators concurrently via `Task.WhenAll`. Collects all failures into a single list | Throws `ValidationException` with all error messages → caught by middleware → HTTP 400 |
| **TransactionBehavior** | Commands only (name ends with `"Command"`) | Begins a database transaction before the handler, commits after success | Rolls back transaction, logs the error, re-throws the exception |
| **PerformanceBehavior** | All requests | Measures handler execution time using `Stopwatch` with a 300ms threshold | Logs a warning if execution exceeds threshold; does not throw |

---

## 📁 Project Structure

```
/
│
├── dotnet-clean-architecture-cqrs.csproj    # Root web project — references all layer projects
├── Program.cs                                # Application entry point — configures DI, middleware, Swagger
├── appsettings.json                          # Configuration — connection strings, logging levels
│
├── Domain/                     # ── INNERMOST LAYER: pure business logic, zero dependencies ──
│       ├── Aggregates/
│      │   └── Project.cs                    # Aggregate root — owns tasks, enforces 20-task limit
│      ├── Entities/
│      │   └── TaskItem.cs                   # Entity — belongs to Project, has status & priority
│      ├── Enums/
│      │   ├── TaskItemStatus.cs             # Pending → InProgress → Completed
│      │   └── TaskPriority.cs               # Low, Medium, High
│      ├── Exceptions/
│      │   └── DomainException.cs            # Custom exception for domain rule violations
│      └── Interfaces/
│          └── IProjectRepository.cs         # Repository contract — defined here, implemented elsewhere
│   
├── Application/              Application/                # ── USE CASE LAYER: orchestrates domain operations ──
│      ├── Commands/
│      │   ├── CreateProject/
│      │   │   ├── CreateProjectCommand.cs           # Request record: Name, Description, Owner → Guid
│      │   │   ├── CreateProjectCommandHandler.cs    # Creates Project aggregate, persists, returns ID
│      │   │   └── CreateProjectCommandValidator.cs  # Name required (≤100 chars), Owner required
│      │   ├── AddTaskToProject/
│      │   │   ├── AddTaskToProjectCommand.cs            # Request: ProjectId, Title, Priority → Guid
│      │   │   ├── AddTaskToProjectCommandHandler.cs     # Loads project, calls AddTask(), saves
│      │   │   └── AddTaskToProjectCommandValidator.cs   # ProjectId required, Title ≤200, Priority in enum
│      │   └── CompleteTask/
│      │       ├── CompleteTaskCommand.cs             # Request: TaskId → Unit (void)
│      │       └── CompleteTaskCommandHandler.cs      # Finds task via project, calls MarkAsCompleted()
│      ├── Queries/
│      │   └── GetProjectDetails/
│      │       ├── GetProjectDetailsQuery.cs          # Request: ProjectId → ProjectDetailsDto
│      │       ├── GetProjectDetailsQueryHandler.cs   # Loads project with tasks, maps to DTO
│      │       └── ProjectDetailsDto.cs               # Response shape: project info + task list + stats
│      ├── Behaviors/
│      │   ├── LoggingBehavior.cs             # Wraps all requests — logs start/end + duration
│      │   ├── ValidationBehavior.cs          # Runs FluentValidation rules before handler
│      │   ├── TransactionBehavior.cs         # Wraps commands in DB transaction (+ ITransactionManager)
│      │   └── PerformanceBehavior.cs         # Warns if handler exceeds 300ms threshold
│      └── DependencyInjection.cs             # Registers MediatR, behaviors (in order), validators
│   
├── Infrastructure/              # ── DATA ACCESS LAYER: implements domain interfaces ──
│      ├── Persistence/
│      │   │   ├── AppDbContext.cs                # EF Core DbContext — DbSet<Project>, DbSet<TaskItem>
│      │   │   ├── ProjectRepository.cs           # Implements IProjectRepository using EF Core
│      │   │   ├── EfTransactionManager.cs        # Implements ITransactionManager for DB transactions
│      │   │   └── Configurations/
│      │   │       ├── ProjectConfiguration.cs    # EF Core fluent config for Project table
│      │   │       └── TaskItemConfiguration.cs   # EF Core fluent config for TaskItem table
│      └── DependencyInjection.cs             # Registers DbContext, repository, transaction manager
├── API/                       # ── OUTERMOST LAYER: HTTP concerns only ──
│       ├── Controllers/
│       │   ├── ProjectsController.cs          # POST /api/projects, GET /api/projects/{id}
│       │   └── TasksController.cs             # POST /api/projects/{id}/tasks, PUT /api/tasks/{id}/complete
│       └── Middleware/
│           └── GlobalExceptionHandlingMiddleware.cs  # Maps exceptions → structured HTTP error responses
```

---

## 🧩 Layer Responsibilities

### Domain Layer

**Responsibility:** Contains the core business rules and domain model. This is the heart of the application — everything else exists to serve this layer.

**What is allowed here:**
- Entity and aggregate root definitions with encapsulated state
- Business rule enforcement (e.g., max 20 tasks per project, no re-completing tasks)
- Domain exceptions for rule violations
- Repository interfaces (contracts only — no implementations)
- Enums representing domain concepts

**What is strictly forbidden here:**
- ❌ References to any framework (EF Core, ASP.NET, MediatR)
- ❌ Database logic, SQL, or ORM attributes
- ❌ HTTP concepts (controllers, status codes, JSON)
- ❌ Dependency on any outer layer



### Application Layer

**Responsibility:** Orchestrates use cases by coordinating domain objects and infrastructure services. Each use case is a command or query with a dedicated handler. Cross-cutting concerns (logging, validation, transactions, performance) are handled by pipeline behaviors.

**What is allowed here:**
- Command and query records (implementing `IRequest<T>`)
- Handlers (implementing `IRequestHandler<TRequest, TResponse>`)
- FluentValidation validators
- DTOs for query responses
- Pipeline behaviors (implementing `IPipelineBehavior<TRequest, TResponse>`)
- References to Domain layer interfaces and entities

**What is strictly forbidden here:**
- ❌ Direct database access (no `DbContext`, no SQL)
- ❌ HTTP concepts (no controllers, no `HttpContext`)
- ❌ Concrete infrastructure implementations


### Infrastructure Layer

**Responsibility:** Provides concrete implementations of the interfaces defined in the Domain and Application layers. This is where frameworks and external tools live — EF Core, database configurations, and the transaction manager.

**What is allowed here:**
- EF Core `DbContext` and entity configurations
- Repository implementations
- Transaction manager implementation
- Database migration logic
- References to Domain and Application layers

**What is strictly forbidden here:**
- ❌ Business logic (belongs in Domain)
- ❌ Use case orchestration (belongs in Application)
- ❌ HTTP concepts (belongs in API)


### Presentation Layer (API)

**Responsibility:** Translates HTTP requests into application commands/queries and application responses into HTTP responses. Controllers are intentionally thin — they contain zero business logic.

**What is allowed here:**
- ASP.NET Core controllers with route definitions
- Request/response DTOs specific to the API contract
- Middleware for cross-cutting HTTP concerns
- Swagger/OpenAPI configuration
- References to Application layer (commands, queries, DTOs)

**What is strictly forbidden here:**
- ❌ Business logic of any kind
- ❌ Direct database access
- ❌ Domain entity manipulation (use commands/queries instead)


## 🔒 Dependency Rule Visualization

The Dependency Rule is the single most important constraint in Clean Architecture. Dependencies flow strictly inward. No inner layer may reference an outer layer.

```
                        ┌─────────────────────┐
                        │       Domain        │
                        │  (no dependencies)  │
                        └──────────▲──────────┘
                                   │
                          ALLOWED  │  ✓
                                   │
                        ┌──────────┴──────────┐
                        │     Application     │
                        │  (depends on Domain)│
                        └──────────▲──────────┘
                                   │
                          ALLOWED  │  ✓
                                   │
              ┌────────────────────┴────────────────────┐
              │                                         │
   ┌──────────┴──────────┐               ┌─────────────┴─────────────┐
   │   Infrastructure    │               │     API / Presentation    │
   │ (depends on Domain  │               │   (depends on Application)│
   │  and Application)   │               │                           │
   └─────────────────────┘               └───────────────────────────┘


   ✗ FORBIDDEN directions:
     Domain     ──✗──▶  Application, Infrastructure, API
     Application ──✗──▶  Infrastructure, API
     API         ──✗──▶  Infrastructure (directly)
```

> **The key insight:** `IProjectRepository` is defined in the Domain layer, but implemented in Infrastructure. The Application layer depends on the interface, not the implementation. At runtime, dependency injection wires them together. This is the **Dependency Inversion Principle** — the "D" in SOLID — and it is what makes Clean Architecture work.

---

## 🎯 Use Cases & API Endpoints

### 1. Create Project (Command)

**`POST /api/projects/v1.0`**

Creates a new project with a name, description, and owner.

**Request Body:**
```json
{
  "name": "Sprint Backlog",
  "description": "Tasks for the current sprint",
  "owner": "Alice"
}
```

**Response — 201 Created:**
```json
{
  "projectId": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
}
```

**Business Rules:**

| Rule | Enforced By | Layer |
|---|---|---|
| Name is required | `CreateProjectCommandValidator` | Application |
| Name ≤ 100 characters | `CreateProjectCommandValidator` | Application |
| Owner is required | `CreateProjectCommandValidator` | Application |

---

### 2. Add Task to Project (Command)

**`POST /api/projects/{projectId}/tasks`**

Adds a task to an existing project.

**Request Body:**
```json
{
  "title": "Implement user authentication",
  "priority": "High"
}
```

**Response — 201 Created:**
```json
{
  "taskId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890"
}
```

**Business Rules:**

| Rule | Enforced By | Layer |
|---|---|---|
| ProjectId is required | `AddTaskToProjectCommandValidator` | Application |
| Title is required | `AddTaskToProjectCommandValidator` | Application |
| Title ≤ 200 characters | `AddTaskToProjectCommandValidator` | Application |
| Priority must be valid enum (Low, Medium, High) | `AddTaskToProjectCommandValidator` | Application |
| Project must exist | `AddTaskToProjectCommandHandler` | Application |
| Max 20 tasks per project | `Project.AddTask()` | Domain |

---

### 3. Complete Task (Command)

**`PUT /api/tasks/{taskId}/complete`**

Marks a task as completed.

**Request Body:** _none_

**Response — 204 No Content**

**Business Rules:**

| Rule | Enforced By | Layer |
|---|---|---|
| Task must exist | `CompleteTaskCommandHandler` | Application |
| Task cannot be completed twice | `TaskItem.MarkAsCompleted()` | Domain |

---

### 4. Get Project Details (Query)

**`GET /api/projects/v1.0/{projectId}`**

Retrieves full project details including all tasks and computed statistics.

**Request Body:** _none_

**Response — 200 OK:**
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "name": "Sprint Backlog",
  "description": "Tasks for the current sprint",
  "owner": "Alice",
  "createdAt": "2026-03-07T04:06:54Z",
  "totalTasks": 2,
  "completedTasks": 1,
  "tasks": [
    {
      "id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
      "title": "Implement user authentication",
      "status": "Completed",
      "priority": "High",
      "createdAt": "2026-03-07T04:10:00Z"
    },
    {
      "id": "b2c3d4e5-f6a7-8901-bcde-f12345678901",
      "title": "Write unit tests",
      "status": "Pending",
      "priority": "Medium",
      "createdAt": "2026-03-07T04:15:00Z"
    }
  ]
}
```

**Business Rules:**

| Rule | Enforced By | Layer |
|---|---|---|
| Project must exist | `GetProjectDetailsQueryHandler` | Application |

---

## 🚀 Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Any IDE: Visual Studio 2022+, JetBrains Rider, or VS Code with C# Dev Kit
- SQL Server LocalDB (included with Visual Studio) or any SQL Server instance

### Steps

**1. Clone the repository**

```bash
git clone https://github.com/OsamaMohamedM/Dotnet-Clean-Architecture-CQRS.git
```

**2. Navigate to the project folder**

```bash
cd Dotnet-Clean-Architecture-CQRS/dotnet-clean-architecture-cqrs
```

**3. Restore NuGet packages**

```bash
dotnet restore
```

**4. Verify the database connection**

The project uses SQL Server LocalDB by default. Check `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=TaskBoardDb;Trusted_Connection=true;MultipleActiveResultSets=true"
  }
}
```

> The database is automatically created on startup via `dbContext.Database.EnsureCreatedAsync()` — no manual migrations are needed.

**5. Run the project**

```bash
dotnet run
```

**6. Open Swagger UI**

Navigate to:

```
https://localhost:{port}/swagger
```

The exact port will be displayed in the console output when the application starts. Swagger provides an interactive interface to test all API endpoints.

---

## 📬 Example HTTP Requests

### 1. Create Project

**curl:**

```bash
curl -X POST https://localhost:5001/api/projects/v1.0 \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Sprint Backlog",
    "description": "Tasks for the current sprint",
    "owner": "Alice"
  }'
```

**.http file:**

```http
POST https://localhost:5001/api/projects/v1.0
Content-Type: application/json

{
  "name": "Sprint Backlog",
  "description": "Tasks for the current sprint",
  "owner": "Alice"
}
```

---

### 2. Add Task to Project

**curl:**

```bash
curl -X POST https://localhost:5001/api/projects/3fa85f64-5717-4562-b3fc-2c963f66afa6/tasks \
  -H "Content-Type: application/json" \
  -d '{
    "title": "Implement user authentication",
    "priority": "High"
  }'
```

**.http file:**

```http
POST https://localhost:5001/api/projects/3fa85f64-5717-4562-b3fc-2c963f66afa6/tasks
Content-Type: application/json

{
  "title": "Implement user authentication",
  "priority": "High"
}
```

---

### 3. Complete Task

**curl:**

```bash
curl -X PUT https://localhost:5001/api/tasks/a1b2c3d4-e5f6-7890-abcd-ef1234567890/complete
```

**.http file:**

```http
PUT https://localhost:5001/api/tasks/a1b2c3d4-e5f6-7890-abcd-ef1234567890/complete
```

---

### 4. Get Project Details

**curl:**

```bash
curl https://localhost:5001/api/projects/v1.0/3fa85f64-5717-4562-b3fc-2c963f66afa6
```

**.http file:**

```http
GET https://localhost:5001/api/projects/v1.0/3fa85f64-5717-4562-b3fc-2c963f66afa6
```

---

## 📄 License

This project is licensed under the MIT License. Use it to learn, fork it, break it, rebuild it.
