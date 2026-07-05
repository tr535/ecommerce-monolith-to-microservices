# ADR 002: Use SQL Server for InventoryService

## Status

Accepted

## Context

InventoryService is responsible for managing product stock.

The service stores how many items are available and how many items are reserved for each product.

Inventory operations must be accurate. The system must not reserve more items than are available.

## Decision

InventoryService uses SQL Server as its database.

The service stores inventory data in the `InventoryServiceDb` database.

## Reason

Inventory data requires strong consistency.

When an order is created, the system must update inventory in a reliable way. If two orders try to reserve the same stock at the same time, the database must protect the data from becoming invalid.

SQL Server is a relational database that supports ACID transactions, making it a good fit for inventory operations.

## Database Concepts

SQL Server follows the ACID model:

- Atomicity: inventory updates should succeed or fail as one unit
- Consistency: inventory quantities should remain valid
- Isolation: concurrent operations should not corrupt stock data
- Durability: confirmed changes should be saved permanently

From a CAP perspective, InventoryService prefers consistency over high availability because incorrect inventory data can cause business problems.

## Consequences

### Advantages

- Strong consistency
- ACID transaction support
- Reliable updates for stock reservation
- Good fit for structured inventory data

### Disadvantages

- Less flexible schema than NoSQL databases
- Scaling writes may require more careful planning
- Strong consistency can reduce availability in some distributed scenarios