# ADR 001: Use MongoDB for ProductCatalogService

## Status

Accepted

## Context

ProductCatalogService is responsible for storing product catalog data.

Products can belong to different categories, and each category may require different attributes. For example, an electronics product may have RAM, storage, and brand, while another product category may require completely different attributes.

Using a relational database for this type of flexible product structure could require many nullable columns or additional tables for dynamic attributes.

## Decision

ProductCatalogService uses MongoDB as its database.

The service stores products in the `ProductCatalogDb` database and the `Products` collection.

## Reason

MongoDB is a document database, which fits product catalog data because each product can be stored as a flexible document.

The `Attributes` field allows different products to store different key-value attributes without changing the database schema.

This makes MongoDB a good choice for a catalog service where product data is flexible and may evolve over time.

## Database Concepts

MongoDB is a NoSQL document database.

Compared to a traditional relational database, MongoDB is closer to the BASE model because it focuses on flexibility, scalability, and availability.

From a CAP perspective, catalog data can tolerate a more flexible consistency model than order or inventory data. Product browsing is usually read-heavy, and slight delays in catalog updates are less critical than incorrect order or inventory data.

## Consequences

### Advantages

- Flexible product attributes
- Good fit for different product categories
- No need for many nullable columns
- Natural document structure
- Suitable for read-heavy catalog data

### Disadvantages

- Less natural relational joins compared to SQL Server
- Some validation must be handled in the application code
- Not the best choice for strongly transactional financial data