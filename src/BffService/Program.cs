using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/", () => "BFF Service is running");

app.MapGet("/api/bff/orders/{orderId:int}/details", async (
    int orderId,
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration) =>
{
    var httpClient = httpClientFactory.CreateClient();

    var orderServiceUrl = configuration["ServiceUrls:OrderService"] ?? "http://localhost:8083";
    var productCatalogServiceUrl = configuration["ServiceUrls:ProductCatalogService"] ?? "http://localhost:8081";
    var notificationServiceUrl = configuration["ServiceUrls:NotificationService"] ?? "http://localhost:8084";

    var jsonOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
    };

    var order = await httpClient.GetFromJsonAsync<OrderDto>(
        $"{orderServiceUrl}/api/Orders/{orderId}",
        jsonOptions);

    if (order is null)
    {
        return Results.NotFound($"Order {orderId} was not found.");
    }

    var productDetails = new List<ProductDto>();

    foreach (var item in order.Items)
    {
        var product = await httpClient.GetFromJsonAsync<ProductDto>(
            $"{productCatalogServiceUrl}/api/Products/{item.ProductId}",
            jsonOptions);

        if (product is not null)
        {
            productDetails.Add(product);
        }
    }

    var notifications = await httpClient.GetFromJsonAsync<List<NotificationDto>>(
        $"{notificationServiceUrl}/api/Notifications",
        jsonOptions) ?? new List<NotificationDto>();

    var orderNotifications = notifications
        .Where(n => n.OrderId == orderId)
        .ToList();

    var response = new OrderDetailsResponse
    {
        Order = order,
        Products = productDetails,
        Notifications = orderNotifications
    };

    return Results.Ok(response);
});

app.Run();

public class OrderDto
{
    public int Id { get; set; }
    public string CustomerEmail { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public List<OrderItemDto> Items { get; set; } = new();
}

public class OrderItemDto
{
    public string ProductId { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}

public class ProductDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Category { get; set; } = string.Empty;
    public Dictionary<string, string> Attributes { get; set; } = new();
}

public class NotificationDto
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public string CustomerEmail { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class OrderDetailsResponse
{
    public OrderDto Order { get; set; } = new();
    public List<ProductDto> Products { get; set; } = new();
    public List<NotificationDto> Notifications { get; set; } = new();
}