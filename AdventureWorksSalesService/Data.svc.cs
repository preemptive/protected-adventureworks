//------------------------------------------------------------------------------
// <copyright file="WebDataService.svc.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System.Data.Services;
using System.Data.Services.Common;

namespace AdventureWorksSalesService
{
    /// <summary>
    /// A WCF data service for accessing customer-related records
    /// in the Adventure Works Sales database.
    /// Only reading and updating of records is permitted; to create or
    /// remove records, or to update phone records, see <see cref="CustomerManagement"/>.
    /// </summary>
    public class Data : DataService<SalesEntities>
    {
        public static void InitializeService(DataServiceConfiguration config)
        {
            // Person-related entries can be changed using this service.
            // To add or remove, client needs to use the CustomerManagement service instead.
            // This ensures proper associations are maintained.
            var readAndEdit = EntitySetRights.AllRead | EntitySetRights.WriteMerge;
            config.SetEntitySetAccessRule("People", readAndEdit);
            config.SetEntitySetAccessRule("EmailAddresses", readAndEdit);
            config.SetEntitySetAccessRule("PersonPhones", readAndEdit);
            config.SetEntitySetAccessRule("PersonCreditCards", readAndEdit);
            config.SetEntitySetAccessRule("CreditCards", readAndEdit);

            // Person records themselves are paged 25 at a time
            config.SetEntitySetPageSize("People", 25);

            // Otherwise default to read-only
            config.SetEntitySetAccessRule("*", EntitySetRights.AllRead);
            config.DataServiceBehavior.MaxProtocolVersion = DataServiceProtocolVersion.V3;

            ServerAppInsights.Client.TrackEvent("DataServiceStartup");
        }

        protected override void OnStartProcessingRequest(ProcessRequestArgs args)
        {
            Auth.ThrowDataServiceExceptionIfNotAuthenticated();
            base.OnStartProcessingRequest(args);
        }

        protected override void HandleException(HandleExceptionArgs args)
        {
            ServerAppInsights.Client.TrackException(args.Exception);
            base.HandleException(args);
        }
    }
}
