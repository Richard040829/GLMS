# TechMove Global Logistics Management System (GLMS)

ASP.NET Core MVC / .NET 8 monolithic prototype using Entity Framework Core and SQL Server LocalDB.

## Features

- Dashboard home page with client, contract, status and service-request summaries.
- Client, Contract and ServiceRequest EF Core entities with validation and one-to-many relationships.
- Contract search by start/end date and status; overdue active contracts are automatically marked Expired when the dashboard or contract list is opened.
- Signed agreement upload and download. Uploads are restricted to PDF files (case-insensitive extension, 10 MB maximum, PDF signature validation), stored under `wwwroot/uploads/contracts` using generated names.
- Central `ContractWorkflowService` prevents new service requests unless the parent contract is Active, within its date range, and not expired/on hold.
- `ExchangeRateService` fetches USD/ZAR rates from the free `open.er-api.com` endpoint and uses a configured fallback if the API fails.
- `CurrencyCalculator` separates currency arithmetic from HTTP integration.
- xUnit tests cover currency calculations/API fallback, PDF validation/storage, contract filtering/expiry, and workflow enforcement.

## Run the web application

1. Install the .NET 8 SDK and SQL Server LocalDB (or update the connection string for your SQL Server instance).
2. Open `GLMS.sln` in Visual Studio or open the `GLMS` folder in VS Code.
3. Check `GLMS.Web/appsettings.json` and confirm `ConnectionStrings:DefaultConnection` points to your SQL Server instance.
4. In a terminal, change to `GLMS.Web` and run:

   ```powershell
   dotnet restore
   dotnet ef migrations list
   dotnet ef database update
   dotnet run
   ```

   If this project has no migrations yet, create the initial migration once before `database update`:

   ```powershell
   dotnet ef migrations add InitialCreate
   dotnet ef database update
   ```

5. Open the local URL printed by `dotnet run`. The dashboard is the default home page.

The database context includes deterministic sample clients, contracts and a service request. If your database has already been created and seeded, do not create a duplicate `InitialCreate` migration.

## Run automated tests

From the solution root:

```powershell
dotnet test GLMS.sln
```

To see individual tests in Visual Studio, open **Test > Test Explorer**, build the solution, and select **Run All Tests**.

## Notes

- The ZAR value is recalculated server-side during service-request creation. The browser preview is only a convenience and is not trusted for persistence.
- The fallback exchange rate is a configurable demo value (`ExchangeRate:FallbackUsdToZarRate`) and is not a guaranteed current market rate.
- For production, store signed agreements outside the public web root or behind authenticated download endpoints, add authentication/authorization and audit logging, and configure secrets outside `appsettings.json`.


## Unit tests

See `README-UNIT-TESTS.md` for the test inventory, commands to run the suite, and guidance for capturing Test Explorer evidence. Run `dotnet test GLMS.sln --configuration Release` from the solution directory.
