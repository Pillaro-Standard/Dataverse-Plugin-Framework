# Changelog

All notable changes are documented in this file.

## [1.0.0.2] - 2026-09-10

- Autonumbering configuration lookup ignores deactivated configurations. The
  `pl_AutoNumbering_GetNewNumber` Custom API used to match on the search attributes alone, so a
  deactivated `pl_autonumbering` record still took part and more than one match made the call fail.
- The plugin assembly is registered under a new id, so importing this version replaces the previous
  assembly and plugin type registration rather than updating it in place. Steps registered outside
  this solution against the old plugin type have to be pointed at the new one.


## [1.0.0.1] - 2026-05-06

- Removed `logSeverityError: fatal`
- Add Exectuion = 30 to PluginStage optionset


## [1.0.0.0] - 2026-04-07

- Initial version