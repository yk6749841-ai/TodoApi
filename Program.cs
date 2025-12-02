

using Microsoft.EntityFrameworkCore;
using TodoApi;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;



var builder = WebApplication.CreateBuilder(args);

// הוספת DbContext לשירותים עם ה-Connection String
builder.Services.AddDbContext<ToDoDbContext>(options =>
    options.UseMySql(builder.Configuration.GetConnectionString("ToDoDB"),
        new MySqlServerVersion(new Version(8, 0, 44))));

// הגדרת CORS – מאפשר לכל דומיין לקרוא את ה-API
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// הוספת Swagger ל‑API
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// הפעלת CORS
app.UseCors();

// הפעלת Swagger
app.UseSwagger();
app.UseSwaggerUI();

// Root endpoint לבדיקה שה־API פעיל
app.MapGet("/", () => Results.Ok("API is running!"));

// Route: שליפת כל המשימות
app.MapGet("/tasks", async (ToDoDbContext db) =>
{
    var tasks = await db.Items.ToListAsync();
    return Results.Ok(tasks);
});

// Route: הוספת משימה חדשה
app.MapPost("/tasks", async (Item task, ToDoDbContext db) =>
{
    db.Items.Add(task);
    await db.SaveChangesAsync();
    return Results.Created($"/tasks/{task.Id}", task);
});


app.MapPut("/tasks/{id}", async (int id, ToDoDbContext db, JsonElement body) =>
{
    var task = await db.Items.FindAsync(id);
    if (task is null) return Results.NotFound();

    // מקבלים רק את isComplete מהקליינט
    if (body.TryGetProperty("isComplete", out var isCompleteProp))
    {
        task.IsComplete = isCompleteProp.GetBoolean();
    }

    await db.SaveChangesAsync();
    return Results.Ok(task);
});



// Route: מחיקת משימה
app.MapDelete("/tasks/{id}", async (int id, ToDoDbContext db) =>
{
    var task = await db.Items.FindAsync(id);
    if (task is null) return Results.NotFound();

    db.Items.Remove(task);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

app.Run();

