using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

// Configure JSON serialization
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
});

// Add MongoDB client
var mongoHost = Environment.GetEnvironmentVariable("MONGODB_HOST") ?? "localhost";
var mongoPort = Environment.GetEnvironmentVariable("MONGODB_PORT") ?? "27017";
var mongoDatabase = Environment.GetEnvironmentVariable("MONGODB_DATABASE") ?? "ToDoAppDb";

var connectionString = $"mongodb://{mongoHost}:{mongoPort}";
builder.Services.AddSingleton<IMongoClient>(new MongoClient(connectionString));
builder.Services.AddScoped(sp =>
{
    var client = sp.GetRequiredService<IMongoClient>();
    return client.GetDatabase(mongoDatabase);
});

var app = builder.Build();

// Serve static files (frontend)
app.UseDefaultFiles();
app.UseStaticFiles();

// API Endpoints
app.MapGet("/api/todos", async (IMongoDatabase db) =>
{
    var collection = db.GetCollection<TodoItem>("TodoItems");
    var todos = await collection.Find(_ => true).ToListAsync();
    return Results.Ok(todos);
});

app.MapGet("/api/todos/{id}", async (int id, IMongoDatabase db) =>
{
    var collection = db.GetCollection<TodoItem>("TodoItems");
    var todo = await collection.Find(t => t.Id == id).FirstOrDefaultAsync();
    return todo is not null ? Results.Ok(todo) : Results.NotFound();
});

app.MapPost("/api/todos", async (TodoItem todo, IMongoDatabase db) =>
{
    var collection = db.GetCollection<TodoItem>("TodoItems");
    await collection.InsertOneAsync(todo);
    return Results.Created($"/api/todos/{todo.Id}", todo);
});

app.MapPut("/api/todos/{id}", async (int id, TodoItem updatedTodo, IMongoDatabase db) =>
{
    var collection = db.GetCollection<TodoItem>("TodoItems");
    var existing = await collection.Find(t => t.Id == id).FirstOrDefaultAsync();
    if (existing == null) return Results.NotFound();

    updatedTodo.Id = id;
    updatedTodo._id = existing._id; // Preserve MongoDB's ObjectId
    var result = await collection.ReplaceOneAsync(t => t.Id == id, updatedTodo);
    return result.ModifiedCount > 0 ? Results.Ok(updatedTodo) : Results.NotFound();
});

app.MapDelete("/api/todos/{id}", async (int id, IMongoDatabase db) =>
{
    var collection = db.GetCollection<TodoItem>("TodoItems");
    var result = await collection.DeleteOneAsync(t => t.Id == id);
    return result.DeletedCount > 0 ? Results.Ok() : Results.NotFound();
});

app.Run();

// Todo model
public class TodoItem
{
    [BsonId]
    public ObjectId _id { get; set; }
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsComplete { get; set; }
}