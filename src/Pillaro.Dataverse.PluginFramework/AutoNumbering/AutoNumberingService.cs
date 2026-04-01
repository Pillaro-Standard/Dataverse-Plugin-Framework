using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Pillaro.Dataverse.PluginFramework.Data;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Pillaro.Dataverse.PluginFramework.AutoNumbering;

public class AutoNumberingService
{
    private readonly IOrganizationService _organizationService;
    private readonly int _retryAttempts;

    public AutoNumberingService(IOrganizationService organizationService, int retryAttempts = 5)
    {
        _organizationService = organizationService;
        _retryAttempts = retryAttempts;
    }

    public virtual string GetAutoNumber(string entityName, Guid entityId, Guid? parentEntityId)
    {
        if (string.IsNullOrEmpty(entityName))
            throw new ArgumentNullException(nameof(entityName));

        if (entityId == Guid.Empty)
            throw new ArgumentNullException(nameof(entityId));

        OrganizationRequest req = new("pl_GetAutoNumber");
        req["EntityName"] = entityName;
        req["ParentEntityId"] = $"{parentEntityId}";
        req["EntityId"] = $"{entityId}";
        req["RetryAttempts"] = _retryAttempts;

        var response = _organizationService.Execute(req);

        if (string.IsNullOrEmpty($"{response?["Number"]}"))
            throw new InvalidPluginExecutionException("Response.Number cannot be null or empty");

        Debug.WriteLine($"Autonumbering response number {response["Number"]}");

        return response["Number"].ToString();
    }

