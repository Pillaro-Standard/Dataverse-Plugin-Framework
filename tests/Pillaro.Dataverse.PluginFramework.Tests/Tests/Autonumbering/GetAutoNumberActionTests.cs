using Microsoft.Xrm.Sdk;
using Pillaro.Dataverse.PluginFramework.Testing.Tests;
using Pillaro.Dataverse.PluginFramework.Tests.Data.Repositories;
using System.Text.RegularExpressions;

namespace Pillaro.Dataverse.PluginFramework.Tests.Tests.Autonumbering;

//[CollectionDefinition("NonParallel", DisableParallelization = true)]
public class CrmNonParallelCollectionDefinition { }

//[Collection("NonParallel")]
[Trait("Owner", "JM")]
[Trait("Category", "Autonumbering.GetAutoNumber")]
public class GetAutoNumberActionTests : TestBase
{
    private const string EntityName = "contact";

    public GetAutoNumberActionTests(TestFixture<TestAutofacModule> testFixture) : base(testFixture)
    {
        CleanupAutoNumbering(EntityName);
    }

    private string ExecuteGetAutoNumber(EntityReference entity, Guid? parentEntityId = null, bool includeParentEntityId = true)
    {
        pl_AutoNumbering_GetNewNumberRequest request = new()
        {
            Entity = entity
        };

        if (includeParentEntityId && parentEntityId.HasValue)
            request.ParentEntityId = parentEntityId.Value;

        var response = (pl_AutoNumbering_GetNewNumberResponse)OrganizationService.Execute(request);
        return response.Number;
    }

    private void CleanupAutoNumbering(string entityName)
    {
        var rows = DataService.Query<pl_AutoNumbering>()
            .Where(x => x.pl_EntityName == entityName)
            .OrderByDescending(x => x.CreatedOn)
            .Select(x => new pl_AutoNumbering { pl_AutoNumberingId = x.pl_AutoNumberingId })
            .ToList();

        DataService.Delete(rows);
    }

    private pl_AutoNumbering CreateConfig(
        string entityName,
        int digitCount,
        int currentNumber,
        string format,
        string? date1 = null,
        string? date2 = null,
        string? date3 = null,
        string? parentLookupAttribute = null,
        Guid? parentId = null,
        pl_AutoNumbering? primaryConfig = null,
        bool? useParentConfiguration = null)
    {
        pl_AutoNumbering config = new()
        {
            pl_EntityName = entityName,
            pl_DigitCount = digitCount,
            pl_Number = currentNumber,
            pl_FormatString = format,
            pl_DateFormat1 = date1,
            pl_DateFormat2 = date2,
            pl_DateFormat3 = date3
        };

        if (parentLookupAttribute != null)
            config.pl_ParentLookupAttribute = parentLookupAttribute;

        if (parentId.HasValue)
            config.pl_ParentLookupId = parentId.Value.ToString("D").ToLowerInvariant();

        if (primaryConfig != null)
            config.pl_ParentAutoNumberingId = primaryConfig.ToEntityReference();

        if (useParentConfiguration.HasValue)
            config.pl_UseParentConfiguration = useParentConfiguration.Value
                ? crd8e_pl_autonumbering_pl_useparentconfiguration.Ano
                : crd8e_pl_autonumbering_pl_useparentconfiguration.Ne;

        config.Id = DataService.CreateTestEntity(config);
        return config;
    }

    private Contact CreateContact(string? firstName = "Jan", string lastName = "Mucha", DateTime? birthDate = null, EntityReference? parentCustomer = null)
    {
        var contact = DataService.GetRepository<ContactRepository>().GetNewContact(firstName!, lastName, birthDate: birthDate, parentCustomer: parentCustomer);
        contact.Id = DataService.CreateTestEntity(contact);
        return contact;
    }

    private Account CreateAccount(string name)
    {
        var account = DataService.GetRepository<AccountRepository>().GetNewAccount(name);
        account.Id = DataService.CreateTestEntity(account);
        return account;
    }

    private int GetPrimaryCounterValue()
    {
        return DataService.Query<pl_AutoNumbering>()
            .Where(x => x.pl_EntityName == EntityName && x.pl_ParentAutoNumberingId == null && x.pl_ParentLookupId == null)
            .Select(x => new pl_AutoNumbering { pl_Number = x.pl_Number })
            .First()
            .pl_Number
            .GetValueOrDefault();
    }

