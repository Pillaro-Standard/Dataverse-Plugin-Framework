# Plugin Step Configuration

> [!IMPORTANT]
> Plugin steps can be configured with unsecure configuration values. Secure configuration must be set manually via Plugin Registration Tool.

---

## 📑 Navigation

- [🔍 What is plugin step configuration](#-what-is-plugin-step-configuration)
- [🔓 Unsecure Configuration](#-unsecure-configuration)
- [🔐 Secure Configuration](#-secure-configuration)
- [⚠️ Security Best Practices](#️-security-best-practices)
- [💻 Examples](#-examples)
- [✅ Recommendations](#-recommendations)

---

## 🔍 What is plugin step configuration

Plugin step configuration allows you to pass configuration values to individual plugin steps at registration time. These values are accessible in your tasks through the `TaskContext`.

There are two types of configuration:

- **Unsecure Configuration** - stored in plain text, can be set via code
- **Secure Configuration** - stored encrypted, **must be set manually via Plugin Registration Tool**

---

## 🔓 Unsecure Configuration

Unsecure configuration is suitable for non-sensitive values such as:

- Feature flags
- Timeout values
- Default behavior settings
- Public API endpoints
- Display preferences

### Setting unsecure configuration

~~~csharp
public override void Register(IPluginRegistration registration)
{
    registration
        .OnCreate<Contact>("00000000-0000-0000-0000-000000000000")
        .PreOperation()
        .Synchronous()
        .WithName("My Plugin Step")
        .WithUnsecureConfiguration("timeout=30;retryCount=3")
        .Rank(1);
}
~~~

### Accessing unsecure configuration

~~~csharp
public class MyTask : TaskBase<Contact>
{
    protected override void DoExecute()
    {
        string config = TaskContext.UnsecureConfig; // "timeout=30;retryCount=3"

        // Parse configuration
        var settings = ParseConfig(config);
        int timeout = int.Parse(settings["timeout"]);
    }
}
~~~

---

## 🔐 Secure Configuration

> [!CAUTION]
> Secure configuration **CANNOT** be set via code. It must be configured manually using Plugin Registration Tool.

Secure configuration should be used for sensitive values such as:

- API keys
- Passwords
- Connection strings
- Authentication tokens
- Encryption keys

### Why secure configuration cannot be set in code

If secure configuration were set in code:

1. Sensitive values would be committed to Git
2. Secrets would be visible in manifest files
3. Credentials would appear in deployment logs
4. Security would be compromised

### ✅ How to set secure configuration

**Step 1: Deploy your plugin without secure configuration**

~~~csharp
public override void Register(IPluginRegistration registration)
{
    registration
        .OnCreate<Lead>("00000000-0000-0000-0000-000000000000")
        .PostOperation()
        .Asynchronous()
        .WithName("Lead Integration")
        .WithUnsecureConfiguration("apiEndpoint=https://api.example.com/leads")
        // Secure config must be set manually via Plugin Registration Tool
        .Rank(1);
}
~~~

**Step 2: Use Plugin Registration Tool to set secure configuration**

1. Open Plugin Registration Tool
2. Connect to your Dataverse environment
3. Find your plugin step
4. Right-click → **Update**
5. In the **Secure Configuration** field, enter your sensitive value
6. Click **Update**

> [!NOTE]
> Secure configuration is stored encrypted in Dataverse and can only be viewed/modified by users with appropriate permissions.

**Step 3: Access secure configuration in your task**

~~~csharp
public class LeadIntegration : TaskBase<Lead>
{
    protected override void DoExecute()
    {
        string endpoint = ExtractValue(TaskContext.UnsecureConfig, "apiEndpoint");
        string apiKey = TaskContext.SecureConfig; // Set manually in Plugin Registration Tool

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidPluginExecutionException(
                "Secure configuration (API key) is not configured. " +
                "Please set it via Plugin Registration Tool.");
        }

        CallExternalApi(endpoint, apiKey, ContextEntity);
    }
}
~~~

---

## ⚠️ Security Best Practices

### DO ✅

- Use unsecure configuration for non-sensitive values
- Use secure configuration for all sensitive values
- Set secure configuration manually in each environment via Plugin Registration Tool
- Store sensitive values in Azure Key Vault or similar secure storage for reference
- Rotate sensitive credentials regularly
- Document what secure configuration values are needed for deployment
- Use different credentials for different environments (dev/test/prod)
- Validate that secure configuration is present before using it

### DON'T ❌

- Store sensitive values in source code
- Commit credentials to Git
- Share credentials via manifest files or documentation
- Use the same credentials across all environments
- Store production credentials in development environments
- Log or display secure configuration values
- Try to set secure configuration programmatically (it's not supported)

---

## 💻 Examples

The examples above use `Guid.Empty` placeholders intentionally. Replace them with real non-empty Dataverse step and image IDs before running deployment validation.

### Example 1: Feature flag with unsecure config

~~~csharp
public override void Register(IPluginRegistration registration)
{
    registration
        .OnUpdate<Account>("00000000-0000-0000-0000-000000000000")
        .PreOperation()
        .Synchronous()
        .WithName("Account Validation")
        .WithUnsecureConfiguration("enableNewValidation=true;minValue=100")
        .WhenChanged("revenue")
        .Rank(1);
}
~~~

~~~csharp
public class AccountValidation : TaskBase<Account>
{
    protected override void DoExecute()
    {
        var config = ParseConfig(TaskContext.UnsecureConfig);

        if (config.GetValueOrDefault("enableNewValidation") == "true")
        {
            int minValue = int.Parse(config.GetValueOrDefault("minValue", "0"));
            ValidateRevenue(ContextEntity.Revenue, minValue);
        }
    }

    private Dictionary<string, string> ParseConfig(string config)
    {
        return config?.Split(';')
            .Select(s => s.Split('='))
            .Where(parts => parts.Length == 2)
            .ToDictionary(parts => parts[0], parts => parts[1])
            ?? new Dictionary<string, string>();
    }
}
~~~

### Example 2: External API integration with secure config

~~~csharp
// In plugin registration code
public override void Register(IPluginRegistration registration)
{
    registration
        .OnCreate<Lead>("00000000-0000-0000-0000-000000000000")
        .PostOperation()
        .Asynchronous()
        .WithName("Lead Integration")
        .WithUnsecureConfiguration("apiEndpoint=https://api.example.com/leads;timeout=30")
        // Secure API key must be set via Plugin Registration Tool
        .Rank(1);
}
~~~

~~~csharp
public class LeadIntegration : TaskBase<Lead>
{
    protected override void DoExecute()
    {
        var config = ParseConfig(TaskContext.UnsecureConfig);
        string endpoint = config.GetValueOrDefault("apiEndpoint");
        int timeout = int.Parse(config.GetValueOrDefault("timeout", "30"));
        string apiKey = TaskContext.SecureConfig;

        if (string.IsNullOrWhiteSpace(endpoint))
        {
            throw new InvalidPluginExecutionException("API endpoint not configured.");
        }

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidPluginExecutionException(
                "Secure configuration (API key) is not set. " +
                "Please configure it via Plugin Registration Tool.");
        }

        CallExternalApi(endpoint, apiKey, timeout, ContextEntity);
    }
}
~~~

### Example 3: Deployment checklist

After deploying your plugin, create a checklist for configuring secure values:

**Deployment Checklist:**

| Step | Environment | Value | Status |
|------|-------------|-------|--------|
| Lead Integration - API Key | DEV | Set via PRT | ☐ |
| Lead Integration - API Key | TEST | Set via PRT | ☐ |
| Lead Integration - API Key | PROD | Set via PRT | ☐ |

> [!TIP]
> Keep your deployment checklist in a secure location (Azure DevOps Wiki, Confluence, etc.)
> **NEVER** include actual secret values in the checklist!

---

## ✅ Recommendations

1. **Use Framework Settings for complex configuration**
   - For complex or frequently changing configuration, use the framework's `SettingsService` instead
   - See [configuration.md](configuration.md) for details
   - Framework settings can be changed without redeployment

2. **Document configuration requirements**
   - Create a deployment guide listing all required configuration values
   - Specify which values are sensitive (secure config)
   - Provide instructions for setting secure configuration via Plugin Registration Tool

3. **Validate configuration presence**
   - Always check if required configuration is present before using it
   - Throw meaningful exceptions if configuration is missing
   - This helps catch configuration issues early

4. **Use structured formats**
   - Consider JSON for complex unsecure configuration values
   - For simple values, use `key=value;key2=value2` format
   - Make parsing robust and handle missing keys gracefully

5. **Test without sensitive values**
   - Use fake/test values in development environments
   - Ensure code validates and handles missing configuration gracefully
   - Never use production credentials in non-production environments

6. **Automate where possible**
   - Consider using PowerShell scripts or Azure DevOps pipelines to document required configuration
   - Use environment variables to manage non-sensitive configuration
   - Keep sensitive configuration management manual for security

---

## ➡️ Related documents

- [Plugin Registration API](./plugin-registration-api.md) - Configure plugin step metadata in code.
- [Deployment Plugins](./deployment-plugins.md) - Deploy plugin assemblies and synchronized plugin steps.
- [Configuration](configuration.md) - Framework runtime settings (alternative to plugin configuration)
- [Getting Started](getting-started.md) - Creating your first plugin
- [Task Model](task-model.md) - Understanding task context and accessing configuration
