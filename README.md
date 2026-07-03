<div dir="ltr">

# Ecommerce Monolith to Microservices

Backend architecture project built with .NET 8, SQL Server, Entity Framework Core, Swagger, Docker, and Docker Compose.

The project starts as a simple monolith and is planned to evolve gradually into a production-style microservices architecture.

## Current Phase

Phase 1 — Monolith Baseline

The goal of this phase is to build a simple e-commerce backend as a single .NET Web API with one relational database.

## Features Implemented

- Product management
- Inventory tracking
- Order creation
- Inventory validation
- Rejected orders when stock is insufficient
- Entity Framework Core migrations
- Swagger API documentation
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

## Architecture

The current monolith uses a simple layered structure:

```text
Controllers
    ?
Business Logic Layer
    ?
Entity Framework Core DbContext
    ?
SQL Server
```

The monolith is intentionally kept simple in Phase 1.  
The service layer contains the business logic, while Entity Framework Core handles data access and database persistence.

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

To remove the containers without deleting the database volume:

```bash
docker compose down
```

To remove the containers and delete the database volume:

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

## Phase 1 Checkpoint

The monolith can be started using Docker Compose.

After running:

```bash
docker compose up --build
```

It is possible to:

1. Create a product
2. Create an order
3. Verify that inventory decreases
4. Create an invalid order
5. Verify that inventory does not decrease

## Why DTOs Are Used

DTOs are used to separate the API contract from the database entities.

Entities represent the database model.  
DTOs represent the API input and output structure.

This also prevents circular reference issues when returning related entities such as products and inventory items.

## Planned Next Phases

- Split the monolith into microservices
- Add API Gateway
- Add BFF layer
- Add asynchronous messaging
- Add Saga pattern for distributed transactions
- Add Redis caching
- Add monitoring and observability

</div>