    private pl_AutoNumbering GetChildConfig(Guid parentId)
    {
        return DataService.Query<pl_AutoNumbering>()
            .Where(x => x.pl_EntityName == EntityName && x.pl_ParentLookupId == parentId.ToString("D").ToLowerInvariant())
            .Select(x => new pl_AutoNumbering
            {
                pl_Number = x.pl_Number,
                pl_ParentAutoNumberingId = x.pl_ParentAutoNumberingId,
                pl_UseParentConfiguration = x.pl_UseParentConfiguration,
                pl_FormatString = x.pl_FormatString,
                pl_DigitCount = x.pl_DigitCount
            })
            .Single();
    }

    [Fact]
    public void NUM_padding_increments_and_pads()
    {
        CreateConfig(EntityName, 4, 15, "{NUM}");
        var contact = CreateContact();

        var number = ExecuteGetAutoNumber(contact.ToEntityReference());

        Assert.Equal("0016", number);
        Assert.Equal(16, GetPrimaryCounterValue());
    }

    [Fact]
    public void Zero_digit_count_returns_unpadded_number()
    {
        CreateConfig(EntityName, 0, 15, "{NUM}");
        var contact = CreateContact();

        var number = ExecuteGetAutoNumber(contact.ToEntityReference());

        Assert.Equal("16", number);
        Assert.Equal(16, GetPrimaryCounterValue());
    }

    [Fact]
    public void Date1_token_is_replaced_using_configured_format()
    {
        CreateConfig(EntityName, 4, 15, "{date1}-02-{NUM}", date1: "yyyy");
        var contact = CreateContact();
        var before = DateTime.Now;

        var number = ExecuteGetAutoNumber(contact.ToEntityReference());

        var after = DateTime.Now;
        Assert.Contains(number, new[] { $"{before:yyyy}-02-0016", $"{after:yyyy}-02-0016" });
    }

    [Fact]
    public void Date2_token_is_replaced_using_configured_format()
    {
        CreateConfig(EntityName, 4, 15, "{date2}-{NUM}", date2: "MM");
        var contact = CreateContact();
        var before = DateTime.Now;

        var number = ExecuteGetAutoNumber(contact.ToEntityReference());

        var after = DateTime.Now;
        Assert.Contains(number, new[] { $"{before:MM}-0016", $"{after:MM}-0016" });
    }

    [Fact]
    public void Date3_token_is_replaced_using_configured_format()
    {
        CreateConfig(EntityName, 4, 15, "{date3}-{NUM}", date3: "dd");
        var contact = CreateContact();
        var before = DateTime.Now;

        var number = ExecuteGetAutoNumber(contact.ToEntityReference());

        var after = DateTime.Now;
        Assert.Contains(number, new[] { $"{before:dd}-0016", $"{after:dd}-0016" });
    }

    [Fact]
    public void Entity_attribute_is_injected()
    {
        CreateConfig(EntityName, 4, 15, "{firstname}{NUM}");
        var contact = CreateContact("Invoice", "Test");

        var number = ExecuteGetAutoNumber(contact.ToEntityReference());

        Assert.Equal("Invoice0016", number);
    }

    [Fact]
    public void Entity_date_attribute_is_formatted_using_date1()
    {
        CreateConfig(EntityName, 4, 15, "{birthdate:date1}{NUM}", date1: "yyyyMM");
        var contact = CreateContact("Jan", "Date", new DateTime(2024, 05, 24));

        var number = ExecuteGetAutoNumber(contact.ToEntityReference());

        Assert.Equal("2024050016", number);
    }

    [Fact]
    public void Parent_lookup_attribute_is_injected()
    {
        CreateConfig(EntityName, 4, 15, "{parentcustomerid.name}_{NUM}");
        var account = CreateAccount("ACME");
        var contact = CreateContact(parentCustomer: account.ToEntityReference());

        var number = ExecuteGetAutoNumber(contact.ToEntityReference());

        Assert.Equal("ACME_0016", number);
    }

