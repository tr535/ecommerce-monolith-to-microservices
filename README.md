# Ecommerce Monolith to Microservices

Backend architecture project built with .NET 8 Web API, SQL Server, Entity Framework Core, MongoDB, YARP, Swagger, Docker, and Docker Compose.

The project starts as a simple monolithic e-commerce API and evolves step by step into a production-style microservices architecture.

## Project Goal

The goal of this project is to take a working monolithic API and gradually evolve it into a distributed system that demonstrates:

* Containers
* Microservices
* Database-per-service
* Polyglot persistence
* API Gateway
* BFF
* Load balancing
* Async messaging
* Saga pattern
* Distributed caching
* Monitoring and observability

## Current Phase

Phase 3 — API Gateway, BFF and Load Balancing

Phase 1 created a working monolithic e-commerce API.

Phase 2 split the monolith into independent microservices with database-per-service and polyglot persistence.

Phase 3 adds an API Gateway, a BFF service, and load balancing for service replicas.

## Branches

```text
master
```

Contains the stable Phase 1 monolith.

```text
phase-2-microservices
```

Contains Phase 1, Phase 2 microservices, and Phase 3 Gateway/BFF/Load Balancing implementation.

---

# Phase 1 — Monolith Baseline

In Phase 1, the system was implemented as a single .NET 8 Web API backed by one SQL Server database.

## Phase 1 Features Implemented

* Product management
* Inventory tracking
* Order creation
* Inventory validation
* Confirmed orders when stock is available
* Rejected orders when stock is insufficient
* Inventory decrease after successful orders
* Inventory remains unchanged after rejected orders
* Entity Framework Core migrations
* Swagger API documentation
* Dockerfile for the API
* Docker Compose setup with API and SQL Server

## Phase 1 Architecture

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

## Phase 1 Main Endpoints

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

---

# Phase 2 — Microservices Split

In Phase 2, the monolith was split into four independent services:

* ProductCatalogService
* InventoryService
* OrderService
* NotificationService

Each service owns its own data and exposes its own API.

The services communicate with each other using synchronous HTTP calls for now.
Async messaging and saga orchestration will be added in Phase 4.

## Phase 2 Architecture

```text
Client / Swagger
      |
      v
OrderService
      |
      +--> ProductCatalogService
      |
      +--> InventoryService
      |
      +--> NotificationService
```

Each service has its own database:

```text
ProductCatalogService  --> MongoDB
InventoryService       --> SQL Server
OrderService           --> SQL Server
NotificationService    --> SQL Server
```

## Phase 2 Services

## ProductCatalogService

ProductCatalogService is responsible for product catalog data.

It uses MongoDB because products can have flexible attributes that differ between categories.

For example, electronics, clothing, and books may all require different product fields.

### Database

```text
Database: ProductCatalogDb
Collection: Products
Technology: MongoDB
```

### Product Model

```text
Id: string ObjectId
Name: string
Description: string
Price: decimal
Category: string
Attributes: Dictionary<string, string>
```

### Endpoints

```http
GET /api/Products
GET /api/Products/{id}
POST /api/Products
```

### Example Product Request

```json
{
  "name": "Laptop",
  "description": "Business laptop",
  "price": 3500,
  "category": "Electronics",
  "attributes": {
    "brand": "Dell",
    "ram": "16GB",
    "storage": "512GB SSD"
  }
}
```

---

## InventoryService

InventoryService is responsible for product stock.

It uses SQL Server because inventory operations require strong consistency.
The system must prevent invalid stock updates and avoid reserving more items than are available.

### Database

```text
Database: InventoryServiceDb
Technology: SQL Server
```

### Entity

```text
InventoryItem
- Id
- ProductId
- QuantityAvailable
- QuantityReserved
```

### Endpoints

```http
GET /api/Inventory
GET /api/Inventory/{productId}
POST /api/Inventory
POST /api/Inventory/reserve
```

### Business Logic

When inventory is reserved:

* `QuantityAvailable` decreases
* `QuantityReserved` increases

If the requested quantity is invalid or not enough stock is available, the service returns `success: false`.

---

## OrderService

OrderService is responsible for creating and storing orders.

It uses SQL Server because orders contain financial and transactional data.
Orders also have a clear relational structure between `Order` and `OrderItem`.

### Database

```text
Database: OrderServiceDb
Technology: SQL Server
```

### Entities

```text
Order
- Id
- CustomerEmail
- CreatedAt
- Status
- TotalAmount
- Items

OrderItem
- Id
- OrderId
- ProductId
- ProductName
- Quantity
- UnitPrice
```

### Order Statuses