    /// <summary>
    /// Generates and returns the next auto-generated transaction number for the specified entity, based on
    /// configured numbering rules and optional grouping or parent entity context.   
    /// </summary>
    /// <remarks>The generated transaction number is formatted according to the entity's auto-numbering
    /// configuration, which may include dynamic fields, date components, and grouping values. If a specific
    /// configuration for the provided parent entity or grouping value does not exist, it is created automatically.
    /// The method also returns an UpdateRequest that should be executed to persist the incremented sequence number
    /// with concurrency control.</remarks>
    /// <param name="entityName">The logical name of the entity for which to generate the transaction number. Cannot be null or empty.</param>
    /// <param name="entityId">The unique identifier of the entity record for which the transaction number is being generated. Must not be
    /// Guid.Empty.</param>
    /// <param name="parentEntityId">The unique identifier of the parent entity, if the numbering configuration requires a parent context.
    /// Required if the entity's auto-numbering is configured to use a parent lookup; otherwise, can be null.</param>
    /// <param name="groupingValue">An optional value used to group numbering sequences, such as a year or other categorization. If provided and
    /// supported by the configuration, a separate sequence is maintained for each grouping value.</param>
    /// <returns>An AutoNumberingTransactionalResponse containing the generated transaction number and an update request to
    /// persist the new sequence state.</returns>
    /// <exception cref="ArgumentNullException">Thrown if entityName is null or empty, or if entityId is Guid.Empty.</exception>
    /// <exception cref="InvalidPluginExecutionException">Thrown if the required auto-numbering configuration does not exist for the specified entity, if a required
    /// parent entity ID or attribute is missing, or if the entity record is not found when attribute-based
    /// formatting is used.</exception>
    public virtual AutoNumberingResponse GetTransactionAutoNumber(string entityName, Guid entityId, Guid? parentEntityId, string groupingValue)
    {
        if (string.IsNullOrEmpty(entityName))
            throw new ArgumentNullException(nameof(entityName));

        if (entityId == Guid.Empty)
            throw new ArgumentNullException(nameof(entityId));

        DataService dataService = new(_organizationService);

        var autoNumName = "pl_autonumbering";

        // Find the primary configuration for the given entity  
        var primaryAutoNum = dataService.LoadRecord(autoNumName, new string[] { "pl_entityname", "pl_parentautonumberingid", "pl_parentlookupid" }, new object[] { entityName, null, null });


        if (primaryAutoNum == null)
            throw new InvalidPluginExecutionException($"Primary autonumbering configuration does not exist for entity '{entityName}'.");

        //currentAutoNum - odkud se bere aktualni cislo
        var currentAutoNum = primaryAutoNum;

        if (primaryAutoNum.Contains("pl_parentlookupattribute") && primaryAutoNum["pl_parentlookupattribute"] != null)
        {
            if (parentEntityId == null)
                throw new InvalidPluginExecutionException($"Attribute '{primaryAutoNum["pl_parentlookupattribute"]}' is required for entity '{entityName}'.");

            //parentEntityId = parentEntityId.Replace("{", "").Replace("}", "").ToLower();

            currentAutoNum = dataService.LoadRecord(autoNumName, new string[] { "pl_entityname", "pl_parentlookupid" }, new object[] { entityName, parentEntityId });
            if (currentAutoNum == null)
            {
                //pokud neni pro dane parent entity id jeste konfigurace, tak ji vytvorime
                currentAutoNum = new Entity(autoNumName);
                currentAutoNum["pl_entityname"] = entityName;
                currentAutoNum["pl_parentlookupid"] = parentEntityId;
                currentAutoNum["pl_number"] = 0;
                currentAutoNum["pl_parentautonumberingid"] = primaryAutoNum.ToEntityReference();
                currentAutoNum["pl_useparentconfiguration"] = new OptionSetValue((int)UseParentConfiguration.Ano);

                currentAutoNum.Id = _organizationService.Create(currentAutoNum);

                currentAutoNum = _organizationService.Retrieve(autoNumName, currentAutoNum.Id, new Microsoft.Xrm.Sdk.Query.ColumnSet(true));
            }
        }
        //existuje hodnota, pres kterou se to ma seskupovat, napr. rok
        else if (!string.IsNullOrEmpty(groupingValue))
        {
            currentAutoNum = dataService.LoadRecord(autoNumName, new string[] { "pl_entityname", "pl_groupingvalue" }, new object[] { entityName, groupingValue });
            if (currentAutoNum == null)
            {
                //pokud neni pro dane parent entity id jeste konfigurace, tak ji vytvorime
                currentAutoNum = new Entity(autoNumName);
                currentAutoNum["pl_entityname"] = entityName;
                currentAutoNum["pl_groupingvalue"] = groupingValue;
                currentAutoNum["pl_number"] = 0;
                currentAutoNum["pl_parentautonumberingid"] = primaryAutoNum.ToEntityReference();
                currentAutoNum["pl_useparentconfiguration"] = new OptionSetValue((int)UseParentConfiguration.Ano);

                currentAutoNum.Id = _organizationService.Create(currentAutoNum);

                currentAutoNum = _organizationService.Retrieve(autoNumName, currentAutoNum.Id, new Microsoft.Xrm.Sdk.Query.ColumnSet(true));
            }
        }

        //aktualni cislo
        var currentNumber = 0;
        if (currentAutoNum.Contains("pl_number") && currentAutoNum["pl_number"] != null)
            currentNumber = (int)currentAutoNum["pl_number"];
        currentNumber++;

        //zjisti se, odkud se bude brat konfigurace
        var configAutoNum = (currentAutoNum.Contains("pl_useparentconfiguration") && currentAutoNum["pl_useparentconfiguration"] != null && ((OptionSetValue)currentAutoNum["pl_useparentconfiguration"]).Value == (int)UseParentConfiguration.Ano ?
                                primaryAutoNum : currentAutoNum);

        var digitCount = (configAutoNum.Contains("pl_digitcount") && configAutoNum["pl_digitcount"] != null ? (int)configAutoNum["pl_digitcount"] : 0);
        var number = Regex.Replace(configAutoNum["pl_formatstring"].ToString(), "{NUM}", currentNumber.ToString().PadLeft(digitCount, '0'), RegexOptions.IgnoreCase);

        //aktualizace datumovych poli
        if (number.IndexOf("{date1}", StringComparison.OrdinalIgnoreCase) >= 0)
            number = Regex.Replace(number, "{date1}", DateTime.Now.ToString(configAutoNum["pl_dateformat1"].ToString()), RegexOptions.IgnoreCase);

        if (number.IndexOf("{date2}", StringComparison.OrdinalIgnoreCase) >= 0)
            number = Regex.Replace(number, "{date2}", DateTime.Now.ToString(configAutoNum["pl_dateformat2"].ToString()), RegexOptions.IgnoreCase);

        if (number.IndexOf("{date3}", StringComparison.OrdinalIgnoreCase) >= 0)
            number = Regex.Replace(number, "{date3}", DateTime.Now.ToString(configAutoNum["pl_dateformat3"].ToString()), RegexOptions.IgnoreCase);

        if (number.IndexOf("{grouping}", StringComparison.OrdinalIgnoreCase) >= 0)
            number = Regex.Replace(number, "{grouping}", groupingValue, RegexOptions.IgnoreCase);

        //aktualizace atributu
        var attribs = GetDynamicFields(number);
        if (attribs.Length > 0)
        {
            var entity = _organizationService.Retrieve(entityName, entityId, new Microsoft.Xrm.Sdk.Query.ColumnSet(true));

            if (entity == null)
                throw new InvalidPluginExecutionException("Formát automatického čísla obsahuje atributy z entity. Záznam musí být uložený.");

            foreach (var item in attribs)
            {
                //dany atribut je v aktualnim zaznamu
                var attrib = item;
                var attribValue = entity[attrib.ToLower()].ToString();
                var dateFormat = string.Empty;

                if (attrib.Contains(":"))
                {
                    dateFormat = attrib.Split(':')[1];
                    attrib = attrib.Split(':')[0];
                }

                if (dateFormat != string.Empty)
                    attribValue = ((DateTime)entity[attrib.ToLower()]).ToString(GetDateFormatString(dateFormat, configAutoNum));

                if (item.Contains(".")) //jde o nadrizenou entitu
                {
                    var parentEntityLookup = item.Split('.')[0];
                    var parentEntityAttrib = item.Split('.')[1];
                    dateFormat = string.Empty;

                    if (parentEntityAttrib.Contains(":"))
                    {
                        dateFormat = parentEntityAttrib.Split(':')[1];
                        parentEntityAttrib = parentEntityAttrib.Split(':')[0];
                    }

                    EntityReference parentEntityLookuAttribute = (EntityReference)entity[parentEntityLookup.ToLower()];

                    if (parentEntityLookuAttribute == null)
                        throw new InvalidPluginExecutionException($"Attribut {parentEntityLookup} není vyplněný! Nelze generovat automatické číslo.");

                    var parentEntity = _organizationService.Retrieve(parentEntityLookuAttribute.LogicalName, parentEntityLookuAttribute.Id, new Microsoft.Xrm.Sdk.Query.ColumnSet(parentEntityAttrib.ToLower()));
                    attribValue = parentEntity[parentEntityAttrib.ToLower()].ToString();

                    if (dateFormat != string.Empty)
                        attribValue = ((DateTime)parentEntity[parentEntityAttrib.ToLower()]).ToString(GetDateFormatString(dateFormat, configAutoNum));
                }

                number = Regex.Replace(number, "{" + item + "}", attribValue, RegexOptions.IgnoreCase);
            }
        }


        Entity updateEntity = new("pl_autonumbering");
        updateEntity.Id = currentAutoNum.Id;
        updateEntity["pl_number"] = currentNumber;
        updateEntity.RowVersion = currentAutoNum.RowVersion;


        AutoNumberingResponse response = new()
        {
            Number = number,
            Request = new UpdateRequest
            {
                Target = updateEntity,
                ConcurrencyBehavior = ConcurrencyBehavior.IfRowVersionMatches
            }
        };

        return response;
    }

    public string[] GetDynamicFields(string format)
    {
        var pattern = "(?<={).+?(?=})";
        List<string> items = [];
        var cols = Regex.Matches(format, pattern);
        foreach (Match item in cols)
        {
            items.Add(item.Groups[0].Value);
        }

        return items.ToArray();

    }

    private string GetDateFormatString(string dateFormat, Entity autoNumbering)
    {
        var format = string.Empty;
        switch (dateFormat.ToLower())
        {
            case "date1":
                format = autoNumbering["pl_dateformat1"]?.ToString();
                break;
            case "date2":
                format = autoNumbering["pl_dateformat2"]?.ToString();
                break;
            case "date3":
                format = autoNumbering["pl_dateformat3"]?.ToString();
                break;
        }
        return format;
    }
}
