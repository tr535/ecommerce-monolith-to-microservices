using Microsoft.EntityFrameworkCore;
using OrderService.Clients;
using OrderService.DAL;
using OrderService.Messaging;
using OrderService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<OrderDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHttpClient<ProductCatalogClient>(client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["ServiceUrls:ProductCatalogService"]!);
});

builder.Services.AddHttpClient<InventoryClient>(client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["ServiceUrls:InventoryService"]!);
});

builder.Services.AddHttpClient<NotificationClient>(client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["ServiceUrls:NotificationService"]!);
});

builder.Services.AddScoped<IOrderService, OrderProcessingService>();

builder.Services.AddSingleton<IRabbitMqPublisher, RabbitMqPublisher>();

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<OrderDbContext>();

    var retryCount = 0;
    var maxRetries = 10;

    while (retryCount < maxRetries)
    {
        try
        {
            dbContext.Database.Migrate();
            break;
        }
        catch
        {
            retryCount++;
            Thread.Sleep(5000);
        }
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

app.MapControllers();

app.Run();