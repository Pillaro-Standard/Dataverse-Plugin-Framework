using Microsoft.Xrm.Sdk;
using Pillaro.Dataverse.PluginFramework.Testing.Infrastructure.Dataverse;

namespace Pillaro.Dataverse.PluginFramework.Tests.Data.Repositories;

public class ContactRepository : IAutoRegisteredTestDataRepository
{
    public Contact GetNewContact(string firstName = "Test", string lastName = "Contact", EntityReference? parentCustomer = null, DateTime? birthDate = null)
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
}