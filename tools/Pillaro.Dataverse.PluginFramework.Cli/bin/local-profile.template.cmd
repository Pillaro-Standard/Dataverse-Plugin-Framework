@echo off

rem Copy this file to:
rem   %USERPROFILE%\.dv\profiles\examples-dev.cmd
rem
rem Keep this file OUTSIDE the repository with real secrets.

set "DV_CONN=AuthType=ClientSecret;Url=https://org.crm4.dynamics.com;ClientId=...;ClientSecret=...;TenantId=..."
set "DV_PAC=examples-dev"
set "DV_PLUGIN_ID="

rem Set to 1 when you want to test only step/image metadata and skip pac plugin push.
set "DV_SKIP_PAC_PUSH=1"
