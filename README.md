# FlowBoard - Microservices Kanban System

FlowBoard is a microservices-based Kanban project management system (similar to Trello/Jira lite). It demonstrates a polyglot microservices architecture built for scale and ease of deployment.

## Tech Stack

| Component | Technology | Database / Cache |
|-----------|------------|------------------|
| **Gateway** | NestJS | N/A |
| **Auth Service** | .NET Core 8 | SQL Server |
| **Project Service** | Java (Spring Boot) | PostgreSQL, Redis |
| **Notification Service** | NestJS | MongoDB |
| **Frontend** | React + Vite + TypeScript | N/A |
| **Infrastructure** | Docker, Nginx, RabbitMQ | N/A |

## Architecture Overview

Client requests arrive at the **Nginx** reverse proxy, which routes them directly to internal services or a central **API Gateway** for consolidated requests.

```text
       +---------------------------------------------------+
       |                     Client                        |
       +-------------------------+-------------------------+
                                 |
                                 v
       +-------------------------+-------------------------+
       |                  Nginx (Reverse Proxy)            |
       +-------------------------+-------------------------+
                                 |
                                 v
       +-------------------------+-------------------------+
       |               API Gateway (NestJS)                |
       +-------+-----------------+-----------------+-------+
               |                 |                 |
               v                 v                 v
+--------------+------+  +-------+-------+  +------+--------------+
|   Auth Service      |  | Project Svc   |  | Notification Svc    |
|   (.NET Core)       |  | (Spring Boot) |  |     (NestJS)        |
+--------------+------+  +-------+-------+  +------+--------------+
               |                 |                 |
               v                 v                 v
        +------+------+   +------+------+   +------+------+
        | SQL Server  |   | PostgreSQL  |   |   MongoDB   |
        +-------------+   |   & Redis   |   +-------------+
                          +------+------+
                                 |
                                 v
                          +------+------+
                          |  RabbitMQ   |-----------> (Consumes messages)
                          +-------------+
```

## Prerequisites

- Docker Desktop
- Node.js 20+
- .NET 8 SDK
- JDK 21
- Git

## Local Setup

1. **Clone the repository:**
   ```bash
   git clone <repo-url>
   cd flowboard
   ```

2. **Setup environment variables:**
   ```bash
   cp .env.example .env
   # Make sure to edit .env and provide strong credentials if needed.
   ```

3. **Start local infrastructure (Databases, Cache, Message Broker, Proxy):**
   ```bash
   docker-compose up -d
   ```

4. **Running services individually:**
   - **Frontend**: `cd frontend && npm install && npm run dev`
   - **Gateway**: `cd gateway && npm install && npm run dev`
   - **Auth Service**: `cd services/auth-service && dotnet run`
   - **Project Service**: `cd services/project-service && ./gradlew bootRun`
   - **Notification Service**: `cd services/notification-service && npm install && npm run dev`

## Port Reference Table

| Service / Infrastructure       | Port |
|--------------------------------|------|
| Nginx Reverse Proxy            | 80 |
| Frontend (React/Vite)          | 5173 |
| Auth Service (.NET)            | 8080 |
| Project Service (Spring Boot)  | 8081 |
| Notification Service (NestJS)  | 3001 |
| PostgreSQL                     | 5432 |
| SQL Server                     | 1433 |
| MongoDB                        | 27017 |
| Redis                          | 6379 |
| RabbitMQ                       | 5672 & 15672 |
