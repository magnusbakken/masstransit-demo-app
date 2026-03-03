We will make the following changes:
- Switch from .sln to .slnx.
- Some redundant properties should be removed from csproj files, since they're specified in Directory.Build.props.
- We'll switch from XUnit to TUnit. Update the Cursor rules as well.
- We'll also switch from Moq to FakeItEasy. Add this decision to the Cursor rules too.