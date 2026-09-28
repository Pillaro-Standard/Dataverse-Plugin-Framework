# Changelog

All notable changes are documented in this file.

## [1.0.0.3] - 2026-09-25

- Replaced the Czech strings that sat in the English (1033) labels of the Autonumbering table.
  The forms were named *Informace*, the `pl_Customer` field *Zákazník*, `pl_UseParentConfiguration`
  *Konfigurace brát z nadřazené*, the parent-child relationship *Podřízené konfigurace*, and the two
  default lookup views *Všechny aktivní Autoumbering* — all under language code 1033, the only
  language the solution declares. All ten business rules on the table carried a Czech name and the
  Czech placeholder description.
- Fixed the *Autoumbering* typo, which also appeared in strings that were already English: the view
  names and the `statecode` and `statuscode` descriptions.
- Unchanged: the legacy `NavBarArea` titles on the main form are still Czech under language code
  1029. They belong to the classic web client navigation bar, which the Unified Interface does not
  render.
- Labels only. No change to schema, plugin registrations or runtime behaviour.
- The first 1.0.0.3 zip published for this change could not be imported. The edit had been
  made through a text-mode round trip that added a UTF-8 BOM to every file it touched and
  rewrote CRLF as LF. The business rule `.xaml` files carry no BOM, and the platform's parser
  rejected them: the import stopped at 60% with *Error generating UiData for workflow* on
  `Field "Parent Entity Attribute"` and rolled back. The strings are the same; the edit is now
  made on raw bytes, so the BOM, the line endings and every untouched byte are left alone. The
  version was not bumped, because no environment ever held the broken artifact.
- The zips in this folder are a genuine export from the framework environment, taken after the
  corrected unmanaged solution was imported there. The SolutionPackager repack was only the
  vehicle for carrying the label changes into the environment.


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