    [Fact]
    public void Token_replacement_is_case_insensitive()
    {
        CreateConfig(EntityName, 3, 6, "{num}-{DATE1}-{FirstName}", date1: "yyyy");
        var contact = CreateContact("JAN", "Case");
        var before = DateTime.Now;

        var number = ExecuteGetAutoNumber(contact.ToEntityReference());

        var after = DateTime.Now;
        Assert.Contains(number, new[] { $"007-{before:yyyy}-JAN", $"007-{after:yyyy}-JAN" });
    }

    [Fact]
    public void Mixed_tokens_are_combined_in_single_format()
    {
        CreateConfig(EntityName, 3, 6, "{date1}-{firstname}-{NUM}", date1: "yyyy");
        var contact = CreateContact("JAN", "Combo");
        var before = DateTime.Now;

        var number = ExecuteGetAutoNumber(contact.ToEntityReference());

        var after = DateTime.Now;
        Assert.Contains(number, new[] { $"{before:yyyy}-JAN-007", $"{after:yyyy}-JAN-007" });
    }

    [Fact]
    public void Parent_series_creates_child_configuration_and_uses_child_counter()
    {
        var account = CreateAccount("Series Parent");
        CreateConfig(EntityName, 3, 0, "{NUM}", parentLookupAttribute: "parentcustomerid");
        var contact = CreateContact(parentCustomer: account.ToEntityReference());

        var number1 = ExecuteGetAutoNumber(contact.ToEntityReference(), account.Id);
        var number2 = ExecuteGetAutoNumber(contact.ToEntityReference(), account.Id);
        var child = GetChildConfig(account.Id);

        Assert.Equal("001", number1);
        Assert.Equal("002", number2);
        Assert.Equal(2, child.pl_Number);
        Assert.NotNull(child.pl_ParentAutoNumberingId);
        Assert.Equal(crd8e_pl_autonumbering_pl_useparentconfiguration.Ano, child.pl_UseParentConfiguration);
    }

    [Fact]
    public void Different_parents_have_independent_counters()
    {
        var account1 = CreateAccount("Parent A");
        var account2 = CreateAccount("Parent B");
        CreateConfig(EntityName, 3, 0, "{NUM}", parentLookupAttribute: "parentcustomerid");

        var contact1 = CreateContact(parentCustomer: account1.ToEntityReference());
        var contact2 = CreateContact(parentCustomer: account2.ToEntityReference());

        var numberA1 = ExecuteGetAutoNumber(contact1.ToEntityReference(), account1.Id);
        var numberA2 = ExecuteGetAutoNumber(contact1.ToEntityReference(), account1.Id);
        var numberB1 = ExecuteGetAutoNumber(contact2.ToEntityReference(), account2.Id);

        Assert.Equal("001", numberA1);
        Assert.Equal("002", numberA2);
        Assert.Equal("001", numberB1);
    }

    [Fact]
    public void Child_configuration_can_override_primary_configuration_when_use_parent_configuration_is_no()
    {
        var account = CreateAccount("Override Parent");
        var primary = CreateConfig(EntityName, 3, 100, "P-{NUM}", parentLookupAttribute: "parentcustomerid", date1: "yyyy");
        CreateConfig(EntityName, 2, 4, "C-{NUM}", parentId: account.Id, primaryConfig: primary, useParentConfiguration: false);

        var contact = CreateContact(parentCustomer: account.ToEntityReference());

        var number = ExecuteGetAutoNumber(contact.ToEntityReference(), account.Id);
        var child = GetChildConfig(account.Id);

        Assert.Equal("C-05", number);
        Assert.Equal(5, child.pl_Number);
    }

    [Fact]
    public void Child_configuration_uses_primary_configuration_when_use_parent_configuration_is_yes()
    {
        var account = DataService.GetRepository<AccountRepository>().GetNewAccount("Inherited Parent");
        account.Id = DataService.CreateTestEntity(account);

        var primary = CreateConfig("contact", 4, 100, "P-{NUM}", parentLookupAttribute: "parentcustomerid");
        CreateConfig("contact", 2, 4, "C-{NUM}", parentId: account.Id, primaryConfig: primary, useParentConfiguration: true);

        var contact = DataService.GetRepository<ContactRepository>().GetNewContact("Jan", "Mucha", parentCustomer: account.ToEntityReference());
        contact.Id = DataService.CreateTestEntity(contact);

        var number = ExecuteGetAutoNumber(contact.ToEntityReference(), account.Id);

        Assert.Equal("P-0005", number);

        var child = DataService.Query<pl_AutoNumbering>()
            .Where(x => x.pl_EntityName == "contact" && x.pl_ParentLookupId == account.Id.ToString("D").ToLowerInvariant())
            .Select(x => new pl_AutoNumbering
            {
                pl_Number = x.pl_Number,
                pl_UseParentConfiguration = x.pl_UseParentConfiguration,
                pl_ParentAutoNumberingId = x.pl_ParentAutoNumberingId
            })
            .Single();

        Assert.Equal(5, child.pl_Number);
        Assert.Equal(crd8e_pl_autonumbering_pl_useparentconfiguration.Ano, child.pl_UseParentConfiguration);
        Assert.NotNull(child.pl_ParentAutoNumberingId);
    }

