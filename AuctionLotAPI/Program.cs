using Microsoft.Azure.Cosmos;

var builder = WebApplication.CreateBuilder(args);

var endpoint = builder.Configuration["CosmosDb:Endpoint"];
var endpointPort = builder.Configuration["CosmosDb:Key"];

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddSingleton(new CosmosClient(
    endpoint,
    endpointPort
));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
