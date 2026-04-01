using Pillaro.Dataverse.PluginFramework.Testing.Tests;
using Pillaro.Dataverse.PluginFramework.Tests.Data.Repositories;
using System.Linq.Expressions;

namespace Pillaro.Dataverse.PluginFramework.Tests.Tests.DataService;

[Trait("Category", "DataService")]
[Trait("Category", "DataService.Query")]
public class DataServiceQueryIntegrationTests : TestBase
{
    private readonly PluginFramework.Data.DataService _dataService;

    public DataServiceQueryIntegrationTests(TestFixture<TestAutofacModule> testFixture) : base(testFixture)
    {
        _dataService = new PluginFramework.Data.DataService(OrganizationService);
    }

    [Fact]
    public void Query_WhereById_ReturnsExpectedEntity()
    {
        var repo = DataService.GetRepository<AccountRepository>();
        var acc = repo.GetNewAccount(1);
        acc.Id = DataService.CreateTestEntity(acc);

        var result = _dataService.Query<Account>().Where(a => a.Id == acc.Id).SingleOrDefault();

        Assert.NotNull(result);
        Assert.Equal(acc.Id, result.Id);
        Assert.Equal(acc.Name, result.Name);
        Assert.Equal(acc.Description, result.Description);
        Assert.Equal(acc.Telephone1, result.Telephone1);
    }

    [Fact]
    public void Query_WhenEntityDoesNotExist_ReturnsNull()
    {
        var result = _dataService.Query<Account>().Where(a => a.Id == Guid.NewGuid()).SingleOrDefault();

        Assert.Null(result);
    }

    [Fact]
    public void Query_CalledMultipleTimes_ReturnsSeparateResults()
    {
        var repo = DataService.GetRepository<AccountRepository>();

        var acc1 = repo.GetNewAccount(1);
        acc1.Id = DataService.CreateTestEntity(acc1);

        var acc2 = repo.GetNewAccount(2);
        acc2.Id = DataService.CreateTestEntity(acc2);

        var result1 = _dataService.Query<Account>().Where(a => a.Id == acc1.Id).SingleOrDefault();
        var result2 = _dataService.Query<Account>().Where(a => a.Id == acc2.Id).SingleOrDefault();

        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.Equal(acc1.Id, result1.Id);
        Assert.Equal(acc2.Id, result2.Id);
        Assert.NotEqual(result1.Id, result2.Id);
    }

    [Fact]
    public void Query_Where_ReturnsOnlyMatchingRecords()
    {
        var repo = DataService.GetRepository<AccountRepository>();
        var key = Guid.NewGuid().ToString();

        var acc1 = repo.GetNewAccount($"{key}-01", "Test account", key);
        acc1.Id = DataService.CreateTestEntity(acc1);

        var acc2 = repo.GetNewAccount($"{key}-02", "Test account", key);
        acc2.Id = DataService.CreateTestEntity(acc2);

        var acc3 = repo.GetNewAccount($"{key}-03", "Test account", Guid.NewGuid().ToString());
        acc3.Id = DataService.CreateTestEntity(acc3);

        var result = _dataService.Query<Account>().Where(a => a.Telephone1 == key).ToList();

        Assert.True(result.Count >= 2);
        Assert.Contains(result, a => a.Id == acc1.Id);
        Assert.Contains(result, a => a.Id == acc2.Id);
        Assert.DoesNotContain(result, a => a.Id == acc3.Id);
    }

    [Fact]
    public void Query_OrderBy_ReturnsAscendingOrder()
    {
        var repo = DataService.GetRepository<AccountRepository>();
        var key = Guid.NewGuid().ToString();

        var acc3 = repo.GetNewAccount($"{key}-03", "Test account", key);
        acc3.Id = DataService.CreateTestEntity(acc3);

        var acc1 = repo.GetNewAccount($"{key}-01", "Test account", key);
        acc1.Id = DataService.CreateTestEntity(acc1);

        var acc2 = repo.GetNewAccount($"{key}-02", "Test account", key);
        acc2.Id = DataService.CreateTestEntity(acc2);

        var result = _dataService.Query<Account>().Where(a => a.Telephone1 == key).OrderBy(a => a.Name).ToList();

        Assert.True(result.Count >= 3);
        Assert.Equal(acc1.Id, result[0].Id);
        Assert.Equal(acc2.Id, result[1].Id);
        Assert.Equal(acc3.Id, result[2].Id);
    }

