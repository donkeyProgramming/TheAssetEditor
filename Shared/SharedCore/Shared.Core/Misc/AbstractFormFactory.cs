namespace Shared.Core.Misc
{
    public interface IAbstractFormFactory<T>
    {
        T Create();
    }

    public class AbstractFormFactory<T> : IAbstractFormFactory<T>
    {
        private readonly Func<T> _factory;

        public AbstractFormFactory(Func<T> factory)
        {
            _factory = factory;
        }

        public T Create() => _factory();
    }
}
