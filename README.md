# Syncfusion Angular Grid Component with Microsoft SQL Server Integration

This beginner-friendly guide walks you through setting up and using a Syncfusion Angular Grid Component to display and manage data from a Microsoft SQL Server database. We'll use raw SQL queries with the Microsoft.Data.SqlClient library (no Entity Framework involved) to handle all database operations. This approach is straightforward for simple scenarios and helps you understand the basics of data binding.

The workflow focuses on a custom adaptor to bridge the grid's actions (like reading, creating, updating, and deleting data) with raw SQL commands. By the end of this guide, you'll have a fully functional grid that supports searching, filtering, sorting, grouping, paging, and CRUD (Create, Read, Update, Delete) operations.

## 1. What This Sample Demonstrates
- **Display Data:** Fetch and show records from a SQL Server table in a Syncfusion Angular Grid component.
- **Interactive Features:** Enable built-in grid functionalities like searching, filtering, sorting, grouping, and pagination.
- **CRUD Operations:** Allow users to add, edit, update, and delete records directly from the grid, with changes persisted to the database.
- **Data Handling:** Use raw SQL queries executed via SqlConnection and SqlCommand for all database interactions, keeping things simple and direct without ORM (Object-Relational Mapping) layers like Entity Framework.

This sample is ideal for beginners learning Angular, ASP.NET Core and Syncfusion, as it avoids complex abstractions and focuses on core concepts.

## 2. High-Level Architecture
Here's a simple overview of how the components fit together:
- **User Interface (UI) Layer:** The grid is hosted in a Angular component (e.g., app.component.html). This is where users interact with the data visually.
- **Data Transport Layer:** Syncfusion's DataManager uses a custom adaptor to handle grid requests. The adaptor acts as a middleman, translating grid actions into calls to the data layer.
- **Data Access Layer:** A dedicated class (e.g., TicketRepository.cs) contains methods that execute raw SQL queries using SqlClient. This layer connects to the SQL Server database, performs operations, and returns results.
- **Styling:** The app uses a theme like Tailwind CSS (referenced in styles.css) for a modern look.

### Workflow Diagram (Text-Based):
```
User interacts with Grid (e.g., sort, add record)
                    ↓
DataManager triggers Custom Adaptor method (e.g., read, insert)
                    ↓
Adaptor calls data layer method (e.g., Read, Insert)
                    ↓
Data layer executes raw SQL (via SqlConnection/SqlCommand) on SQL Server
                    ↓
Results flow back: Database → Data Layer → Adaptor → Grid → UI Update
```

This flow ensures efficient, event-driven data handling without reloading the page.

## 3. Prerequisites
Before starting, ensure you have:
- **.NET SDK:** Installed and compatible with the project (e.g., targeting .NET 8.0 or later).
- **SQL Server Instance:** A local or remote SQL Server (e.g., SQL Express) that your machine can access. You'll need admin rights to create databases and tables.
- **NuGet Packages:** The project already includes **@syncfusion/ej2-angular-grids** (for the client side) and **Microsoft.Data.SqlClient** (for SQL connections) in the .csproj file. If starting from scratch, add them via NuGet Package Manager or the command line:
    ```bash
    npm install @syncfusion/ej2-angular-grids

    &
  
    dotnet add package Microsoft.Data.SqlClient
    ```
- **Development Environment:** Visual Studio or VS Code with Node and ASP.NET Core support.
- **Basic Knowledge:** Familiarity with C#, SQL basics, and Angular components will help, but we'll explain everything step-by-step.

## 4. Database Setup
### 4.1 Create the Database and Table
1. Open SQL Server Management Studio (SSMS) or use a command-line tool like sqlcmd.
2. Create a new database (e.g., NetworkSupportDB):
 
    ```sql
    CREATE DATABASE NetworkSupportDB;
    ```