```text
Pending
Confirmed
Rejected
Cancelled
```

### Endpoints

```http
GET /api/Orders
GET /api/Orders/{id}
POST /api/Orders
```

### Service Clients

OrderService communicates with:

```text
ProductCatalogClient   --> ProductCatalogService
InventoryClient        --> InventoryService
NotificationClient     --> NotificationService
```

### Order Flow

When a new order is created:

1. OrderService receives the order request.
2. It calls ProductCatalogService to get product name and price.
3. It calls InventoryService to reserve stock.
4. If the product exists and inventory is available:

   * The order is saved with status `Confirmed`
   * The total amount is calculated
   * Inventory is reserved
   * A notification is sent to NotificationService
5. If the product does not exist, quantity is invalid, or inventory is not available:

   * The order is saved with status `Rejected`
   * Total amount is set to `0`
   * A rejected notification is sent to NotificationService

### Example Order Request

```json
{
  "customerEmail": "customer@test.com",
  "items": [
    {
      "productId": "PRODUCT_ID_FROM_MONGODB",
      "quantity": 2
    }
  ]
}
```

### Example Confirmed Order Response

```json
{
  "id": 1,
  "customerEmail": "customer@test.com",
  "status": "Confirmed",
  "totalAmount": 7000,
  "items": [
    {
      "productId": "PRODUCT_ID_FROM_MONGODB",
      "productName": "Laptop",
      "quantity": 2,
      "unitPrice": 3500
    }
  ]
}
```

### Example Rejected Order Response

```json
{
  "id": 2,
  "customerEmail": "customer@test.com",
  "status": "Rejected",
  "totalAmount": 0,
  "items": []
}
```

---

## NotificationService

NotificationService is responsible for storing customer notifications.

In this demo project, no real email is sent.
Instead, the notification is saved in the database to prove that the order flow reached the notification step.

### Database

```text
Database: NotificationServiceDb
Technology: SQL Server
```

### Entity

```text
NotificationMessage
- Id
- OrderId
- CustomerEmail
- Status
- Message
- CreatedAt
```

### Endpoints

```http
GET /api/Notifications
GET /api/Notifications/{id}
POST /api/Notifications
```

### Example Notification Response

```json
[
  {
    "id": 1,
    "orderId": 1002,
    "customerEmail": "customer@test.com",
    "status": "Confirmed",
    "message": "Order confirmed successfully.",
    "createdAt": "2026-07-05T15:19:26.481135"
  }
]
```

---

# Phase 3 — API Gateway, BFF and Load Balancing

Phase 3 adds an entry layer above the microservices.

The goal of this phase is to make the client communicate through a single entry point, add a BFF endpoint that aggregates data from multiple services, and demonstrate load balancing between multiple instances of the same service.

## Phase 3 Architecture

```text
Client
  |
  v
API Gateway :8000
  |
  +--> ProductCatalogService instance 1
  +--> ProductCatalogService instance 2
  +--> InventoryService
  +--> OrderService
  +--> NotificationService
  +--> BffService
```

## API Gateway

An API Gateway was added using YARP.

The gateway runs on port `8000` and routes requests to the internal microservices.

Before the gateway, the client had to know the direct service ports:

```text
ProductCatalogService  -> http://localhost:8081
InventoryService       -> http://localhost:8082
OrderService           -> http://localhost:8083
NotificationService    -> http://localhost:8084
```

After adding the gateway, the client can use one entry point:

```text
http://localhost:8000
```

Example routes:

```text
GET  /api/products       -> ProductCatalogService
GET  /api/inventory      -> InventoryService
GET  /api/orders         -> OrderService
GET  /api/notifications  -> NotificationService
GET  /api/bff/...        -> BffService
```

The routing rules are configured in:

```text
src/ApiGateway/appsettings.json
```

The gateway is also registered in the root `docker-compose.yml`:

```text
api-gateway -> http://localhost:8000
```

## BFF Service

A BFF service was added for a web-client order details view.

The BFF exposes a single endpoint:

```http
GET /api/bff/orders/{orderId}/details
```

This endpoint aggregates data from multiple services:

```text
BffService
  |
  +--> OrderService
  +--> ProductCatalogService
  +--> NotificationService
```

Instead of forcing the client to call several services separately, the BFF returns one combined response containing:

```text
order
products
notifications
```

This keeps the client simpler and hides internal microservice communication from the frontend.

Example request through the API Gateway:

```text
http://localhost:8000/api/bff/orders/2002/details
```

## Load Balancing

Load balancing was added for ProductCatalogService.

Two running instances of ProductCatalogService are defined in `docker-compose.yml`:

```text
product-catalog-service
product-catalog-service-replica
```

