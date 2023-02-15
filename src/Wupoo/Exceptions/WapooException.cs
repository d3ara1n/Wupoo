using System;

namespace Wupoo.Exceptions;

public class WapooException : Exception
{
    public WapooException()
    {
    }

    public WapooException(string message)
        : base(message)
    {
    }

    public WapooException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}