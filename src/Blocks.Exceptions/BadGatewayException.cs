using System.Net;

namespace Blocks.Exceptions;

public class BadGatewayException : HttpException
{
    public BadGatewayException(string exceptionMessage) : base(HttpStatusCode.BadGateway, exceptionMessage) { }

    public BadGatewayException(string exceptionMessage, Exception exception) : base(HttpStatusCode.BadGateway, exceptionMessage, exception) { }
}