3. Switch to the new database and create the **Tickets** table. This table structure matches the data model used in the Blazor app:
    ```sql
    USE NetworkSupportDB;

    CREATE TABLE dbo.Tickets
    (
        TicketId        INT IDENTITY(1,1) PRIMARY KEY,
        PublicTicketId  NVARCHAR(50) NOT NULL,
        Title           NVARCHAR(200) NOT NULL,
        Description     NVARCHAR(MAX) NULL,
        Category        NVARCHAR(100) NULL,
        Department      NVARCHAR(100) NULL,
        Assignee        NVARCHAR(100) NULL,
        CreatedBy       NVARCHAR(100) NULL,
        Status          NVARCHAR(50)  NULL,
        Priority        NVARCHAR(50)  NULL,
        ResponseDue     DATETIME2     NULL,
        DueDate         DATETIME2     NULL,
        CreatedAt       DATETIME2     NULL,
        UpdatedAt       DATETIME2     NULL
    );
    ```
#### Why this structure?
The columns align with the Tickets C# model class. The IDENTITY on TicketId auto-generates unique IDs, and NOT NULL constraints ensure required fields like Title are always provided.

### 4.2 Optional: Add Sample Data
To test the grid with some initial records, run this INSERT script:
USE NetworkSupportDB;
```sql
INSERT INTO dbo.Tickets
(PublicTicketId, Title, Description, Category, Department, Assignee, CreatedBy, Status, Priority, ResponseDue, DueDate, CreatedAt, UpdatedAt)

VALUES
('NET-1001', 'Cannot access VPN', 'User reports VPN failure after recent update.', 'Access', 'IT Support', 'Alex Johnson', 'Sam Doe', 'Open', 'High', GETUTCDATE(), DATEADD(DAY, 5, GETUTCDATE()), GETUTCDATE(), NULL),
('NET-1002', 'Email sync issue', 'Emails not syncing on mobile device.', 'Email', 'IT Support', 'Jordan Lee', 'Pat Kim', 'In Progress', 'Medium', GETUTCDATE(), DATEADD(DAY, 3, GETUTCDATE()), GETUTCDATE(), NULL);
```

This seeds two tickets for immediate visibility in the grid.

## 5. Configure the Connection String
In the data access file (Data/TicketRepository.cs), locate the connectionString variable and update it to point to your SQL Server instance. Example for a local SQL Express:
```csharp
private static string connectionString = 
    "Data Source=localhost\\SQLEXPRESS;Initial Catalog=NetworkSupportDB;Integrated Security=True;Encrypt=False;Command Timeout=30";
```

Tips:
- Use Integrated Security=True for Windows authentication (no username/password needed).
- For SQL authentication, add: User ID=your_username;Password=your_password;.
- Set Encrypt=False for local dev to avoid certificate issues.
- This string is used across all database methods, so get it right early!

## 6. How the Grid Connects to the Database (Raw SQL Workflow)
The grid doesn't connect directly to SQL; it uses a custom adaptor to delegate operations to raw SQL methods. Here's a breakdown:
### 6.1 Reading Data (Load, Search, Filter, Sort, Group, Page)
When the grid loads or a user applies filters/sorts, the custom adaptor's Read method is called. This invokes TicketRepository.GetTickets(), which:
1. Opens a SqlConnection.
2. Executes a raw SELECT query: SELECT * FROM dbo.Tickets ORDER BY TicketId.
3. Loads results into a DataTable.
4. Maps rows to a list of Tickets objects.

Grid features (e.g., filtering) are handled in-memory using Syncfusion's DataOperations utility after fetching all data. For large datasets, consider optimizing with server-side SQL clauses later.
### 6.2 Creating Records
- User clicks "Add" in the grid toolbar → Adaptor's Insert → TicketRepository.InsertAsync. This method:
- Generates a unique PublicTicketId (via a helper method that checks existing IDs).
- Builds and executes an INSERT SQL statement using string concatenation (e.g., INSERT INTO dbo.Tickets VALUES (...)).
- Uses SqlCommand.ExecuteNonQuery() to insert the row and return the affected count.

