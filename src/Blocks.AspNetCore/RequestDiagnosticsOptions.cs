using System.ComponentModel.DataAnnotations;

namespace Blocks.AspNetCore;

public class RequestDiagnosticsOptions
{
    [Range(1, int.MaxValue)]
    public int SlowRequestMs { get; init; } = 1000;

    [Range(1, int.MaxValue)]
    public int SlowFileTransferMs { get; init; } = 3000;
}
