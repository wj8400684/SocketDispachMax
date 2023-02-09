using Core;
using Work;

var client = new PlayClient();

client.RegisterHandle<ClientOfflinePacket>(OnClientOfflineAsync);
client.RegisterHandle<ClientOnlinePacket>(OnClientOnlineAsync);

await client.StartAsync();

await client.LoginAsync("kk", "hhhh");

Task OnClientOnlineAsync(ClientOnlinePacket packet)
{
    Console.WriteLine($"客户端上线!名称:[{packet.Name}] ｜ 地址:[{packet.Address}] ｜ 账号:[{packet.UserId}]");
    return Task.CompletedTask;
}

Task OnClientOfflineAsync(ClientOfflinePacket packet)
{
    Console.WriteLine($"客户端下线!名称:[{packet.Name}] ｜ 地址:[{packet.Address}] ｜ 账号:[{packet.UserId}]");
    return Task.CompletedTask;
}

//Console.ReadKey();

using var tokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(10));

while (true)
{
    try
    {
        var reply = await client.AddOrderAsync(CancellationToken.None);

        Console.WriteLine($"添加订单结果：{reply.SuccessFul}");

        var reply1 = await client.RefreshOrderAsync();

        Console.WriteLine($"刷新订单结果：{reply1.SuccessFul}");

        var reply2 = await client.RestartOrderAsync();

        Console.WriteLine($"重启订单结果：{reply2.SuccessFul}");
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine(ex);
    }
    
    await Task.Delay(1);
}