namespace OnlineBookStore.Application.Common.Interfaces
{
    public interface IUnitOfWork
    {
        IUserRepository UserRepository { get; }
    }
}
