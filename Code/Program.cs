using Microsoft.EntityFrameworkCore;
using Amazon.S3;
using Saritasa;
using Amazon.Runtime.CredentialManagement;

var builder = WebApplication.CreateBuilder(args);
var dbHost=Environment.GetEnvironmentVariable("DB_HOST");
var dbName=Environment.GetEnvironmentVariable("DB_NAME");
var dbPassword=Environment.GetEnvironmentVariable("DB_SA_PASSWORD");
var connectionString=$"Server={dbHost};Database={dbName};User Id=sa;Password={dbPassword};";
// Add services to the container.
// builder.Services.AddDbContext<UserDataContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddDbContext<UserDataContext>(options => options.UseSqlServer(connectionString));
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDefaultAWSOptions(builder.Configuration.GetAWSOptions());
builder.Services.AddAWSService<IAmazonS3>();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
