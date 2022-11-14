using AwesomeCompany;
using AwesomeCompany.Entities;
using Dapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<DatabaseContext>(
    o => o.UseSqlServer(builder.Configuration.GetConnectionString("Database")));

// Add services to the container.
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapPut("increase-salaries", async (int companyId, DatabaseContext dbContext) =>
{
    var company = await dbContext
    .Set<Company>()
    .Include(c => c.Employees)
    .FirstOrDefaultAsync(x => x.Id == companyId);

    if(company is null)
    {
        return Results.NotFound($"The company with Id is {companyId} was not found.");
    }

    foreach (var item in company.Employees)
    {
        item.Salary *= 1.1m;
    }

    company.LastSalariesUpdateUtc = DateTime.UtcNow;

    await dbContext.SaveChangesAsync();

    return Results.NoContent();
});

app.MapPut("increase-salaries-sql", async (int companyId, DatabaseContext dbContext) =>
{
    var company = await dbContext
    .Set<Company>()
    .Include(c => c.Employees)
    .FirstOrDefaultAsync(x => x.Id == companyId);

    if (company is null)
    {
        return Results.NotFound($"The company with Id is {companyId} was not found.");
    }



    await dbContext.Database.BeginTransactionAsync();

    await dbContext.Database.ExecuteSqlInterpolatedAsync(
        $"UPDATE Employees  SET Salary = Salary * 1.1 WHERE CompanyId = 1");
    company.LastSalariesUpdateUtc = DateTime.UtcNow;

    await dbContext.SaveChangesAsync();

    await dbContext.Database.CommitTransactionAsync();

    return Results.NoContent();
});


app.MapPut("increase-salaries-sql-dapper", async (int companyId, DatabaseContext dbContext) =>
{
    var company = await dbContext
    .Set<Company>()
    .Include(c => c.Employees)
    .FirstOrDefaultAsync(x => x.Id == companyId);

    if (company is null)
    {
        return Results.NotFound($"The company with Id is {companyId} was not found.");
    }
    var transaction = await dbContext.Database.BeginTransactionAsync();
    await dbContext.Database.GetDbConnection().ExecuteAsync(
        $"UPDATE Employees  SET Salary = Salary * 1.1 WHERE CompanyId = @CompanyId",
        new { CompanyId = 1 }, transaction.GetDbTransaction());

    company.LastSalariesUpdateUtc = DateTime.UtcNow;

    await dbContext.SaveChangesAsync();

    await dbContext.Database.CommitTransactionAsync();

    return Results.NoContent();
});

app.MapControllers();

app.Run();
