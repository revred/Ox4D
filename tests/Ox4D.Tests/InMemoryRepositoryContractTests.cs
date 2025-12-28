using Ox4D.Store;

namespace Ox4D.Tests;

/// <summary>
/// Contract tests for InMemoryDealRepository.
/// Ensures InMemoryDealRepository adheres to the IDealRepository contract.
/// </summary>
public class InMemoryRepositoryContractTests : RepositoryContractTests<InMemoryDealRepository>
{
    protected override InMemoryDealRepository CreateRepository()
    {
        return new InMemoryDealRepository();
    }

    protected override Task CleanupRepository(InMemoryDealRepository repository)
    {
        repository.Clear();
        return Task.CompletedTask;
    }
}
