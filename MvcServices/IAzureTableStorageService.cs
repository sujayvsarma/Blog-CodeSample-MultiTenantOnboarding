using SujaySarma.Sdk.DataSources.AzureTables;

using System;

namespace MultiTenantSample.MvcServices
{
    /// <summary>
    /// Interfacing out the AzureTablesService so that it can be service-moduled into 
    /// MVC and then used via DI
    /// </summary>
    public interface IAzureTableStorageService
    {
        /// <summary>
        /// Gets an initialized reference to a table data source
        /// </summary>
        /// <typeparam name="T">Type of business object to get table for</typeparam>
        /// <param name="tenantId">Guid of the onboarded tenant. Required only for tenant-specific tables</param>
        /// <returns>AzureTablesDataSource ready to use</returns>
        AzureTablesDataSource GetDataSource<T>(Guid? tenantId = null) where T : class;

    }
}
