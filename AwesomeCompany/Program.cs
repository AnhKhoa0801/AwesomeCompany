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
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.DisplayRequestDuration();
});

app.UseHttpsRedirection();

app.MapPut("increase-salaries", async (int companyId, DatabaseContext dbContext) =>
{
    var company = await dbContext
    .Set<Company>()
    .Include(c => c.Employees)
    .FirstOrDefaultAsync(x => x.Id == companyId);

    if (company is null)
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

app.MapPut("increase-salaries-v2", async (int companyId, DatabaseContext dbContext) =>
{
    var company = await dbContext
    .Set<Company>()
    .FirstOrDefaultAsync(x => x.Id == companyId);

    if (company is null)
    {
        return Results.NotFound($"The company with Id is {companyId} was not found.");
    }

    await dbContext.Set<Employee>()
             .Where(e => e.CompanyId == company.Id)
             .ExecuteUpdateAsync(s => s.SetProperty(
                 e => e.Salary,
                 e => e.Salary * 1.1m));

    return Results.NoContent();
});

app.MapPut("increase-salaries-sql", async (int companyId, DatabaseContext dbContext) =>
{
    var company = await dbContext
    .Set<Company>()
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

app.MapDelete("delete-employees", async (
    int companyId,
    decimal salaryThreshold,
    DatabaseContext dbContext) =>
{
    var company = await dbContext
    .Set<Company>()
    .FirstOrDefaultAsync(x => x.Id == companyId);

    if (company is null)
    {
        return Results.NotFound($"The company with Id is {companyId} was not found.");
    }

    await dbContext.Set<Employee>()
             .Where(e => e.CompanyId == company.Id && e.Salary > salaryThreshold)
             .ExecuteDeleteAsync();

    return Results.NoContent();
});

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