Both instances run the same ProductCatalogService code and connect to the same MongoDB database.

The API Gateway uses YARP with a `RoundRobin` load balancing policy:

```json
"LoadBalancingPolicy": "RoundRobin"
```

RoundRobin means that requests are distributed between the available service instances in turn.

Each ProductCatalogService response includes a custom header:

```text
X-Service-Instance
```

This header contains the container instance that handled the request.

The header is added in `src/ProductCatalogService/Program.cs`:

```csharp
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Service-Instance"] = Environment.MachineName;
    await next();
});
```

To prove load balancing, several requests were sent through the gateway:

```powershell
1..10 | ForEach-Object {
    $response = Invoke-WebRequest -Uri "http://localhost:8000/api/products" -Method Get -UseBasicParsing
    "$_ -> " + $response.Headers["X-Service-Instance"]
}
```

The output showed two different instance values, proving that requests were distributed between the two ProductCatalogService containers.

## Phase 3 Demo Evidence

Screenshots are stored under:

```text
docs/screenshots/phase-3
```

Evidence includes:

```text
01-api-gateway-products.png
02-bff-order-details.png
03-load-balancing-roundrobin.png
```

## Phase 3 Checkpoint

Phase 3 is complete because:

* The client can access services through `http://localhost:8000`
* The API Gateway routes requests to internal microservices
* The BFF returns combined order details from multiple services
* ProductCatalogService runs with two instances
* The gateway distributes requests between the two ProductCatalogService instances using RoundRobin load balancing

---

# Docker Compose Services

The root `docker-compose.yml` runs the monolith, all microservices, databases, the API Gateway, and the BFF service.

## Application Services

```text
ecommerce-api                         -> http://localhost:8080
product-catalog-service               -> http://localhost:8081
product-catalog-service-replica        -> http://localhost:8086
inventory-service                     -> http://localhost:8082
order-service                         -> http://localhost:8083
notification-service                  -> http://localhost:8084
bff-service                           -> http://localhost:8085
api-gateway                           -> http://localhost:8000
```

## Databases

```text
sqlserver                    -> localhost:1433
mongodb                      -> localhost:27017
inventory-sqlserver          -> localhost:1435
order-sqlserver              -> localhost:1436
notification-sqlserver       -> localhost:1437
```

## Internal Docker URLs

Inside Docker, services communicate using container names:

```text
ProductCatalogService          -> http://product-catalog-service:8080
ProductCatalogService Replica  -> http://product-catalog-service-replica:8080
InventoryService               -> http://inventory-service:8080
OrderService                   -> http://order-service:8080
NotificationService            -> http://notification-service:8080
BffService                     -> http://bff-service:8080
```

---

# Project Structure

```text
EcommerceMonolith
|
|-- BLL
|-- Controllers
|-- DAL
|-- DTOs
|-- Migrations
|-- Models
|
|-- docs
|   |
|   |-- adr
|   |   |-- 001-product-catalog-mongodb.md
|   |   |-- 002-inventory-sql-server.md
|   |   |-- 003-order-sql-server.md
|   |   |-- 004-notification-sql-server.md
|   |
|   |-- screenshots
|       |-- phase-3
|           |-- 01-api-gateway-products.png
|           |-- 02-bff-order-details.png
|           |-- 03-load-balancing-roundrobin.png
|
|-- src
|   |
|   |-- ApiGateway
|   |   |-- Dockerfile
|   |   |-- Program.cs
|   |   |-- appsettings.json
|   |
|   |-- BffService
|   |   |-- Dockerfile
|   |   |-- Program.cs
|   |
|   |-- ProductCatalogService
|   |   |-- Controllers
|   |   |-- Models
|   |   |-- Services
|   |   |-- Dockerfile
|   |   |-- Program.cs
|   |
|   |-- InventoryService
|   |   |-- Controllers
|   |   |-- Data
|   |   |-- DTOs
|   |   |-- Models
|   |   |-- Migrations
|   |   |-- Dockerfile
|   |   |-- Program.cs
|   |
|   |-- OrderService
|   |   |-- Clients
|   |   |-- Controllers
|   |   |-- Data
|   |   |-- DTOs
|   |   |-- Models
|   |   |-- Migrations
|   |   |-- Dockerfile
|   |   |-- Program.cs
|   |
|   |-- NotificationService
|       |-- Controllers
|       |-- DAL
|       |-- DTOs
|       |-- Models
|       |-- Migrations
|       |-- Services
|       |-- Dockerfile
|       |-- Program.cs
|
|-- Dockerfile
|-- docker-compose.yml
|-- .dockerignore
|-- appsettings.json
|-- Program.cs
|-- README.md
```

