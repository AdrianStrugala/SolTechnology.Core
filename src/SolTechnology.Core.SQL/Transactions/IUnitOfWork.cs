using System.Transactions;

namespace SolTechnology.Core.SQL.Transactions;

/// <summary>
/// Groups database work into one ambient <see cref="TransactionScope"/> that flows across <c>await</c>. Connections
/// opened between <see cref="Begin"/> and <see cref="Complete"/> enlist in it. Registered as transient by
/// <see cref="ModuleInstaller.AddSolSQL"/>; disposing it without <see cref="Complete"/> rolls the work back.
/// </summary>
public interface IUnitOfWork : IDisposable
{
    /// <summary>
    /// Opens the transaction scope and returns this unit of work, so it can be used in a <c>using</c> statement.
    /// </summary>
    /// <param name="isolationLevel">Isolation level of the transaction.</param>
    /// <exception cref="InvalidOperationException">The scope is already open.</exception>
    IUnitOfWork Begin(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted);

    /// <summary>
    /// Commits the work and closes the scope; <see cref="Begin"/> can then open a new one.
    /// </summary>
    /// <exception cref="InvalidOperationException"><see cref="Begin"/> was not called.</exception>
    void Complete();

    /// <summary>
    /// Rolls the work back and closes the scope. Does nothing when no scope is open.
    /// </summary>
    void Rollback();
}
