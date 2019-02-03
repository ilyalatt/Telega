namespace Telega.CallMiddleware
{
    public interface ITgCallMiddleware
    {
        TgCallHandler<T> Handle<T>(TgCallHandler<T> next);
    }
}