    [Fact]
    public void Missing_primary_configuration_fails()
    {
        var contact = DataService.GetRepository<ContactRepository>().GetNewContact("Jan", "Mucha");
        contact.Id = DataService.CreateTestEntity(contact);

        var ex = Assert.ThrowsAny<Exception>(() => ExecuteGetAutoNumber(contact.ToEntityReference()));
        var message = ex.ToString();

        Assert.True(
            message.Contains("autonumber", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("numbering", StringComparison.OrdinalIgnoreCase),
            $"Unexpected exception message: {message}");
    }

    [Fact]
    public void Duplicate_primary_configuration_fails()
    {
        CreateConfig(EntityName, 3, 0, "{NUM}");
        CreateConfig(EntityName, 3, 0, "{NUM}");
        var contact = CreateContact();

        var ex = Assert.ThrowsAny<Exception>(() => ExecuteGetAutoNumber(contact.ToEntityReference()));

        Assert.Contains("more than one", ex.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Missing_parent_entity_id_for_parent_series_fails()
    {
        CreateConfig(EntityName, 3, 0, "{NUM}", parentLookupAttribute: "parentcustomerid");
        var contact = CreateContact();

        var ex = Assert.ThrowsAny<Exception>(() => ExecuteGetAutoNumber(contact.ToEntityReference(), includeParentEntityId: false));

        Assert.Contains("parententityid", ex.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Empty_format_string_fails()
    {
        CreateConfig(EntityName, 3, 0, string.Empty);
        var contact = CreateContact();

        var ex = Assert.ThrowsAny<Exception>(() => ExecuteGetAutoNumber(contact.ToEntityReference()));

        Assert.Contains("format", ex.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Missing_date_format_for_used_date_token_fails()
    {
        CreateConfig("contact", 3, 0, "{date1}-{NUM}");
        var contact = DataService.GetRepository<ContactRepository>().GetNewContact("Jan", "Mucha");
        contact.Id = DataService.CreateTestEntity(contact);

        var ex = Assert.ThrowsAny<Exception>(() => ExecuteGetAutoNumber(contact.ToEntityReference()));
        var message = ex.ToString();

        Assert.True(
      message.Contains("corresponding date format", StringComparison.OrdinalIgnoreCase) ||
      message.Contains("date format", StringComparison.OrdinalIgnoreCase),
      $"Unexpected exception message: {message}");
    }

    [Fact]
    public void Missing_entity_attribute_fails()
    {
        CreateConfig(EntityName, 3, 0, "{firstname}{NUM}");
        var contact = CreateContact(null!, "NoFirstName");

        var ex = Assert.ThrowsAny<Exception>(() => ExecuteGetAutoNumber(contact.ToEntityReference()));

        Assert.Contains("firstname", ex.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Missing_parent_lookup_value_fails()
    {
        CreateConfig(EntityName, 3, 0, "{parentcustomerid.name}-{NUM}");
        var contact = CreateContact();

        var ex = Assert.ThrowsAny<Exception>(() => ExecuteGetAutoNumber(contact.ToEntityReference()));

        Assert.Contains("parentcustomerid", ex.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Missing_parent_attribute_fails()
    {
        CreateConfig(EntityName, 3, 0, "{parentcustomerid.accountnumber}-{NUM}");
        var account = CreateAccount("NoAccountNumber");
        var contact = CreateContact(parentCustomer: account.ToEntityReference());

        var ex = Assert.ThrowsAny<Exception>(() => ExecuteGetAutoNumber(contact.ToEntityReference()));

        Assert.Contains("accountnumber", ex.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Non_date_value_with_date_format_key_fails()
    {
        CreateConfig(EntityName, 3, 0, "{firstname:date1}-{NUM}", date1: "yyyy");
        var contact = CreateContact("Jan", "WrongType");

        var ex = Assert.ThrowsAny<Exception>(() => ExecuteGetAutoNumber(contact.ToEntityReference()));

        Assert.Contains("datetime", ex.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Concurrent_calls_return_unique_numbers_and_increment_counter()
    {
        CreateConfig(EntityName, 3, 0, "{NUM}");
        var contact = CreateContact("Jan", "Concurrency");

        var tasks = Enumerable.Range(0, 5)
            .Select(_ => Task.Run(() => ExecuteGetAutoNumber(contact.ToEntityReference())))
            .ToArray();

        await Task.WhenAll(tasks);

        var results = tasks.Select(x => x.Result).ToList();

        Assert.Equal(5, results.Count);
        Assert.Equal(5, results.Distinct().Count());
        Assert.All(results, x => Assert.Matches(@"^\d{3}$", x));
        Assert.Equal(new[] { "001", "002", "003", "004", "005" }, results.OrderBy(x => x).ToArray());
        Assert.Equal(5, GetPrimaryCounterValue());
    }

    [Fact]
    public async Task Concurrent_calls_for_same_parent_series_return_unique_numbers_and_increment_single_child_counter()
    {
        var account = CreateAccount("Concurrent Parent");
        CreateConfig(EntityName, 3, 0, "{NUM}", parentLookupAttribute: "parentcustomerid");
        var contact = CreateContact(parentCustomer: account.ToEntityReference());

        var tasks = Enumerable.Range(0, 5)
            .Select(_ => Task.Run(() => ExecuteGetAutoNumber(contact.ToEntityReference(), account.Id)))
            .ToArray();

        await Task.WhenAll(tasks);

        var results = tasks.Select(x => x.Result).ToList();
        var child = GetChildConfig(account.Id);

        Assert.Equal(5, results.Distinct().Count());
        Assert.Equal(new[] { "001", "002", "003", "004", "005" }, results.OrderBy(x => x).ToArray());
        Assert.Equal(5, child.pl_Number);
    }

    [Fact]
    public async Task Concurrent_calls_for_different_parent_series_keep_counters_isolated()
    {
        var account1 = CreateAccount("Concurrent Parent A");
        var account2 = CreateAccount("Concurrent Parent B");
        CreateConfig(EntityName, 3, 0, "{NUM}", parentLookupAttribute: "parentcustomerid");

        var contact1 = CreateContact(parentCustomer: account1.ToEntityReference());
        var contact2 = CreateContact(parentCustomer: account2.ToEntityReference());

        var tasks = Enumerable.Range(0, 3)
            .SelectMany(_ => new[]
            {
                Task.Run(() => ExecuteGetAutoNumber(contact1.ToEntityReference(), account1.Id)),
                Task.Run(() => ExecuteGetAutoNumber(contact2.ToEntityReference(), account2.Id))
            })
            .ToArray();

        await Task.WhenAll(tasks);

        var parentAResults = tasks.Where((_, index) => index % 2 == 0).Select(x => x.Result).OrderBy(x => x).ToArray();
        var parentBResults = tasks.Where((_, index) => index % 2 == 1).Select(x => x.Result).OrderBy(x => x).ToArray();

        Assert.Equal(new[] { "001", "002", "003" }, parentAResults);
        Assert.Equal(new[] { "001", "002", "003" }, parentBResults);
        Assert.Equal(3, GetChildConfig(account1.Id).pl_Number);
        Assert.Equal(3, GetChildConfig(account2.Id).pl_Number);
    }

    [Fact]
    public void Generated_number_matches_expected_pattern_for_complex_format()
    {
        CreateConfig(EntityName, 4, 27, "{date1}-{firstname}-{birthdate:date2}-{NUM}", date1: "yyyy", date2: "MM");
        var contact = CreateContact("JAN", "Pattern", new DateTime(2024, 05, 24));

        var number = ExecuteGetAutoNumber(contact.ToEntityReference());

        Assert.True(Regex.IsMatch(number, @"^\d{4}-JAN-\d{2}-0028$"), $"Unexpected generated number '{number}'.");
    }
}
