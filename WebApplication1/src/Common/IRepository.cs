namespace Lander.src.Common
{
    public interface IAggregateRoot { }
    public interface IRepository<T> where T : class
    {
        IUnitOfWork UnitOfWork {get;}

        T Add(T entity);
    }
}
