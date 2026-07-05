# ADR 004: Use SQL Server for NotificationService

## Status

Accepted

## Context

NotificationService is responsible for storing customer notifications after an order is confirmed or rejected.

In this project, the service does not send real emails. Instead, it stores notification records in the database to demonstrate that the order flow reached the notification step.

Each notification includes order ID, customer email, status, message, and creation time.

## Decision

NotificationService currently uses SQL Server as its database.

The service stores notification data in the `NotificationServiceDb` database.

## Reason

At the current stage of the project, notification data has a simple and structured format.

SQL Server is already used in the project and provides reliable persistence for notification records.

This keeps the implementation simple while still following the database-per-service pattern, because NotificationService owns its own database and does not access another service's database.

## Database Concepts

SQL Server follows the ACID model.

For the current implementation, strong consistency is useful because notification records should be saved reliably after an order is confirmed or rejected.

However, notification data could also fit a NoSQL document database in the future if the notification structure becomes more flexible.

For example, different notification channels such as email, SMS, push notifications, or webhooks may require different fields and metadata.

## Consequences

### Advantages

- Simple structured storage
- Reliable persistence
- Easy integration with Entity Framework Core
- Keeps the current implementation stable
- Maintains database-per-service separation

### Disadvantages

- Less flexible for future notification types
- Schema changes require migrations
- A document database may be a better fit if notifications become more dynamic

## Future Consideration

NotificationService may be moved to MongoDB in a later phase if the system requires more flexible notification payloads or multiple notification channels.