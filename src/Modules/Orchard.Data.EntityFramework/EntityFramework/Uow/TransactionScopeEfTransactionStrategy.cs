using Orchard.Domain.Uow;
using System.Collections.Generic;
using System.Data.Entity;
using System.Transactions;

namespace Orchard.Data.EntityFramework.Uow
{
    public class TransactionScopeEfTransactionStrategy : IEfTransactionStrategy, ITransientDependency
    {
        protected UnitOfWorkOptions Options { get; private set; }

        protected TransactionScope CurrentTransaction { get; set; }

        protected List<DbContext> DbContexts { get; }

        public TransactionScopeEfTransactionStrategy()
        {
            DbContexts = new List<DbContext>();
        }

        public virtual void InitOptions(UnitOfWorkOptions options)
        {
            Options = options;

            StartTransaction();
        }

        public virtual void Commit()
        {
            if (CurrentTransaction == null)
            {
                return;
            }

            CurrentTransaction.Complete();

            CurrentTransaction.Dispose();
            CurrentTransaction = null;
        }

        private void StartTransaction()
        {
            if (CurrentTransaction != null)
            {
                return;
            }

            var transactionOptions = new TransactionOptions
            {
                IsolationLevel = Options.IsolationLevel.GetValueOrDefault(IsolationLevel.ReadUncommitted),
            };

            if (Options.Timeout.HasValue)
            {
                transactionOptions.Timeout = Options.Timeout.Value;
            }

            CurrentTransaction = new TransactionScope(
                Options.Scope.GetValueOrDefault(TransactionScopeOption.Required),
                transactionOptions,
                Options.AsyncFlowOption.GetValueOrDefault(TransactionScopeAsyncFlowOption.Enabled)
            );
        }

        public virtual void Dispose()
        {
            DbContexts.Clear();
            if (CurrentTransaction != null)
            {
                CurrentTransaction.Dispose();
                CurrentTransaction = null;
            }
        }
    }
}