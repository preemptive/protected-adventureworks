# Dotfuscator Runtime Checks Sample

This is a sample line-of-business solution that demonstrates the use of Runtime Checks, a security feature offered by Dotfuscator Community Edition, which is included with Visual Studio 2017.

The solution is based on the [AdventureWorks2014 OLTP database sample for Microsoft SQL Server](https://github.com/Microsoft/sql-server-samples/releases/tag/adventureworks2014).
The solution contains two apps:

* *AdventureWorksSalesService*: An ASP.NET web service that exposes customer data from the database, and

* *AdventureWorksSalesClient*: A WPF desktop client that interacts with that web service.

## Prerequisites

The following are prerequisites for using this sample:

* Visual Studio 2017

* Microsoft SQL Server or SQL Server Express

* Microsoft SQL Server Management Studio

* The following Windows features:

  * Internet Information Services (IIS) for Windows

  * ASP.NET 4.7

  * WCF HTTP Activation

* Dotfuscator Community Edition (CE) version 5.32 or later. [Get the latest version for Visual Studio 2017 here](https://www.preemptive.com/products/dotfuscator/downloads).

* (*Optional*) An Application Insights resource for the web service

* (*Optional*) An Application Insights resource for the desktop client

## Running the Sample

To set up the database:

1. Download [the AdventureWorks2014 OLTP database sample](https://github.com/Microsoft/sql-server-samples/releases/tag/adventureworks2014) as a full database backup (ZIP archive containing a BAK file).

2. Extract the BAK file to the `Database` subdirectory of this repository.

3. Run SQL Server Management Studio and connect to your SQL Server instance.

4. These instructions assume your [server authentication mode](https://docs.microsoft.com/en-us/sql/database-engine/configure-windows/change-server-authentication-mode#SSMSProcedure) is *SQL Server and Windows Authentication mode*.

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

1. Open the IIS Manager (`inetmgr`).

2. Ensure [IIS' Default Web Site](https://superuser.com/questions/825580/what-exactly-is-default-website) is present. If not, [follow these instructions to re-create it](https://stackoverflow.com/a/30083183/2137382).

3. For the Default Web Site, [enable Anonymous Authentication through the Application Pool identity](https://stackoverflow.com/questions/10418669/hosting-asp-net-in-iis7-gives-access-is-denied/16938687#16938687).

4. Run Visual Studio 2017 as an administrator (needed to publish the service to IIS).

5. Open the `AdventureWorksInternal.sln` solution file and expand the *AdventureWorksSalesService* project node in Solution Explorer.

6. (*Optional*) If using Application Insights, open the `ApplicationInsights.config` file and replace the comment between the `<InstrumentationKey>` tags with the Application Insights instrumentation key for the web service.

7. Open the `Web.config` file and locate the `<connectionStrings>` node. Within the connection strings for  both `SalesEntities` and `CustomerManagement`, replace the following substrings as follows:

  * Replace `INSERT_SQL_INSTANCE_NAME_HERE` with the name of your SQL Server instance (e.g., `.\SQLEXPRESS` for a locally-hosted SQL Server Express).
  
  * Replace `INSERT_SQL_LOGIN_HERE` with the name of the SQL Server login you created when setting up the database.
  
  * Replace `INSERT_SQL_PASSWORD_HERE` with the plain-text password for the SQL Server login.

8. Right click on the *AdventureWorksSalesService* project node and select *Publish...*.

9. Select *IIS, FTP, etc* and click *Publish*.

10. In the Publish profile dialog, enter the following for the Connection page:

  * *Publish method*: *Web Deploy*
  
  * *Server*: `localhost`
  
  * *Site name*: `Default Web Site/Sales`

11. Click *Next*.

12. Ensure the *Configuration* is set to *Release*.

13. Click *Save*.

14. Visual Studio builds and publishes the web service to your local IIS instance.

15. Verify the web service is available by opening a web browser and browsing to [`http://localhost/Sales/Authentication.svc`](http://localhost/Sales/Authentication.svc). The browser should display a "You have created a service" page.

To build and test the desktop client without Runtime Check protection:

1. Run Visual Studio 2017.

2. Open the `AdventureWorksInternal.sln` solution file and expand the *AdventureWorksSalesClient* project node in Solution Explorer.

3. (*Optional*) If using Application Insights, open the `ApplicationInsights.config` file and replace the comment between the `<InstrumentationKey>` tags with the Application Insights instrumentation key for the desktop client.

4. Select the *Release* solution configuration from the appropriate drop-down.

5. Right-click the *AdventureWorksSalesClient* project node in Solution Explorer and select *Build*. 

6. Browse to `AdventureWorksSalesClient\bin\Release` and run `AdventureWorksSalesClient.exe`.

7. At the login prompt, give the username `UserA`, the password `PasswordA`, and the confirmation code `SecretA`.

8. The desktop client opens, allowing reading and writing of AdventureWorks2014 customer data.

## Protecting the Sample

To add Runtime Check protection to the desktop client:

1. Build the desktop client as described in the previous section.

2. In Visual Studio 2017, open the *Tools* menu and select *PreEmptive Protection - Dotfuscator*.

3. If this is your first time running Dotfuscator CE, read and accept the license agreement, and (optionally) register your copy to enable command-line builds.

4. From the Dotfuscator CE user interface, open the *File* menu and select *Open Project...*.

5. Browse to and open `AdventureWorksSalesClient\Dotfuscator.xml`.

6. (*Optional*) View the Runtime Checks that Dotfuscator will inject by opening the *Injection* screen and selecting the *Checks* tab. Two Debugging Checks and one Tamper Check will be listed; double-click each for details.

7. Click the *Build* button.

8. Verify that the protection build completes and that Dotfuscator's *Build Output* pane displays the text `Build Finished.`

Under normal use, the protected desktop client will behave identically to the unprotected version.
To test it:

1. Browse to `AdventureWorksSalesClient\Dotfuscated\Release` and run `AdventureWorksSalesClient.exe`.

2. At the login prompt, give the username `UserA`, the password `PasswordA`, and the confirmation code `SecretA`.

3. The desktop client opens, allowing reading and writing of AdventureWorks2014 customer data.

To test the Debugging Checks:

1. Open a debugger that can operate on .NET apps.

  * If you're using WinDbg, see [this article](https://blogs.msdn.microsoft.com/kaevans/2011/04/11/intro-to-windbg-for-net-developers/) for information on how to use it with .NET apps.

2. Run `AdventureWorksSalesClient\Dotfuscated\Release\AdventureWorksSalesClient.exe`.

3. Trigger one of the two Debugging Checks by having the debugger attached to the process at one of the following points:

  * **"Login" Debugging Check**: When entering and submitting the login confirmation code. The app will then throw exceptions if you try to filter or edit the name data of customer records, even if the debugger is no longer attached at that point.
  
  * **"Query" Debugging Check**: When opening or reloading data in the Email Address, Phone Number, or Credit Card windows. The app will exit.

To test the Tamper Check:

1. Open Dotfuscator CE again (in Visual Studio 2017, open the *Tools* menu and select *PreEmptive Protection - Dotfuscator*).

2. From the *Tools* menu, select *Dotfuscator Command Prompt*.

  * Note that you may see an error saying Dotfuscator CE command line support requires a registered Dotfuscator CE copy. You can ignore this error for these steps.

3. Execute the following in the command line, substituting the path in the first command appropriately:
    
    ```
    cd c:\path\to\project\Dotfuscated\Release  
    mkdir ..\Tampered  
    xcopy . ..\Tampered  
    TamperTester AdventureWorksSalesClient.exe ..\Tampered
    ```

4. Run `AdventureWorksSalesClient\Dotfuscated\Tampered\AdventureWorksSalesClient.exe`.

5. Trigger the Tamper Check by entering and submitting the login confirmation code.

If using Application Insights, to view the incident telemetry after triggering a Check:

1. Open your Application Insights web portal.

2. Open *Metrics Explorer*.

3. Click *Add chart*.

4. For Chart type, select *Grid*.

5. In the new grid, click *Edit*:

  * For Metrics, under *Usage*, check the *Events* checkbox.
  
  * For Group by, select *Event name*.

6. Events generated by the desktop client will appear in the new grid.
  
  * **"Login" Debugging Check**: The event name is "Debugger Detected at Login".
  
  * **"Query" Debugging Check**: The event name is "Debugger Detected when Querying Sensitive Data". Selecting that row will show occurrences of the event in a Search blade; select an occurrence to see details in another blade. Under *Custom Data*, the *Query* key's value will indicate what query (Email Addresses, Phone Numbers, or Credit Cards) was being accessed when the Check detected unauthorized debugging.
  
  * **Tamper Check**: The event name is "Tampering Detected".
 
  * Other feature-related events may also appear.
