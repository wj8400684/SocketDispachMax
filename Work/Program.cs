using Core;
using Work;

/// <summary>
/// 当前为工作客户端
/// </summary>
class Program
{
    [STAThread]
    static async Task Main(string[] args)
    {
        var client = new PlayClient();

        client.RegisterHandle<OrderPacket, OrderRespPacket>(OnOrderAsync)
              .RegisterHandle<RefreshPacket, RefreshRespPacket>(OnRefreshAsync)
              .RegisterHandle<RestartPacket, RestartRespPacket>(OnRestartAsync);

        //开始
        await client.StartAsync();

        //授权客户端
        //await client.AuthorizeAsync((ulong)Random.Shared.Next(100000000));
        await client.AuthorizeAsync(1554106598);
        
        async Task<RestartRespPacket> OnRestartAsync(RestartPacket packet)
        {
            Console.WriteLine("重启订单");

            await Task.Delay(TimeSpan.FromMilliseconds(Random.Shared.Next(100)));

            return new RestartRespPacket
            {
                SuccessFul = true,
            };
        }

        async Task<RefreshRespPacket> OnRefreshAsync(RefreshPacket packet)
        {
            Console.WriteLine("刷新订单");

            await Task.Delay(TimeSpan.FromMilliseconds(Random.Shared.Next(100)));

            return new RefreshRespPacket
            {
                SuccessFul = true,
            };
        }

        async Task<OrderRespPacket> OnOrderAsync(OrderPacket packet)
        {
            Console.WriteLine("添加订单");

            await Task.Delay(TimeSpan.FromMilliseconds(Random.Shared.Next(100)));

            return new OrderRespPacket
            {
                SuccessFul = true,
            };
        }

        while (true)
        {
            await Task.Delay(1000);
        }
    }
}

//
// BasePacket request = new LoginPacket
// {
//     Username = "wujun",
//     Password = "wujun520.",
//     Id = 1,
// };
//
// var body1 = request.EncodeBody();
//
//
//
// var loginRequest = new BasePacket(new LoginPacket
// {
//     Username = "wujun",
//     Password = "wujun520.",
// })
// {
//     Key = CommandKey.Login,
// };
//
// var stream = new MemoryStream();
//
// ProtoBuf.Serializer.Serialize(stream, loginRequest);
//
// var body = stream.ToArray();
//
// stream = new MemoryStream(body);
//
// var type = typeof(BasePacket).MakeGenericType(typeof(LoginPacket));
//
// var s = ProtoBuf.Serializer.Deserialize(type, stream);
//
// loginRequest = ProtoBuf.Serializer.Deserialize<BasePacket>(stream);
//
// Console.WriteLine("Hello, World!");