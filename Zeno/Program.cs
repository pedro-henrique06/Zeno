using FluentValidation;
using Zeno.Application.Interfaces;
using Zeno.Application.Services;
using Zeno.Infrastructure.SQL.Extentions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddValidatorsFromAssemblyContaining<Zeno.Application.Validators.EntryValidator>();
builder.Services.AddInfrastructureSQL(builder.Configuration.GetConnectionString("DefaultConnection")!);
builder.Services.AddScoped<IEntryService, EntryService>();
builder.Services.AddScoped<IWalletService, WalletService>();

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
