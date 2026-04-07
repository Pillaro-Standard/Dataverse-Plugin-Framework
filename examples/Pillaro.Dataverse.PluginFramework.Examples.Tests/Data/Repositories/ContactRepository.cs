using Microsoft.Xrm.Sdk;
using Pillaro.Dataverse.PluginFramework.Examples.Logic;
using Pillaro.Dataverse.PluginFramework.Testing.Infrastructure.Dataverse;

namespace Pillaro.Dataverse.PluginFramework.Examples.Tests.Data.Repositories;

public class ContactRepository : IAutoRegisteredTestDataRepository
{
    public Contact GetNew(string firstName = "Test", string lastName = "Contact", EntityReference? parentCustomer = null, DateTime? birthDate = null)
    {
        var contact = new Contact
        {
            FirstName = firstName,
            LastName = lastName
        };

        if (parentCustomer != null)
            contact.ParentCustomerId = parentCustomer;

        if (birthDate.HasValue)
            contact.BirthDate = birthDate.Value;

        return contact;
    }

    public Contact GetNewWithAddress(
        string firstName = "Test",
        string lastName = "Contact",
        string? addressLine1 = null,
        string? city = null,
        string? postalCode = null,
        string? country = null)
    {
        var contact = GetNew(firstName, lastName);

        if (addressLine1 != null)
            contact.Address1_Line1 = addressLine1;

        if (city != null)
            contact.Address1_City = city;

        if (postalCode != null)
            contact.Address1_PostalCode = postalCode;

        if (country != null)
            contact.Address1_Country = country;

        return contact;
    }
}