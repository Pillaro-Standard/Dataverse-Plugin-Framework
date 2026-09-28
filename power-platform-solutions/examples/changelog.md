# Changelog

All notable changes to the examples solution are documented in this file.

## [1.0.0.1] - 2026-09-10

- Added the **Pillaro Examples Post Delete Contact** step, which runs the `ArchiveDeletedContact`
  example task and records the deleted contact on its parent account.
- **Pillaro Examples Pre Update Contact** now also filters on `jobtitle`, for the
  `RecordJobTitleChange` example task.
- **Pillaro Examples Post Update Task** carries one Both image instead of a separate pre-image and
  post-image. `SummarySync` reads the same image keys, so example behaviour is unchanged.
- The plugin assembly is registered under a new id, so importing this version replaces the previous
  assembly and plugin type registration rather than updating it in place.


## [1.0.0.0] - 2026-04-07

- Initial release of example solutions
- Task entity implementation with autonumbering support
- ForbiddenWords validator example
- Demonstration of entity-based plugin patterns
