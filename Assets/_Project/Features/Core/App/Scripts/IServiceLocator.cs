namespace TypingSurvivor.Features.Core.App
{
    /// <summary>
    /// A simple service locator interface.
    /// </summary>
    public interface IServiceLocator
    {
        T GetService<T>();
        void RegisterService<T>(T service);
    }
}
