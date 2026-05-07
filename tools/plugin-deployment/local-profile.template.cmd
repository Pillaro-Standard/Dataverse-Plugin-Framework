@echo off
rem Copy this file to:
rem   %USERPROFILE%\.pillaro\dataverse-plugin-framework\profiles\default.cmd
rem or:
rem   %USERPROFILE%\.pillaro\dataverse-plugin-framework\profiles\<profile-name>.cmd
rem
rem Never commit real credentials to the repository.

set "PILLARO_DV_URL=https://your-dev-org.crm4.dynamics.com"

rem Supported values: ClientSecret, ConnectionString, Interactive.
set "PILLARO_DV_AUTH_TYPE=ClientSecret"

rem ClientSecret authentication.
set "PILLARO_DV_TENANT_ID=00000000-0000-0000-0000-000000000000"
set "PILLARO_DV_CLIENT_ID=00000000-0000-0000-0000-000000000000"
set "PILLARO_DV_CLIENT_SECRET=replace-with-local-secret"

rem ConnectionString authentication.
rem set "PILLARO_DV_AUTH_TYPE=ConnectionString"
rem set "PILLARO_DV_CONNECTION_STRING=AuthType=ClientSecret;Url=https://your-dev-org.crm4.dynamics.com;ClientId=00000000-0000-0000-0000-000000000000;ClientSecret=replace-with-local-secret;TenantId=00000000-0000-0000-0000-000000000000"

rem Interactive authentication for developer scenarios.
rem set "PILLARO_DV_AUTH_TYPE=Interactive"
