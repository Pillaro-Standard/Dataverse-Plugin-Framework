# Examples — Purpose and test instructions

The `examples` folder contains sample implementations included in this repository. These examples are provided to help you quickly test and validate the framework behavior and to demonstrate recommended patterns.

## Required runtime setting for examples

- **ForbiddenWords** (JSON) = `["Admin","Test"]` — used by example validators to block disallowed values.

## Required Autonumberings configuration

The examples require a specific record in the **Autonumberings** entity (custom entity provided by the framework). Create the following record:

| Field | Value |
|-------|-------|
| **Entity System Name** | Task |
| **Last Used Number {NUM}** | 1000 |
| **Number of Digits** | 6 |
| **Format** | `{date1}-{NUM}` |
| **Date 1 Format {date1}** | yy-MM-dd |

This configuration enables automatic number generation for Task records in the examples with format like `26-04-07-001000`.

## Quick test steps

1. Deploy the framework to Dataverse [see](../framework/README.md).
2. Configure runtime setting `ForbiddenWords` with the JSON value `["Admin","Test"]`.
3. Create the required `Autonumberings` record as shown in the table above.
4. Deploy or register the example solutions/plugins from the `examples` folder.
5. Run the provided test scenarios or perform the expected actions manually (e.g., create/update Task records).

Note: Examples are intended for demonstration and testing only — they are not production-ready.