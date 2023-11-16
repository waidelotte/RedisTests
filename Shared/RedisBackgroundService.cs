using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RedLockNet;
using RedLockNet.SERedis;
using RedLockNet.SERedis.Configuration;
using StackExchange.Redis;

namespace Shared;

public class RedisBackgroundService : BackgroundService
{
	private readonly ILogger<RedisBackgroundService> _logger;
	private readonly IServer _server;
	private readonly IDatabase _db;
	private readonly RedLockFactory _lockFactory;

	public RedisBackgroundService(RedisService redis, ILogger<RedisBackgroundService> logger)
	{
		_logger = logger;
		_server = redis.Connection.GetServer("localhost", 6379);
		_db = redis.Connection.GetDatabase(-1);
		_lockFactory = RedLockFactory.Create(new List<RedLockMultiplexer> { redis.Connection });
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		while (!stoppingToken.IsCancellationRequested)
		{
			await using (IRedLock? redisLock = await _lockFactory.CreateLockAsync("lock", TimeSpan.FromSeconds(60)))
			{
				if (redisLock.IsAcquired)
				{
					await ProcessAsync();
				}
			}

			await Task.Delay(50, stoppingToken);
		}
	}

	private async Task ProcessAsync()
	{
		foreach (RedisKey key in _server.Keys(database: -1))
		{
			if(key.ToString().StartsWith("redlock")) continue;

			_logger.LogInformation("Process key {Key}", key);

			RedisValue redisValue = _db.StringGet(key);

		    if (!redisValue.HasValue)
		    {
			    _logger.LogInformation("Key [{Key}] value is empty. Return", key);
			    return;
		    }

			_logger.LogInformation("Processing key {Key} with value {Value}", key, redisValue.ToString());

			await Task.Delay(70);

			_logger.LogInformation("Deleting key [{Key}] after processing", key);
			_db.KeyDelete(key);
		}
	}
}