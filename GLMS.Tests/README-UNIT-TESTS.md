# GLMS unit testing guide

The solution includes a separate `GLMS.Tests` xUnit project. It references `GLMS.Web` and uses EF Core InMemory for isolated service tests and Moq to mock external dependencies.

## Run the tests

From the folder containing `GLMS.sln`:

```bash
dotnet restore
dotnet test GLMS.sln --configuration Release
```

For a detailed list of test names and pass/fail results:

```bash
dotnet test GLMS.Tests/GLMS.Tests.csproj --logger "console;verbosity=detailed"
```

In Visual Studio:
1. Open `GLMS.sln`.
2. Select **Build > Build Solution**.
3. Open **Test > Test Explorer**.
4. Select **Run All Tests**.
5. Capture a screenshot with all tests passing for submission evidence.

## Coverage areas

- Currency conversion arithmetic, rounding, invalid amounts/rates, live API response parsing and fallback behaviour.
- Signed agreement file validation, including extension case-insensitivity, forbidden extensions, spoofed `.pdf` content, empty files, file saving and path traversal.
- Contract status/date workflow enforcement and prevention of writes for blocked requests.
- Contract date/status search and overdue contract expiration.
- Model validation for required client fields, contract date ordering and service-request cost ranges.
- Service-request persistence, calculated ZAR values, navigation loading, and missing-contract handling.

The tests mock HTTP and file dependencies; they do not require a live currency API or SQL Server. The EF Core InMemory provider is for unit tests only and does not replace SQL Server integration testing.
