Generate integration tests for the selected Task class.

Before generating test code, apply:
- .github/project-setup.md
- .github/instructions/coding-best-practices.instructions.md

Follow these rules strictly:
- Use the reference test implementation defined in .github/project-setup.md (`TestReferenceFile`) as the primary reference
- Generate tests only for the Task class, not for Plugin classes
- Use repository-based test data only
- Obtain the entity repository from DataService
- Create default valid entity via GetNew()
- Modify only the fields relevant to the scenario
- Create Dataverse records via DataService.CreateTestEntity(...)
- Do not manage cleanup manually; TestBase handles cleanup
- Use DataService or QueryService for reads
- Never use mocks
- Follow Arrange / Act / Assert
- Every test must include [Trait("Category", nameof(<TaskName>))]
- Use naming: Should_<Result>_When_<Condition>
- Generated tests must compile with zero warnings/messages (compiler + analyzers)
- Avoid analyzer issues: SYSLIB1045, IDE0057, CA1862, CA1861, CA1822, IDE0028, CA1307, CA1309, IDE0005, IDE0059, IDE0060

Generate only compilable C# test code.
Do not include explanations.