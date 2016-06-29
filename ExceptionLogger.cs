using System;
using System.Reflection;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.RetryPolicies;
using Microsoft.WindowsAzure.Storage.Table;
using Serilog;

namespace GuardRex.AzureTableStorageExceptionLogger
{
    public class ExceptionLog : TableEntity
    {
        public ExceptionLog()
        {
        }

        public ExceptionLog(string ts)
        {
            PartitionKey = "EX";
            RowKey = ts;
        }

        public DateTime DT { get; set; }
        public string MachineName { get; set; }
        public string Application { get; set; }
        public string Source { get; set; }
        public string Description { get; set; }

        public async static void Log(
            string storageAccountName, 
            string storageAccountKey, 
            string exceptionsTableName, 
            string exceptionSource, 
            string exceptionDescription, 
            ILogger logger = null)
        {
            DateTime dt = DateTime.UtcNow;
            try
            {
                StorageCredentials storageCredentials = new StorageCredentials(storageAccountName, storageAccountKey);
                CloudStorageAccount storageAccount = new CloudStorageAccount(storageCredentials, true);
                CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
                tableClient.DefaultRequestOptions.RetryPolicy = new ExponentialRetry(TimeSpan.FromSeconds(1), 10);
                CloudTable table_AppExceptions = tableClient.GetTableReference(exceptionsTableName);
                await table_AppExceptions.CreateIfNotExistsAsync();
                ExceptionLog entity = new ExceptionLog((DateTime.MaxValue - dt).ToString());
                entity.DT = dt;
                entity.MachineName = Environment.MachineName;
                entity.Application = Assembly.GetEntryAssembly().GetName().Name;
                entity.Source = exceptionSource;
                entity.Description = exceptionDescription;
                await table_AppExceptions.ExecuteAsync(TableOperation.Insert(entity));
            } 
            catch (System.Exception ex)
            {
                if (logger != null)
                {
                    logger.Error(exceptionSource + ":" + exceptionDescription + ": " + ex.ToString());
                }
                Console.WriteLine(dt.ToString() + " " + Environment.MachineName + ":" + Assembly.GetEntryAssembly().GetName().Name + ":" + exceptionSource + ":" + exceptionDescription + ": " + ex.ToString());
            } 
        }
    }
}