### 6.3 Updating Records
- User edits a row and saves → Adaptor's Update → TicketRepository.UpdateAsync.
- The method composes an UPDATE SQL (e.g., UPDATE dbo.Tickets SET Title='New Title' WHERE TicketId=1) and executes it via ExecuteNonQuery.

### 6.4 Deleting Records
- User selects and deletes a row → Adaptor's Remove → TicketRepository.DeleteAsync.
- Executes a DELETE SQL (e.g., DELETE FROM dbo.Tickets WHERE TicketId=1) via ExecuteNonQuery.

**Note:** All operations use raw SQL for simplicity, but we'll discuss security improvements in Section 10.

## 7. Project Structure Quick Reference
- src/app/.component.html: Entry point; includes Grid component.
- src/app/app.component: Defines the SfGrid, toolbar, edit settings, and implements the custom adaptor.
- Data/TicketRepository.cs: Contains all raw SQL methods (GetTickets, AddAsync, etc.).
- Data/Tickets.cs: The C# model class representing a ticket record.
- Program.cs: Registers Syncfusion services and bootstraps the ASP.NET Core app.
- TicketManagement.Server.csproj: Lists dependencies like Syncfusion.EJ2.AspNet.Core and Microsoft.Data.SqlClient.

This structure keeps concerns separated: UI in pages, data logic in Data folder.

