var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("HospitalManagementDB");
Console.WriteLine("Connection String from appsettings.json: " + connectionString);

// Dodaj serwisy do Dependency Injection
builder.Services.AddControllers();
builder.Services.AddSwaggerGen(); // Dodaj Swaggera
builder.Services.AddSingleton<DatabaseService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();
