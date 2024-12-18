using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Read API key from app settings
var apiKey = builder.Configuration["ApiKey"];

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
   
}

app.UseHttpsRedirection();

// Logging middleware
app.Use(async (context, next) =>
{
    Console.WriteLine($"Request: {context.Request.Method} {context.Request.Path}");
    await next.Invoke();
    Console.WriteLine($"Response: {context.Response.StatusCode}");
});

// API key validation middleware for PUT and POST methods
app.UseWhen(context => context.Request.Method == HttpMethods.Put || context.Request.Method == HttpMethods.Post, appBuilder =>
{
    appBuilder.Use(async (context, next) =>
    {
        if (!context.Request.Headers.TryGetValue("ApiKey", out var extractedApiKey) || extractedApiKey != apiKey)
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("Unauthorized");
            return;
        }
        await next.Invoke();
    });
});

// Employee entity dictionary
var employees = new Dictionary<int, Employee>
{
    { 1, new Employee { Id = 1, Name = "John Doe", Position = "Developer" } },
    { 2, new Employee { Id = 2, Name = "Jane Smith", Position = "Manager" } }
};

// Get all employees
app.MapGet("/employees", () => employees.Values)
   .WithName("GetEmployees");

// Get employee by ID
app.MapGet("/employees/{id}", (int id) =>
{
    return employees.TryGetValue(id, out var employee) ? Results.Ok(employee) : Results.NotFound();
})
.WithName("GetEmployeeById");

// Create a new employee
app.MapPost("/employees", (Employee employee) =>
{
    employee.Id = employees.Keys.Max() + 1;
    employees[employee.Id] = employee;
    return Results.Created($"/employees/{employee.Id}", employee);
})
.WithName("CreateEmployee");

// Update an existing employee
app.MapPut("/employees/{id}", (int id, Employee updatedEmployee) =>
{
    if (!employees.ContainsKey(id)) return Results.NotFound();

    var employee = employees[id];
    employee.Name = updatedEmployee.Name;
    employee.Position = updatedEmployee.Position;
    return Results.Ok(employee);
})
.WithName("UpdateEmployee");

// Delete an employee
app.MapDelete("/employees/{id}", (int id) =>
{
    if (!employees.ContainsKey(id)) return Results.NotFound();

    employees.Remove(id);
    return Results.NoContent();
})
.WithName("DeleteEmployee");

app.Run("http://localhost:5000");

record Employee
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Position { get; set; }
}
