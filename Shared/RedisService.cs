using StackExchange.Redis;

namespace Shared;

public class RedisService
{
	public ConnectionMultiplexer Connection { get; private set; }

	public void Connect()
	{
		Connection = ConnectionMultiplexer.Connect("localhost");
	}
}