## 8. Running the Sample
1. Open the project in your IDE.
2. From the terminal (project's TicketManagement.Server folder):
```bash
dotnet restore   # Installs dependencies
dotnet build     # Compiles the project
dotnet run       # Starts the server
```
3. Open the URL shown in the console (e.g., https://localhost:4200) in your browser.
4. The grid should display tickets from the database. Test adding, editing, and deleting records.

## 9. Security and Reliability Notes for Raw SQL
Raw SQL is powerful but requires care:
- **Prevent SQL Injection:** Avoid string concatenation. Use parameterized queries instead:
    ```csharp
    const string sql = @"INSERT INTO dbo.Tickets (PublicTicketId, Title, ...) VALUES (@PublicTicketId, @Title, ...);";

    using var cmd = new SqlCommand(sql, connection);

    cmd.Parameters.AddWithValue("@PublicTicketId", ticket.PublicTicketId);

    // Add other parameters...
    await cmd.ExecuteNonQueryAsync();
    ```
- **Resource Management:** Always wrap SqlConnection and SqlCommand in using blocks for automatic cleanup.
- **Error Handling:** Add try-catch blocks to log errors (e.g., via Console.WriteLine or a logger).
- **Performance for Large Data:** Currently, all rows are loaded into memory before applying grid operations. For big tables, modify queries to include WHERE, ORDER BY, and OFFSET/FETCH for server-side processing.
Adopt these in production to make your app secure and robust.

## 10. Quick Setup Checklist
1. Create the database and **Tickets** table (use the script in Section 4).
2. Update the connection string in **TicketRepository.cs**.
3. Run `dotnet restore` and `dotnet run`.
4. Open the app in a browser and test grid features/CRUD.
5. Seed data if needed and troubleshoot any issues.

## 11. Summary
This setup integrates SQL Server data into a Syncfusion Angular Grid Component using a custom adaptor and raw SQL via SqlClient. It's beginner-friendly, focusing on direct database interactions without extra frameworks. Follow the steps for a smooth experience, and enhance with parameters/security as you advance. If you encounter issues, refer to troubleshooting or Syncfusion's official docs for more examples.

## 12. FAQ’s for Syncfusion Angular Grid Component with Microsoft SQL Server Integration

Below is a list of common questions beginners might have when working with this setup. These are derived from typical challenges in **Angular**, **Syncfusion**, and raw SQL integrations.

---

#### 1. Why is my grid empty or not showing any data?

- This often happens if the `Tickets` table is empty, the connection string is incorrect, or the database doesn't exist.
- First, verify your SQL Server setup by querying the table directly (e.g., in SSMS).
- Then, double-check the `connectionString` in `TicketRepository.cs` for the right server, database, and authentication details.
- Also, ensure you've run `dotnet restore` and that there are no compilation errors.

---

#### 2. How do I fix SQL connection errors like **"Cannot connect to server"** or **"Trust server certificate"?**

- Common causes include an invalid connection string or network issues.
- Set `Encrypt=False` for local development to bypass certificate problems.
- If using SQL authentication, add `User ID` and `Password` to the string.
- Test the connection outside the app (e.g., in SSMS) to isolate the issue.
- For remote servers, ensure firewall rules allow access.

---

#### 3. Can I switch from raw SQL to Entity Framework Core?

- Yes, but it requires refactoring.
- Replace `TicketRepository.cs` with an EF `DbContext` and `DbSet<Tickets>`.
- Move the connection string to `appsettings.json`.
- Use EF migrations for schema management.
- Update the custom adaptor to use LINQ queries and EF methods like:
  - `AddAsync`
  - `Update`
  - `Remove`
  - `SaveChangesAsync`

---

#### 4. What packages do I need, and how do I install them?

- The essentials are:
  - `@syncfusion/ej2-angular-grids` (for the grid)
  - `Microsoft.Data.SqlClient` (for SQL connections)

They’re already in the `package.json` and `.csproj`, but if missing, run:

```bash
npm install @syncfusion/ej2-angular-grids
```
and
```bash
dotnet add package Microsoft.Data.SqlClient
```
Then execute:
```bash
dotnet restore
```
---

#### 5. How does the custom adaptor work, and why is it needed?

- The custom adaptor overrides methods like:
  - `Read`
  - `Insert`
  - `Update`
  - `Remove`

- These methods call raw SQL functions in `TicketRepository.cs`.
- It bridges the grid's UI events to database actions, enabling features like paging and CRUD without direct SQL in the UI.
---

#### 6. What if CRUD operations fail, like insert or update not saving changes?

- Check for null values in required fields (e.g., `PublicTicketId`, `Title`).
- Ensure column names in your SQL queries match the table exactly (case-sensitive).
- Verify the `TicketId` is correctly used in `WHERE` clauses for updates/deletes.
- Add `try-catch` blocks in `TicketRepository.cs` to log errors for debugging.

---

#### 7. How can I add more features, like custom columns or validation?

- In the Angular Grid configuration, customize the grid by defining column settings for each field.
- For validation, use edit settings with validation rules or custom validators.
- Refer to Syncfusion’s documentation for advanced options like templates or events.

---

#### 8. Where can I find more resources or examples?

- **Syncfusion Angular DataGrid Documentation**  
  [Getting Started with Angular DataGrid | Syncfusion](https://ej2.syncfusion.com/angular/documentation/grid/getting-started)

- **Microsoft SqlClient Documentation**  
  [Microsoft.Data.SqlClient Namespace | Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/api/microsoft.data.sqlclient)

- Community forums like **Stack Overflow** are helpful for resolving specific errors.

## 13. Common Pitfalls and How to Avoid Them  
*(Syncfusion Angular DataGrid with SQL Server – Raw SQL via SqlClient)*

Here are frequent mistakes users make with this raw SQL + Syncfusion **Angular** setup, based on troubleshooting patterns. Avoiding these will save you time and frustration.

---

#### 1. Forgetting to Update the Connection String

- **Pitfall:** Using the default example string, leading to connection failures.
- **Avoidance:** Always customize it immediately after database setup. Test it by running a simple `SELECT` query in code or tools like SSMS.

---

#### 2. Mismatched Column Names or Data Types

- **Pitfall:** Typos in SQL queries or model mappings cause runtime errors or data loss (e.g., `TicketID` vs. `TicketId`).
- **Avoidance:** Copy-paste column names from the `CREATE TABLE` script. Use consistent casing and verify mappings in `GetTickets()` where `DataRow` values are assigned to `Tickets` properties.

---

#### 3. Ignoring SQL Injection Risks with String Concatenation

- **Pitfall:** Building queries like:
  ```csharp
  $"INSERT INTO Tickets VALUES('{userInput}')"
  ```
  which exposes the application to SQL injection attacks.
- **Avoidance:** Switch to parameterized commands (e.g., `cmd.Parameters.AddWithValue("@Title", ticket.Title)`). This is highlighted in the security notes—implement it early.

---

#### 4. Loading Entire Datasets into Memory for Large Tables

- **Pitfall:** The current `ReadAsync` implementation fetches all rows and then applies filters and sorting in-memory, causing performance issues or out-of-memory errors for large datasets.
- **Avoidance:** Modify `GetTickets()` to include dynamic SQL with `WHERE`, `ORDER BY`, and `OFFSET / FETCH` based on Angular Grid request parameters. Build server-side queries instead of client-side filtering.

---

#### 5. Skipping Prerequisites or Package Restoration

- **Pitfall:** Running the app without the .NET SDK, SQL Server, or forgetting to run `dotnet restore`, resulting in missing references or build errors.
- **Avoidance:** Follow the prerequisites checklist carefully. Always run `dotnet restore` after adding packages or cloning the repository.

---

#### 6. Not Handling Resource Disposal Properly

- **Pitfall:** Forgetting to use `using` blocks for `SqlConnection` and `SqlCommand`, leading to connection leaks or timeouts.
- **Avoidance:** Wrap all database objects in `using` statements to ensure proper disposal, even when exceptions occur.

---

#### 7. Overlooking Browser Console or Server Logs for Errors

- **Pitfall:** Ignoring hidden issues such as JavaScript errors in the Angular Grid or uncaught exceptions in the ASP.NET Core backend.
- **Avoidance:** Check browser developer tools (F12) for client-side errors and monitor server console output for backend exceptions. Add logging inside `try-catch` blocks for better visibility.

---

#### 8. Assuming Auto-Generated IDs Work Without Configuration

- **Pitfall:** `PublicTicketId` is custom-generated, but if not handled properly (e.g., via `GeneratePublicTicketIdAsync`), insert operations fail.
- **Avoidance:** Ensure the helper method is implemented and invoked inside `AddTicketAsync`. For `TicketId`, rely on the SQL `IDENTITY` column and do not set it manually.

---

#### 9. Testing with Incomplete Data or Without Seeding

- **Pitfall:** Running the application without sample data, making it difficult to verify grid features and CRUD operations.
- **Avoidance:** Use the optional seed script to insert test records. This allows quick validation of paging, sorting, and CRUD behavior.

---

#### 10. Neglecting Timezone or Date Handling in Queries

- **Pitfall:** Date columns such as `CreatedAt` use `DATETIME2`, but mismatched application and database timezones cause incorrect filtering or sorting.
- **Avoidance:** Use UTC-based functions like `SYSUTCDATETIME()` in seed scripts and SQL queries. In C#, prefer `DateTime.UtcNow` for consistency.

## 14. Let’s Learn About the Connection String  
*(Syncfusion Angular DataGrid with SQL Server – Raw SQL via SqlClient)*

The connection string is essentially a set of key-value pairs that tell your application how to connect to the SQL Server database. It is used in `TicketRepository.cs` to create `SqlConnection` objects for executing queries. In this example, it is hard-coded, which works for local development but is not ideal for flexibility or security.

### 1. Breaking Down the Connection String

Here is the connection string:

```csharp
string connectionString = @"Data Source=SYNCLAPN-43362;
Initial Catalog=NetworkSupportDB;
Integrated Security=True;
Connect Timeout=30;
Encrypt=False;
Trust Server Certificate=False;
Application Intent=ReadWrite;
Multi Subnet Failover=False;
Command Timeout=30";
```

### Let’s dissect each part

#### Data Source (or Server): `SYNCLAPN-43362`

- This is the name or IP address of the SQL Server instance.
- In this case, it refers to a local machine named `SYNCLAPN-43362`.
- For a default local instance, it could be `localhost` or `.` (dot).
- For remote servers, use a hostname or IP address (e.g., `servername.domain.com` or `192.168.1.100`).
- **Common variation:**  
  `Data Source=localhost\SQLEXPRESS` for SQL Express.

#### Initial Catalog (or Database): `NetworkSupportDB`

- Specifies the default database to connect to.
- This must match the database created in SQL Server.

#### Integrated Security: `True`

- Uses Windows Authentication (current Windows user).
- Set to `False` when using SQL Server Authentication.
- When `False`, include:
  ```text
  User ID=your_username;Password=your_password;
  ```

#### Connect Timeout: `30`

- Time in seconds to wait for a connection before timing out.
- Default is 15 seconds; 30 is reasonable for slower networks.

#### Encrypt: `False`

- Disables SSL encryption for the connection.
- Set to `True` for production environments with proper certificates.
- Commonly set to `False` for local development to avoid certificate issues.

#### Trust Server Certificate: `False`

- Applies when `Encrypt=True`.
- `False` requires a trusted certificate.
- Set to `True` for development when using self-signed certificates.

#### Application Intent: `ReadWrite`

- Indicates whether the connection is read-only or read-write.
- Mainly used with Always On Availability Groups.
- `ReadWrite` is standard for CRUD applications.

#### Multi Subnet Failover: `False`

- Improves failover performance in clustered SQL Server environments.
- Usually unnecessary unless using SQL Server clustering.

#### Command Timeout: `30`

- **Not a standard connection string key.**
- Applies to `SqlCommand`, not `SqlConnection`.
- Should be set in code instead:
  ```csharp
  cmd.CommandTimeout = 30;
  ```

> **Tip:** Use tools like [ConnectionStrings.com](https://www.connectionstrings.com) to generate connection strings for different scenarios. Always test your connection using SSMS or a small console application.

---

### 2. Making the Connection String Generic and Portable

Hard-coding the connection string inside `TicketRepository.cs` ties the project to a specific machine. To make it portable and easier to migrate, use one of the following approaches.

#### Option 1: Basic Customization (Beginner-Friendly)

- **Edit Directly:**  
  When sharing the project, instruct users to update `TicketRepository.cs` with their own environment details.
  - Change `Data Source` to `localhost` or their server name.
  - For SQL Authentication:
    ```text
    Integrated Security=False;
    User ID=sa;
    Password=strongpassword;
    ```

**Example generic local connection string:**

```csharp
string connectionString = @"Data Source=localhost;
Initial Catalog=NetworkSupportDB;
Integrated Security=True;
Connect Timeout=30;
Encrypt=False;
Trust Server Certificate=False;";
```

- Remove unnecessary keys like `Application Intent` if not required.

---

#### Option 2: Move to Configuration Files (Recommended)

Using configuration files keeps sensitive information out of source code and allows changes without recompiling.

#### Step 1: Add `appsettings.json`

Create `appsettings.json` in the project root (next to `.csproj`):

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=localhost;Initial Catalog=NetworkSupportDB;Integrated Security=True;Connect Timeout=30;Encrypt=False;Trust Server Certificate=False;"
  }
}
```

Ensure **Copy to Output Directory** is set to **Copy if newer**.

---

#### Step 2: Update `Program.cs`

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

// other builder configuration
```

---

#### Step 3: Refactor `TicketRepository.cs`

- Inject `IConfiguration`.
- Read the connection string dynamically.
- Register `TicketRepository` as a service in `Program.cs`.

```csharp
using Microsoft.Extensions.Configuration;

public class TicketRepository
{
    private readonly string _connectionString;

    public TicketRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection");
    }

    // Example usage
    using var connection = new SqlConnection(_connectionString);
}
```

---

#### Step 4: Inject into Angular Grid Page

- Inject the service where the grid adaptor is implemented.
- Use the service methods to execute database operations.

---

### Migration to Another Machine

- Copy the project.
- Update `appsettings.json`:
  - Change `Data Source` to the target machine (e.g., `TheMachineName\SQLEXPRESS`).
  - Adjust authentication settings if required.
