using OnlineBookStore.Application.Common.Interfaces;
using OnlineBookStore.Infrastructure.Data;
using OnlineBookStore.Infrastructure.Data.DataAccess;

namespace OnlineBookStore.Infrastructure.Repository
{
    public sealed class UnitOfWork(ISqlDataAccess sqlDataAccess, ApplicationDbContext _db,
        IConfiguration _configuration) : IUnitOfWork
    {
        private readonly Lazy<IUserRepository> _LazyUserRepository = new Lazy<IUserRepository>(() => new UserRepository(_db, _configuration, sqlDataAccess));
  
        public IUserRepository UserRepository => _LazyUserRepository.Value;
    }
}
