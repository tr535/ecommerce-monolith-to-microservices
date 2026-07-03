# Ecommerce Monolith to Microservices

Backend architecture project built with .NET 8, SQL Server, Entity Framework Core, Swagger, Docker, and Docker Compose.

The project starts as a simple monolithic e-commerce API and is planned to evolve step by step into a production-style microservices architecture.

## Project Goal

The goal of this project is to take a working monolithic API and gradually evolve it into a distributed system that demonstrates:

- Containers
- Microservices
- Database-per-service
- Polyglot persistence
- API Gateway
- BFF
- Load balancing
- Async messaging
- Saga pattern
- Distributed caching
- Monitoring and observability

## Current Phase

Phase 1 — Monolith Baseline

The goal of this phase is to build a simple e-commerce backend as a single .NET 8 Web API with one relational database.

## Phase 1 Features Implemented

- Product management
- Inventory tracking
- Order creation
- Inventory validation
- Confirmed orders when stock is available
- Rejected orders when stock is insufficient
- Inventory decrease after successful orders
- Inventory remains unchanged after rejected orders
- Entity Framework Core migrations
- Swagger API documentation
- Dockerfile for the API
- Docker Compose setup with API and SQL Server

## Tech Stack

- .NET 8 Web API
- C#
- Entity Framework Core
- SQL Server
- Docker
- Docker Compose
- Swagger / OpenAPI

## Project Structure

```text
EcommerceMonolith
??? BLL
?   ??? IProductService.cs
?   ??? ProductService.cs
?   ??? IOrderService.cs
?   ??? OrderService.cs
?
??? Controllers
?   ??? ProductsController.cs
?   ??? OrdersController.cs
?
??? DAL
?   ??? AppDbContext.cs
?
??? DTOs
?   ??? ProductCreateDto.cs
?   ??? ProductResponseDto.cs
?   ??? OrderCreateDto.cs
?   ??? OrderItemCreateDto.cs
?   ??? OrderItemResponseDto.cs
?   ??? OrderResponseDto.cs
?
??? Migrations
?
??? Models
?   ??? Product.cs
?   ??? InventoryItem.cs
?   ??? Order.cs
?   ??? OrderItem.cs
?   ??? OrderStatus.cs
?
??? Dockerfile
??? docker-compose.yml
??? .dockerignore
??? appsettings.json
??? Program.cs
??? README.md
```

## Monolith Architecture

The current system is a single deployable .NET Web API.

```text
Client / Swagger
      |
      v
.NET 8 Web API Monolith
      |
      +--> ProductsController
      |
      +--> OrdersController
      |
      v
Business Logic Layer
      |
      +--> ProductService
      |
      +--> OrderService
      |
      v
Entity Framework Core DbContext
      |
      v
SQL Server Database
      |
      +--> Products
      +--> InventoryItems
      +--> Orders
      +--> OrderItems
```

## Architecture Notes

The monolith is intentionally kept simple in Phase 1.

Current flow:

```text
Controller
    |
    v
Service
    |
    v
AppDbContext
    |
    v
SQL Server
```

The service layer contains the business logic.

Entity Framework Core handles data access and database persistence.

A Repository layer was intentionally not added in this phase in order to keep the monolith simple and focused.

## Main API Endpoints

### Products

```http
GET /api/Products
GET /api/Products/{id}
POST /api/Products
```

### Orders

```http
GET /api/Orders
GET /api/Orders/{id}
POST /api/Orders
```

## Run with Docker

Make sure Docker Desktop is running.

From the project folder, run:

```bash
docker compose up --build
```

Open Swagger:

```text
http://localhost:8080/swagger
```

The application runs together with a SQL Server container.

## Database

The application uses SQL Server with Entity Framework Core migrations.

When the application starts, migrations are applied automatically.

The Docker setup uses a SQL Server volume, so data is persisted between container restarts unless the volume is removed.

Stop the containers without deleting the database volume:

```bash
docker compose down
```

Stop the containers and delete the database volume:

```bash
docker compose down -v
```

## Example Product Request

```json
{
  "name": "Laptop",
  "description": "Business laptop",
  "price": 3500,
  "category": "Electronics",
  "quantityAvailable": 10,
  "quantityReserved": 0
}
```

## Example Product Response

```json
{
  "id": 1,
  "name": "Laptop",
  "description": "Business laptop",
  "price": 3500,
  "category": "Electronics",
  "quantityAvailable": 10,
  "quantityReserved": 0
}
```

## Example Valid Order Request

```json
{
  "customerEmail": "customer@test.com",
  "items": [
    {
      "productId": 1,
      "quantity": 2
    }
  ]
}
```

## Example Valid Order Response

```json
{
  "id": 1,
  "customerEmail": "customer@test.com",
  "status": "Confirmed",
  "totalAmount": 7000,
  "items": [
    {
      "productId": 1,
      "productName": "Laptop",
      "quantity": 2,
      "unitPrice": 3500
    }
  ]
}
```

## Example Invalid Order Request

```json
{
  "customerEmail": "customer@test.com",
  "items": [
    {
      "productId": 1,
      "quantity": 999
    }
  ]
}
```

## Example Invalid Order Response

```json
{
  "id": 2,
  "customerEmail": "customer@test.com",
  "status": "Rejected",
  "totalAmount": 0,
  "items": []
}
```

## Business Logic

When an order is created, the system checks whether all requested products exist and whether enough inventory is available.

If the order is valid:

- The order status becomes `Confirmed`
- The total amount is calculated
- Inventory is decreased

If the order is invalid:

- The order status becomes `Rejected`
- The total amount is `0`
- Inventory is not decreased

Rejected orders are still saved in the database for tracking and audit purposes.

## Why DTOs Are Used

DTOs are used to separate the API contract from the database entities.

Entities represent the database model.

DTOs represent the API input and output structure.

This also prevents circular reference issues when returning related entities such as products and inventory items.

## Expected Problems at Scale

This monolithic architecture is intentionally simple for Phase 1, but it can become problematic as the system grows.

### 1. Tight Coupling

Orders, products, and inventory are all part of the same deployable application.

A small change in one area requires rebuilding and redeploying the entire monolith.

### 2. Limited Independent Scaling

All features scale together, even if only one part of the system is under heavy load.

For example, if product browsing receives high traffic, the entire monolith must be scaled, including order and inventory logic.

### 3. Single Database Bottleneck

All modules use the same relational database.

As the system grows, this database can become a performance bottleneck and a single point of failure.

## Phase 1 Checkpoint

The monolith can be started using Docker Compose.

After running:

```bash
docker compose up --build
```

It is possible to:

1. Create a product
2. Place an order
3. Verify that inventory decreases
4. Place an invalid order
5. Verify that inventory does not decrease

## Planned Next Phases

### Phase 2 — Split into Microservices

The monolith will be split into separate services:

- OrderService
- ProductCatalogService
- InventoryService
- NotificationService

Each service will own its own data store.

### Phase 3 — Gateway, BFF and Load Balancing

The system will add:

- API Gateway
- BFF layer
- Load balancing for service replicas

### Phase 4 — Async Messaging, Saga and Caching

The order flow will move from synchronous calls to asynchronous messaging.

The system will include:

- Message broker
- Order Saga
- Compensation flow
- Redis cache using the cache-aside pattern

### Phase 5 — Monitoring and Observability

The system will include:

- Structured logging
- Health checks
- Correlation ID across services and messages

## Current Status

Phase 1 is complete.

The monolith runs with Docker Compose and supports the basic e-commerce flow:

```text
Create product
Place order
Validate inventory
Confirm or reject order
Update inventory when needed
```