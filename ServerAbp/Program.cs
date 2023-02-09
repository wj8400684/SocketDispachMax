using Core;
using Server;
using SuperSocket;
using SuperSocket.Command;
using SuperSocket.IOCPTcpChannelCreatorFactory;
using SuperSocket.ProtoBase;

var builder = WebApplication.CreateBuilder(args);

builder.Host.AsSuperSocketHostBuilder<PlayPacket, PlayPipeLineFilter>()
    .UseSession<PlaySession>()
    .UseHostedService<PlayServer>()
    .UseCommand(options =>
    {
        options.AddCommandAssembly(typeof(Authorize).Assembly);
    })
    .UseClearIdleSession()
    .UseInProcSessionContainer()
    .UseIOCPTcpChannelCreatorFactory()
    .AsMinimalApiHostBuilder()
    .ConfigureHostBuilder();

builder.Services.AddSingleton<IPackageEncoder<PlayPacket>>(new PlayPacketEncode());
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{

//}

app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();