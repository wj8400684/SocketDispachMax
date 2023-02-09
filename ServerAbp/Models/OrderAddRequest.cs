namespace ServerAbp;

public sealed class OrderAddRequest
{
    /// <summary>
    /// 订单内容
    /// </summary>
    public string Content { get; set; } = null!;
}

public sealed class OrderAddResponse
{
    public bool SuccessFul { get; set; }

    public string? ErrorMessage { get; set; }
}