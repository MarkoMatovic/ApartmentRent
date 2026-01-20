using Lander.Helpers;
namespace Lander.src.Common
{
    public interface IAggregateRoot { }
    public interface IRepository<T> where T : class
    {
        IUnitofWork UnitOfWork {get;}
        T Add(T entity);
    }
}
