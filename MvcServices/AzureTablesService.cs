using Microsoft.Extensions.Configuration;

using SujaySarma.Sdk.DataSources.AzureTables;

using System;
using System.Collections.Concurrent;

namespace MultiTenantSample.MvcServices
{
    /// <summary>
    /// Provides cached access to AzureTables bound data structures
    /// </summary>
    public class AzureTablesService : IAzureTableStorageService
    {

        /// <summary>
        /// Gets an initialized reference to a table data source
        /// </summary>
        /// <typeparam name="T">Type of business object to get table for</typeparam>
        /// <param name="tenantId">Guid of the onboarded tenant. Required only for tenant-specific tables</param>
        /// <returns>AzureTablesDataSource ready to use</returns>
        public AzureTablesDataSource GetDataSource<T>(Guid? tenantId = null)
            where T : class
            => tables.GetOrAdd(
                    AzureTablesDataSource.GetTableName<T>(),
                    (tableName) =>
                    {
                        //if (tenantId == null)
                        //{
                            return new AzureTablesDataSource(_configurationDB, tableName);
                        //}

                        //TODO: Add logic to instantiate tenant-specific providers
                    }
                );


        public AzureTablesService(IConfiguration configuration)
        {
            _configurationDB = configuration.GetSection("ConnectionStrings")["ConfigurationDB"];

            tables = new ConcurrentDictionary<string, AzureTablesDataSource>();
        }


        private readonly string _configurationDB;
        private readonly ConcurrentDictionary<string, AzureTablesDataSource> tables;
    }
}