---

# Important Project Configuration

The monolith project excludes the `src` folder from compilation.

This is needed because the new microservices are separate .NET projects inside the same repository, and the monolith should not try to compile their files.

```xml
<ItemGroup>
  <Compile Remove="src\**\*.cs" />
  <Content Remove="src\**\*" />
  <None Remove="src\**\*" />
  <EmbeddedResource Remove="src\**\*" />
</ItemGroup>
```

---

# Tech Stack

## Backend

* .NET 8 Web API
* C#
* Entity Framework Core
* MongoDB.Driver
* YARP Reverse Proxy
* Swagger / OpenAPI

## Databases

* SQL Server
* MongoDB

## DevOps

* Docker
* Docker Compose

---

# Run with Docker

Make sure Docker Desktop is running.

From the root project folder, run:

```bash
docker compose up --build
```

Open the API Gateway:

```text
http://localhost:8000
```

Open Swagger for each directly exposed service:

```text
Monolith Swagger:              http://localhost:8080/swagger
ProductCatalogService Swagger: http://localhost:8081/swagger
InventoryService Swagger:      http://localhost:8082/swagger
OrderService Swagger:          http://localhost:8083/swagger
NotificationService Swagger:   http://localhost:8084/swagger
BffService Swagger:            http://localhost:8085/swagger
```

The client should communicate through the API Gateway on port `8000`.

Direct service ports are kept exposed only for development and debugging.

Stop the containers without deleting database volumes:

```bash
docker compose down
```

Stop the containers and delete database volumes:

```bash
docker compose down -v
```

---

# Useful Commands

Build all projects:

```bash
dotnet build
```

Run all services:

```bash
docker compose up --build
```

Run all services and recreate containers:

```bash
docker compose up --build --force-recreate
```

Add NotificationService migration:

```bash
dotnet ef migrations add InitialCreate --project src/NotificationService/NotificationService.csproj --startup-project src/NotificationService/NotificationService.csproj --output-dir Migrations
```

---

# How to Test Phase 2 End-to-End

## 1. Create a Product

Use ProductCatalogService:

```text
POST http://localhost:8081/api/Products
```

Example body:

```json
{
  "name": "Laptop",
  "description": "Business laptop",
  "price": 3500,
  "category": "Electronics",
  "attributes": {
    "brand": "Dell",
    "ram": "16GB"
  }
}
```

Copy the returned product `id`.

## 2. Create Inventory for the Product

Use InventoryService:

```text
POST http://localhost:8082/api/Inventory
```

Example body:

```json
{
  "productId": "PRODUCT_ID_FROM_STEP_1",
  "quantityAvailable": 10,
  "quantityReserved": 0
}
```

## 3. Create an Order

Use OrderService:

```text
POST http://localhost:8083/api/Orders
```

Example body:

```json
{
  "customerEmail": "customer@test.com",
  "items": [
    {
      "productId": "PRODUCT_ID_FROM_STEP_1",
      "quantity": 2
    }
  ]
}
```

Expected result:

```text
Status: Confirmed
TotalAmount: price * quantity
```

## 4. Verify Inventory Was Reserved

Use InventoryService:

```text
GET http://localhost:8082/api/Inventory/PRODUCT_ID_FROM_STEP_1
```

Expected result:

```text
QuantityAvailable decreased
QuantityReserved increased
```

## 5. Verify Notification Was Created

Use NotificationService:

```text
GET http://localhost:8084/api/Notifications
```

Expected result:

```text
Status: Confirmed
Message: Order confirmed successfully.
```

## 6. Test Rejected Order

Create an order with a quantity larger than the available stock.

Expected result:

```text
Order status: Rejected
TotalAmount: 0
Notification status: Rejected
```

---

# How to Test Phase 3

## 1. Test API Gateway

```text
GET http://localhost:8000/api/products
```

Expected result:

```text
Products are returned through the API Gateway.
```

## 2. Test Order Creation Through the Gateway

```text
POST http://localhost:8000/api/orders
```

Example body:

```json
{
  "customerEmail": "customer@test.com",
  "items": [
    {
      "productId": "PRODUCT_ID_FROM_MONGODB",
      "quantity": 1
    }
  ]
}
```

Expected result:

```text
Order is created through the API Gateway.
```

## 3. Test BFF

```text
GET http://localhost:8000/api/bff/orders/ORDER_ID/details
```

Expected result:

```text
A single response containing order, products, and notifications.
```

## 4. Test Load Balancing

```powershell
1..10 | ForEach-Object {
    $response = Invoke-WebRequest -Uri "http://localhost:8000/api/products" -Method Get -UseBasicParsing
    "$_ -> " + $response.Headers["X-Service-Instance"]
}
```

