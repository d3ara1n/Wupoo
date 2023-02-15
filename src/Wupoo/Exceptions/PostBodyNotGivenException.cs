namespace Wupoo.Exceptions;

public class PostBodyNotGivenException : WapooException
{
    public PostBodyNotGivenException()
        : base("Method Post requires a HTTP Post body.")
    {
    }
}