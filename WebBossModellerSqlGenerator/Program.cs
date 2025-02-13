using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

var builder = WebApplication.CreateBuilder(args);
var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";
// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: MyAllowSpecificOrigins,
                      builder =>
                      {
                          builder.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
                      });
});
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddScoped<MSSQLDatabaseService>();
builder.Services.AddScoped<PostgreSQLDatabaseService>();
builder.Services.AddScoped<MySQLDatabaseService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
    });
}

//app.UseHttpsRedirection();
app.UseCors(MyAllowSpecificOrigins); // Add this line

app.UseAuthorization();

app.MapControllers();

// Add a custom endpoint to serve the OpenAPI YAML file
app.MapGet("/swagger/v1/swagger.yaml", async context =>
{
    var json = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "swagger/v1/swagger.json"));
    var deserializer = new DeserializerBuilder().Build();
    var serializer = new SerializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .Build();

    var yaml = serializer.Serialize(deserializer.Deserialize<object>(json));
    await context.Response.WriteAsync(yaml);
});

app.Run();