    [Fact]
    public void Query_OrderByDescending_ReturnsDescendingOrder()
    {
        var repo = DataService.GetRepository<AccountRepository>();
        var key = Guid.NewGuid().ToString();

        var acc1 = repo.GetNewAccount($"{key}-01", "Test account", key);
        acc1.Id = DataService.CreateTestEntity(acc1);

        var acc2 = repo.GetNewAccount($"{key}-02", "Test account", key);
        acc2.Id = DataService.CreateTestEntity(acc2);

        var acc3 = repo.GetNewAccount($"{key}-03", "Test account", key);
        acc3.Id = DataService.CreateTestEntity(acc3);

        var result = _dataService.Query<Account>().Where(a => a.Telephone1 == key).OrderByDescending(a => a.Name).ToList();

        Assert.True(result.Count >= 3);
        Assert.Equal(acc3.Id, result[0].Id);
        Assert.Equal(acc2.Id, result[1].Id);
        Assert.Equal(acc1.Id, result[2].Id);
    }

    [Fact]
    public void Query_Take_ReturnsLimitedResults()
    {
        var repo = DataService.GetRepository<AccountRepository>();
        var key = Guid.NewGuid().ToString();

        for (int i = 0; i < 3; i++)
        {
            var acc = repo.GetNewAccount($"{key}-{i}", "Test account", key);
            acc.Id = DataService.CreateTestEntity(acc);
        }

        var result = _dataService.Query<Account>().Where(a => a.Telephone1 == key).OrderBy(a => a.Name).Take(2).ToList();

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void Query_Skip_ReturnsRemainingResults()
    {
        var repo = DataService.GetRepository<AccountRepository>();
        var key = Guid.NewGuid().ToString();

        var acc1 = repo.GetNewAccount($"{key}-01", "Test account", key);
        acc1.Id = DataService.CreateTestEntity(acc1);

        var acc2 = repo.GetNewAccount($"{key}-02", "Test account", key);
        acc2.Id = DataService.CreateTestEntity(acc2);

        var acc3 = repo.GetNewAccount($"{key}-03", "Test account", key);
        acc3.Id = DataService.CreateTestEntity(acc3);

        var result = _dataService.Query<Account>().Where(a => a.Telephone1 == key).OrderBy(a => a.Name).Skip(1).ToList();

        Assert.True(result.Count >= 2);
        Assert.Equal(acc2.Id, result[0].Id);
        Assert.Equal(acc3.Id, result[1].Id);
    }

    [Fact]
    public void Query_Select_ReturnsProjectedValues()
    {
        var repo = DataService.GetRepository<AccountRepository>();
        var key = Guid.NewGuid().ToString();

        var acc1 = repo.GetNewAccount($"{key}-01", "Test account", key);
        acc1.Id = DataService.CreateTestEntity(acc1);

        var acc2 = repo.GetNewAccount($"{key}-02", "Test account", key);
        acc2.Id = DataService.CreateTestEntity(acc2);

        var result = _dataService.Query<Account>().Where(a => a.Telephone1 == key).OrderBy(a => a.Name).Select(a => a.Name).ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal($"{key}-01", result[0]);
        Assert.Equal($"{key}-02", result[1]);
    }

    [Fact]
    public void Query_First_ReturnsFirstRecord()
    {
        var repo = DataService.GetRepository<AccountRepository>();
        var key = Guid.NewGuid().ToString();

        var acc2 = repo.GetNewAccount($"{key}-02", "Test account", key);
        acc2.Id = DataService.CreateTestEntity(acc2);

        var acc1 = repo.GetNewAccount($"{key}-01", "Test account", key);
        acc1.Id = DataService.CreateTestEntity(acc1);

        var result = _dataService.Query<Account>().Where(a => a.Telephone1 == key).OrderBy(a => a.Name).First();

        Assert.Equal(acc1.Id, result.Id);
    }

    [Fact]
    public void Query_FirstOrDefault_WhenNoMatch_ReturnsNull()
    {
        var result = _dataService.Query<Account>().Where(a => a.Telephone1 == Guid.NewGuid().ToString()).FirstOrDefault();

        Assert.Null(result);
    }

    [Fact]
    public void Query_Single_WhenMultiple_Throws()
    {
        var repo = DataService.GetRepository<AccountRepository>();
        var key = Guid.NewGuid().ToString();

        var acc1 = repo.GetNewAccount($"{key}-01", "Test account", key);
        acc1.Id = DataService.CreateTestEntity(acc1);

        var acc2 = repo.GetNewAccount($"{key}-02", "Test account", key);
        acc2.Id = DataService.CreateTestEntity(acc2);

        Assert.Throws<InvalidOperationException>(() => _dataService.Query<Account>().Where(a => a.Telephone1 == key).Single());
    }

    [Fact]
    public void Query_SingleOrDefault_WhenNoMatch_ReturnsNull()
    {
        var result = _dataService.Query<Account>().Where(a => a.Telephone1 == Guid.NewGuid().ToString()).SingleOrDefault();

        Assert.Null(result);
    }

    [Fact]
    public void Query_Page_ReturnsCorrectPage()
    {
        var repo = DataService.GetRepository<AccountRepository>();
        var key = Guid.NewGuid().ToString();

        var acc1 = repo.GetNewAccount($"{key}-01", "Test account", key);
        acc1.Id = DataService.CreateTestEntity(acc1);

        var acc2 = repo.GetNewAccount($"{key}-02", "Test account", key);
        acc2.Id = DataService.CreateTestEntity(acc2);

        var acc3 = repo.GetNewAccount($"{key}-03", "Test account", key);
        acc3.Id = DataService.CreateTestEntity(acc3);

        var acc4 = repo.GetNewAccount($"{key}-04", "Test account", key);
        acc4.Id = DataService.CreateTestEntity(acc4);

        var result = _dataService.Query<Account>().Where(a => a.Telephone1 == key).OrderBy(a => a.Name).Page(2, 2).ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal(acc3.Id, result[0].Id);
        Assert.Equal(acc4.Id, result[1].Id);
    }

    [Fact]
    public void Query_Page_FirstPage_ReturnsFirstItems()
    {
        var repo = DataService.GetRepository<AccountRepository>();
        var key = Guid.NewGuid().ToString();

        for (int i = 1; i <= 3; i++)
        {
            var acc = repo.GetNewAccount($"{key}-{i:D2}", "Test account", key);
            acc.Id = DataService.CreateTestEntity(acc);
        }

        var result = _dataService.Query<Account>().Where(a => a.Telephone1 == key).OrderBy(a => a.Name).Page(1, 2).ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal($"{key}-01", result[0].Name);
        Assert.Equal($"{key}-02", result[1].Name);
    }

    [Fact]
    public void Query_MultipleWhere_CombinesPredicates()
    {
        var repo = DataService.GetRepository<AccountRepository>();
        var key = Guid.NewGuid().ToString();

        var acc1 = repo.GetNewAccount($"{key}-01", "A", key);
        acc1.Id = DataService.CreateTestEntity(acc1);

        var acc2 = repo.GetNewAccount($"{key}-02", "B", key);
        acc2.Id = DataService.CreateTestEntity(acc2);

        var result = _dataService.Query<Account>().Where(a => a.Telephone1 == key).Where(a => a.Name == $"{key}-02").SingleOrDefault();

        Assert.NotNull(result);
        Assert.Equal(acc2.Id, result.Id);
    }

    [Fact]
    public void Query_ThenBy_ReturnsSecondarySortedResults()
    {
        var repo = DataService.GetRepository<AccountRepository>();
        var key = Guid.NewGuid().ToString();

        var acc2 = repo.GetNewAccount($"{key}-02", "A", key);
        acc2.Id = DataService.CreateTestEntity(acc2);

        var acc1 = repo.GetNewAccount($"{key}-01", "A", key);
        acc1.Id = DataService.CreateTestEntity(acc1);

        var acc4 = repo.GetNewAccount($"{key}-04", "B", key);
        acc4.Id = DataService.CreateTestEntity(acc4);

        var acc3 = repo.GetNewAccount($"{key}-03", "B", key);
        acc3.Id = DataService.CreateTestEntity(acc3);

        var result = _dataService.Query<Account>().Where(a => a.Telephone1 == key).OrderBy(a => a.Description).ThenBy(a => a.Name).ToList();

        Assert.True(result.Count >= 4);
        Assert.Equal(acc1.Id, result[0].Id);
        Assert.Equal(acc2.Id, result[1].Id);
        Assert.Equal(acc3.Id, result[2].Id);
        Assert.Equal(acc4.Id, result[3].Id);
    }

    [Fact]
    public void Query_ThenByDescending_ReturnsSecondarySortedResults()
    {
        var repo = DataService.GetRepository<AccountRepository>();
        var key = Guid.NewGuid().ToString();

        var acc1 = repo.GetNewAccount($"{key}-01", "A", key);
        acc1.Id = DataService.CreateTestEntity(acc1);

        var acc2 = repo.GetNewAccount($"{key}-02", "A", key);
        acc2.Id = DataService.CreateTestEntity(acc2);

        var acc3 = repo.GetNewAccount($"{key}-03", "B", key);
        acc3.Id = DataService.CreateTestEntity(acc3);

        var acc4 = repo.GetNewAccount($"{key}-04", "B", key);
        acc4.Id = DataService.CreateTestEntity(acc4);

        var result = _dataService.Query<Account>().Where(a => a.Telephone1 == key).OrderBy(a => a.Description).ThenByDescending(a => a.Name).ToList();

        Assert.True(result.Count >= 4);
        Assert.Equal(acc2.Id, result[0].Id);
        Assert.Equal(acc1.Id, result[1].Id);
        Assert.Equal(acc4.Id, result[2].Id);
        Assert.Equal(acc3.Id, result[3].Id);
    }

    [Fact]
    public void Query_Page_WithoutExplicitOrderBy_ReturnsRequestedPageSizeWithoutDuplicates()
    {
        var repo = DataService.GetRepository<AccountRepository>();
        var key = Guid.NewGuid().ToString();

        var acc1 = repo.GetNewAccount($"{key}-01", "Test account", key);
        acc1.Id = DataService.CreateTestEntity(acc1);

        var acc2 = repo.GetNewAccount($"{key}-02", "Test account", key);
        acc2.Id = DataService.CreateTestEntity(acc2);

        var acc3 = repo.GetNewAccount($"{key}-03", "Test account", key);
        acc3.Id = DataService.CreateTestEntity(acc3);

        var acc4 = repo.GetNewAccount($"{key}-04", "Test account", key);
        acc4.Id = DataService.CreateTestEntity(acc4);

        var expectedIds = new[] { acc1.Id, acc2.Id, acc3.Id, acc4.Id };

        var result = _dataService.Query<Account>()
            .Where(a => a.Telephone1 == key)
            .Page(2, 2)
            .ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal(2, result.Select(x => x.Id).Distinct().Count());
        Assert.All(result, x => Assert.Contains(x.Id, expectedIds));
    }

    [Fact]
    public void Query_Select_First_ReturnsProjectedValue()
    {
        var repo = DataService.GetRepository<AccountRepository>();
        var key = Guid.NewGuid().ToString();

        var acc2 = repo.GetNewAccount($"{key}-02", "Test account", key);
        acc2.Id = DataService.CreateTestEntity(acc2);

        var acc1 = repo.GetNewAccount($"{key}-01", "Test account", key);
        acc1.Id = DataService.CreateTestEntity(acc1);

        var result = _dataService.Query<Account>().Where(a => a.Telephone1 == key).OrderBy(a => a.Name).Select(a => a.Name).First();

        Assert.Equal(acc1.Name, result);
    }

    [Fact]
    public void Query_Select_SingleOrDefault_ReturnsProjectedValue()
    {
        var repo = DataService.GetRepository<AccountRepository>();
        var key = Guid.NewGuid().ToString();

        var acc = repo.GetNewAccount($"{key}-01", "Test account", key);
        acc.Id = DataService.CreateTestEntity(acc);

        var result = _dataService.Query<Account>().Where(a => a.Id == acc.Id).Select(a => a.Name).SingleOrDefault();

        Assert.Equal(acc.Name, result);
    }

    [Fact]
    public void Query_NullExpressions_ThrowArgumentNullException()
    {
        Expression<Func<Account, bool>>? predicate = null;
        Expression<Func<Account, string>>? orderBy = null;
        Expression<Func<Account, string>>? selector = null;

        Assert.Throws<ArgumentNullException>(() => _dataService.Query<Account>().Where(predicate));
        Assert.Throws<ArgumentNullException>(() => _dataService.Query<Account>().OrderBy(orderBy));
        Assert.Throws<ArgumentNullException>(() => _dataService.Query<Account>().OrderByDescending(orderBy));
        Assert.Throws<ArgumentNullException>(() => _dataService.Query<Account>().ThenBy(orderBy));
        Assert.Throws<ArgumentNullException>(() => _dataService.Query<Account>().ThenByDescending(orderBy));
        Assert.Throws<ArgumentNullException>(() => _dataService.Query<Account>().Select(selector));
    }

    [Fact]
    public void Query_InvalidPagingArguments_ThrowArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _dataService.Query<Account>().Skip(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => _dataService.Query<Account>().Take(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => _dataService.Query<Account>().Page(0, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => _dataService.Query<Account>().Page(1, 0));
    }
}