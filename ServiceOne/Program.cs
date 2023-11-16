using System.Text.Json;
using Bogus;
using Microsoft.AspNetCore.Mvc;
using Shared;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<RedisService>();
builder.Services.AddHostedService<RedisBackgroundService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

WebApplication app = builder.Build();

app.Services.GetRequiredService<RedisService>().Connect();

app.UseSwagger();
app.UseSwaggerUI();

app.MapPost("/redis", ([FromServices] ILogger<Program> logger, [FromServices] RedisService redis) =>
{
	Faker<RedisItem> faker = new Faker<RedisItem>()
		.RuleFor(o => o.Id, _ => Guid.NewGuid())
		.RuleFor(o => o.FullName, f => f.Name.FullName());

	RedisItem item = faker.Generate();

	bool result = redis.Connection.GetDatabase(-1).StringSet(item.Id.ToString(), JsonSerializer.Serialize(item));

	return result ? Results.Ok(result) : Results.UnprocessableEntity();
});

app.Run();