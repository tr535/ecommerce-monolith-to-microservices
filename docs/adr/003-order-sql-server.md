# ADR 003: Use SQL Server for OrderService

## Status

Accepted

## Context

OrderService is responsible for creating and storing customer orders.

Orders include customer details, order status, total amount, and order items.

Order data is financial and transactional. It must be stored reliably and consistently.

## Decision

OrderService uses SQL Server as its database.

The service stores order data in the `OrderServiceDb` database.

## Reason

Orders have a clear relational structure.

An order can contain multiple order items, and each order item belongs to one order. This relationship fits well in a relational database.

Order data also includes financial values such as total amount and unit price. This type of data requires strong consistency and reliable persistence.

SQL Server provides ACID guarantees, which are important for order processing.

## Database Concepts

SQL Server follows the ACID model.

This is important for OrderService because order data must remain consistent and durable.

From a CAP perspective, OrderService prefers consistency for order state and financial data. It is more important to store correct order information than to accept inconsistent order states.

## Consequences

### Advantages

- Strong consistency for order data
- ACID transaction support
- Natural relational structure between Order and OrderItem
- Reliable storage for financial data
- Clear schema for order status and total amount

### Disadvantages

- Less flexible than document databases
- Schema changes require migrations
- Not ideal for highly flexible or unstructured data