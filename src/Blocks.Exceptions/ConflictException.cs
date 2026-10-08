using System.Net;

namespace Blocks.Exceptions;

public class ConflictException : HttpException
{
    public ConflictException(string exceptionMessage) : base(HttpStatusCode.Conflict, exceptionMessage) { }

    public ConflictException(string exceptionMessage, Exception exception) : base(HttpStatusCode.Conflict, exceptionMessage, exception) { }
}
