# Adventure Works Sales Sample

This is a sample line-of-business solution that demonstrates the use of Runtime Checks, a security feature offered by Dotfuscator Community Edition, which is included with Visual Studio 2017.

The solution is based on the AdventureWorks2014 OLTP database sample for Microsoft SQL Server.
The solution contains two apps:

* *AdventureWorksSalesService*: An ASP.NET web service that exposes customer data from the database, and
* *AdventureWorksSalesClient*: A WPF desktop client that interacts with that web service.

## Building the Sample

The following are prerequisites for using this sample:

* Visual Studio 2017
* Microsoft SQL Server or SQL Server Express
* Microsoft SQL Server Management Studio
* Internet Information Services (IIS) for Windows
* (*Optional*) An Application Insights resource the web service
* (*Optional*) An Application Insights resource the desktop client

To set up the database:

1. Download [the AdventureWorks2014 OLTP database sample](https://github.com/Microsoft/sql-server-samples/releases/tag/adventureworks2014) as a full database backup (ZIP archive containing a BAK file).
2. Extract the BAK file to the `Database` subdirectory of this repository.
3. Run SQL Server Management Studio and connect to your SQL Server instance.
4. These instructions assume your [sever authentication mode](https://docs.microsoft.com/en-us/sql/database-engine/configure-windows/change-server-authentication-mode#SSMSProcedure) is *SQL Server and Windows Authentication mode*.
5. [Create a SQL login](https://docs.microsoft.com/en-us/sql/relational-databases/security/authentication-access/create-a-login#SSMSProcedure) for the web service to use.
  * These instructions assume the login is created with *SQL Server authentication*.
  * Record these credentials in a safe place. They will be needed later.
6. Open the `Database\ImportDatabaseBackup.sql` script. Adjust the paths as necessary, then run the script against your SQL Server instance.
7. Under the imported *AdventureWorks2014* database node in Object Explorer, expand the *Security* node and right-click the *Users* node, then *New User...*.
8. Select *SQL user with login*, enter a user name, and for the login name select the login created in step 5. Then click *OK*.
9. Open the `Database\GrantUserRights.sql` script. Adjust the username as necessary, then run the script against your SQL Server instance.
10. Open another connection to your database instance, this time using *SQL Server authentication* and the login created in step 5.
  * Depending on your configuration, you may be prompted to set a new password after logging in.
11. Validate the login's permissions by expanding the *AdventureWorks2014* database node in Object Explorer, expanding the *Tables* sub-node, right-clicking the *Person.Person* table node, and choosing *Select Top 1000 Rows*. Contents of the table should appear.

To build and deploy the web service:

1. Run Visual Studio 2017 as an administrator (needed to publish the service to IIS).
2. Open the `AdventureWorksInternal.sln` solution file and expand the *AdventureWorksSalesService* project node in Solution Explorer.
3. (*Optional*) If using Application Insights, open the `ApplicationInsights.config` file and replace the comment between the `<InstrumentationKey>` tags with the Application Insights instrumentation key for the web service.
4. Open the `Web.config` file and locate the `<connectionStrings>` node. Within the connection strings for  both `SalesEntities` and `CustomerManagement`, replace the following substrings as follows:
  * Replace `INSERT_SQL_INSTANCE_NAME_HERE` with the name of your SQL Server instance (e.g., `.\SQLEXPRESS` for a locally-hosted SQL Server Express).
  * Replace `INSERT_SQL_LOGIN_HERE` with the name of the SQL Server login you created when setting up the database.
  * Replace `INSERT_SQL_PASSWORD_HERE` with the plain-text password for the SQL Server login.
5. Right click on the *AdventureWorksSalesService* project node and select *Publish...*.
6. Select *IIS, FTP, etc* and click *Publish*.
7. In the Publish profile dialog, enter the following for the Connection page:
  * *Publish method*: *Web Deploy*
  * *Server*: `localhost`
  * *Site name*: `Default Web Site/Sales`
8. Click *Next*.
9. Ensure the *Configuration* is set to *Release*.
10. Click *Save*.
11. Visual Studio builds and publishes the web service to your local IIS instance.
12. Verify the web service is available by opening a web browser and browsing to `http://localhost/Sales/Authentication.svc`. The browser should display a "You have created a service" page.

To build and deploy the desktop client without Runtime Check protection:

1. Run Visual Studio 2017.
2. Open the `AdventureWorksInternal.sln` solution file and expand the *AdventureWorksSalesClient* project node in Solution Explorer.
3. (*Optional*) If using Application Insights, open the `ApplicationInsights.config` file and replace the comment between the `<InstrumentationKey>` tags with the Application Insights instrumentation key for the desktop client.
4. Select the *Release* solution configuration from the appropriate drop-down.
5. Right-click the *AdventureWorksSalesClient* project node in Solution Explorer and select *Build*. 
6. Browse to `AdventureWorksSalesClient\bin\Release` and run `AdventureWorksSalesClient.exe`.
7. At the login prompt, give the username `UserA`, the password `PasswordA`, and the confirmation code `SecretA`.
8. The desktop client opens, allowing reading and writing of AdventureWorks2014 customer data.

