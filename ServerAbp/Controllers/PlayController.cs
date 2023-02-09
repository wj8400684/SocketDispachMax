using System.Text;
using Core;
using Microsoft.AspNetCore.Mvc;
using ServerAbp;
using SuperSocket;

namespace Server.Controllers;

[ApiController]
[Route("[controller]")]
public sealed class PlayController : ControllerBase
{
    private readonly ILogger<PlayController> _logger;
    private readonly IAsyncSessionContainer _sessionContainer;

    public PlayController(IAsyncSessionContainer sessionContainer, ILogger<PlayController> logger)
    {
        _logger = logger;
        _sessionContainer = sessionContainer;
    }

    /// <summary>
    /// 添加订单
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPost]
    [Route("order/add")]
    public async ValueTask<OrderAddResponse> OrderAddAsync([FromBody] OrderAddRequest request)
    {
        var sessions = await _sessionContainer.GetSessionsAsync<PlaySession>(s => !s.IsAdmin);

        return new OrderAddResponse();
    }
    
    /// <summary>
    /// 指定客户端添加订单
    /// </summary>
    /// <param name="clientId"></param>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPost]
    [Route("order/add/{clientId}")]
    public async ValueTask<OrderAddResponse> OrderAddAsync(ulong clientId, [FromBody] OrderAddRequest request)
    {
        //查找客户端id
        var sessions = await _sessionContainer.GetSessionsAsync<PlaySession>(s => s.UserId == clientId);

        if (sessions == null || !sessions.Any())
            return new OrderAddResponse
            {
                SuccessFul = false,
                ErrorMessage = "客户端不在线"
            };

        var session = sessions.First();

        await session.SendPacketAsync(new ForwardPacket
        {
            TargetId = 20424666,//管理员id
            Body = Encoding.UTF8.GetBytes(request.Content)
        });
        
        return new OrderAddResponse
        {
            SuccessFul = true
        };
    }

    [HttpGet]
    [Route("order/refresh/{clientId}")]
    public async ValueTask<IActionResult> OrderRefreshAsync(string clientId)
    {
        var sessions = await _sessionContainer.GetSessionsAsync<PlaySession>(s => !s.IsAdmin);


        return Ok();
    }

    [HttpGet]
    [Route("order/restart/{clientId}")]
    public async ValueTask<IActionResult> OrderRestartAsync(string clientId)
    {
        var sessions = await _sessionContainer.GetSessionsAsync<PlaySession>(s => !s.IsAdmin);


        return Ok();
    }

    [HttpGet]
    [Route("client/details")]
    public async ValueTask<IActionResult> GetSessionDetailsAsync()
    {
        var sessions = await _sessionContainer.GetSessionsAsync<PlaySession>(s => !s.IsAdmin);


        return Ok();
    }
}