Expected result:

```text
Two different X-Service-Instance values appear in the output.
```

This proves that the API Gateway distributes requests between two ProductCatalogService instances.

---

# Business Logic

## Monolith Business Logic

In Phase 1, the monolith handled all business logic inside one application.

When an order was created, the system checked whether all requested products existed and whether enough inventory was available.

If the order was valid:

* The order status became `Confirmed`
* The total amount was calculated
* Inventory was decreased

If the order was invalid:

* The order status became `Rejected`
* The total amount was `0`
* Inventory was not decreased

Rejected orders were still saved in the database for tracking and audit purposes.

## Microservices Business Logic

In Phase 2, the same business flow is distributed between independent services.

OrderService does not access another service's database directly.

Instead, it communicates through HTTP APIs:

```text
OrderService
    |
    +--> ProductCatalogService
    +--> InventoryService
    +--> NotificationService
```

This keeps data ownership clear and follows the database-per-service pattern.

In Phase 3, the client can access the system through the API Gateway instead of calling services directly.

---

# Architecture Decisions Summary

Detailed ADR files are stored under:

```text
docs/adr
```

## ProductCatalogService — MongoDB

Product catalog data is flexible and can vary by category.

A document database fits this service because each product can store different attributes without requiring many relational tables or nullable columns.

This is a good fit for a BASE-style, flexible document model where read flexibility is important.

## InventoryService — SQL Server

Inventory requires strong consistency.

The system must avoid reserving more stock than is available.

SQL Server is used because relational databases provide ACID transactions and a strong consistency model.

## OrderService — SQL Server

Orders include financial data, status changes, and order items.

The relationship between orders and order items is naturally relational.

SQL Server is used because order data benefits from ACID guarantees, structured relationships, and reliable persistence.

## NotificationService — SQL Server

NotificationService currently uses SQL Server to keep notification records in a simple structured table.

In a future phase, this service could be moved to a document database if notifications become more flexible, for example if the system supports different notification channels such as email, SMS, push notifications, or webhooks.

---

# Expected Problems in the Phase 1 Monolith

The monolithic architecture was intentionally simple, but it has several expected problems at scale.

## 1. Tight Coupling

Orders, products, and inventory are all part of the same deployable application.

A small change in one area requires rebuilding and redeploying the entire monolith.

## 2. Limited Independent Scaling

All features scale together, even if only one part of the system is under heavy load.

For example, if product browsing receives high traffic, the entire monolith must be scaled, including order and inventory logic.

## 3. Single Database Bottleneck

All modules use the same relational database.

As the system grows, this database can become a performance bottleneck and a single point of failure.

---

# Improvements Introduced

## Phase 2 Improvements

Phase 2 improves the architecture by introducing:

* Separate services by business capability
* Separate databases per service
* Clear data ownership
* Polyglot persistence
* Independent deployment boundaries
* HTTP communication between services
* A dedicated notification service

## Phase 3 Improvements

Phase 3 improves the architecture by introducing:

* A single API Gateway entry point
* Centralized routing through YARP
* A BFF service for frontend-specific aggregation
* Load balancing between two ProductCatalogService instances
* Better separation between client-facing APIs and internal services

The system is now closer to a production-style distributed architecture.

---

# Current Status

Phase 1 is complete.

Phase 2 core implementation is complete.

Phase 3 implementation is complete.

The following Phase 2 flow works end-to-end:

```text
Create product in ProductCatalogService
Create inventory in InventoryService
Place order in OrderService
OrderService reads product data
OrderService reserves inventory
OrderService saves the order
OrderService creates notification in NotificationService
NotificationService stores the notification
```

Confirmed order flow was tested successfully:

```text
Order status: Confirmed
Notification status: Confirmed
Inventory available quantity decreased
Inventory reserved quantity increased
```

The following Phase 3 capabilities were tested successfully:

```text
API Gateway routes requests through port 8000
BFF returns combined order details
Load balancing distributes product requests between two ProductCatalogService instances
```

---

# Planned Next Phases

## Phase 4 — Async Messaging, Saga and Caching

The order flow will move from synchronous HTTP calls to asynchronous messaging.

The system will include:

* Message broker
* Order Saga
* Compensation flow
* Redis cache using the cache-aside pattern

## Phase 5 — Monitoring and Observability

The system will include:

* Structured logging
* Health checks
* Correlation ID across services and messages

---

# Git Commands for Phase 3 Commit

```bash
git status
git add .
git commit -m "Add phase 3 gateway BFF and load balancing"
git push
```
