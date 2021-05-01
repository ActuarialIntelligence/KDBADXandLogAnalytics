using Kusto.Data;
using Kusto.Data.Common;
using Kusto.Data.Net.Client;
using Kusto.Ingest;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace demo.infrastructure.connectionsnetcore
{
    public class ADXConnection
    {
        /// <param name="engineConnectionString">Indicates the connection to the Kusto engine service.</param>
        /// <param name="dmConnectionString">Indicates the connection to the Kusto data management service.</param>
        public static void KustoIngest(string ingestURI,string kustoConnectURI,string engineURI, string dmsURI, string database, string adxtable,
            string tableMapping,Stream ingestionStream, 
            string AppclientID, string AppKey, string key, bool isBatch,
            bool isMulti,bool createTables,bool getFromStorage, string blobPath)
        {            
            //var adxtenantId = "<TenantId>";
            // var kustoUri = "https://<ClusterName>.<Region>.kusto.windows.net/";
            var adxingestUri = ingestURI;// "https://ingest-<ClusterName>.<Region>.kusto.windows.net";
            var adxingestConnectionStringBuilder = new KustoConnectionStringBuilder(adxingestUri)
                .WithAadApplicationKeyAuthentication(AppclientID,AppKey,key);
            var ConnectionStringBuilder = new KustoConnectionStringBuilder(kustoConnectURI)
    .WithAadApplicationKeyAuthentication(AppclientID, AppKey, key);

            var streamAdxEngineConnectionStringBuilder = new KustoConnectionStringBuilder(kustoConnectURI)
    .WithAadApplicationKeyAuthentication(AppclientID, AppKey, key);
            var streamAdxDmsURIConnectionStringBuilder = new KustoConnectionStringBuilder(ingestURI)
    .WithAadApplicationKeyAuthentication(AppclientID, AppKey, key);


            if (createTables)
            {
                Console.WriteLine("Creating ADX table: " + adxtable);
                var createCommand = CreateADXTable(database, adxtable, ConnectionStringBuilder);
                Console.WriteLine("Successfully created ADX table: " + adxtable);
                CreateTableMapping(database, adxtable, tableMapping, ConnectionStringBuilder);
                Console.WriteLine("Successfully created ADX table mapping: " + tableMapping);
            }
                var kustoClient = KustoClientFactory.CreateCslAdminProvider(ConnectionStringBuilder);
                var ingestClient = KustoIngestFactory.CreateQueuedIngestClient(adxingestConnectionStringBuilder);
           
            var streamIngestClient = KustoIngestFactory.
                CreateManagedStreamingIngestClient(streamAdxEngineConnectionStringBuilder, streamAdxDmsURIConnectionStringBuilder);
            if (isMulti)
            {
                using (kustoClient)
                {
                    var tablePolicyAlterCommand =
CslCommandGenerator.GenerateTableAlterStreamingIngestionPolicyCommand(adxtable, isEnabled: true);
                    kustoClient.ExecuteControlCommand(database, tablePolicyAlterCommand);

                    var command =
                        CslCommandGenerator.GenerateTableAlterIngestionBatchingPolicyCommand(
                        database,
                        adxtable,
                        new IngestionBatchingPolicy(maximumBatchingTimeSpan: TimeSpan.FromSeconds(2.0),
                        maximumNumberOfItems: 10000, maximumRawDataSizeMB: 1024));
                    kustoClient.ExecuteControlCommand(command);
                }
            }

            if (isBatch)
            {
                using (kustoClient)
                {
                    var command =
                        CslCommandGenerator.GenerateTableAlterIngestionBatchingPolicyCommand(
                        database,
                        adxtable,
                        new IngestionBatchingPolicy(maximumBatchingTimeSpan: TimeSpan.FromSeconds(2.0),
                        maximumNumberOfItems: 10000, maximumRawDataSizeMB: 1024));
                    kustoClient.ExecuteControlCommand(command);
                }
            }
            else 
            {
                if (!isMulti)
                {
                    var tablePolicyAlterCommand =
    CslCommandGenerator.GenerateTableAlterStreamingIngestionPolicyCommand(adxtable, isEnabled: true);
                    kustoClient.ExecuteControlCommand(database, tablePolicyAlterCommand);
                }
            }

                var properties =
                    new KustoQueuedIngestionProperties(database, adxtable)
                    {
                        Format = DataSourceFormat.csv,
                        IngestionMapping = new IngestionMapping()
                        {
                            IngestionMappingReference = tableMapping,
                            IngestionMappingKind = Kusto.Data.Ingestion.IngestionMappingKind.Csv
                        },
                        IgnoreFirstRecord = true
                    };

            //ingestClient.IngestFromStreamAsync(ingestionStream, ingestionProperties: properties).GetAwaiter().GetResult();
            if (isBatch)
            {
                if (getFromStorage)
                {
                    //can occour in stream still!!
                    ingestClient.IngestFromStorageAsync(blobPath, ingestionProperties: properties).Wait();
                    Console.WriteLine("Ingestion from blob completed successfully!!");
                }
                else
                {
                    ingestClient.IngestFromStream(ingestionStream, ingestionProperties: properties).GetIngestionStatusCollection();
                    Console.WriteLine("Ingestion from stream completed successfully!!");
                }
            }
            else
            {
                if (getFromStorage)
                {
                    //can occour in stream still!!
                    streamIngestClient.IngestFromStorageAsync(blobPath, ingestionProperties: properties).Wait();
                    Console.WriteLine("Ingestion from blob completed successfully!!");
                }
                else
                {
                    streamIngestClient.IngestFromStream(ingestionStream, ingestionProperties: properties).GetIngestionStatusCollection();
                    Console.WriteLine("Ingestion from stream completed successfully!!");
                }
            }


        }

        private static string CreateADXTable(string databaseName, string table, 
            KustoConnectionStringBuilder kustoConnectionStringBuilder)
        {
            var command = "";
                using (var kustoClient = KustoClientFactory.CreateCslAdminProvider(kustoConnectionStringBuilder))
            {
                 command =
                    CslCommandGenerator.GenerateTableCreateCommand(
                        table,
                        new[]
                        {
     
                Tuple.Create("id", "System.Int32"),
                Tuple.Create("date", "System.DateTime"),
                Tuple.Create("time", "System.DateTime"),
                Tuple.Create("sym", "System.String"),
                Tuple.Create("qty", "System.Double"),
                Tuple.Create("px", "System.Double")
                        });
                
                kustoClient.ExecuteControlCommand(databaseName, command);
                //if (!isBatch)
                //{
                //    var tablePolicyAlterCommand =
                //        CslCommandGenerator.GenerateTableAlterStreamingIngestionPolicyCommand(table, isEnabled: true);
                //    kustoClient.ExecuteControlCommand(databaseName, tablePolicyAlterCommand);
                //}
                return command;
               //  kustoClient.ExecuteControlCommand(databaseName, ".create table StreamingDataTable (['id']:int)");
            }
        }

        public static void CreateTableMapping(string databaseName, string table, string tableMappingName, 
            KustoConnectionStringBuilder kustoConnectionStringBuilder)
        {

            using (var kustoClient = KustoClientFactory.CreateCslAdminProvider(kustoConnectionStringBuilder))
            {

                var command =
                    CslCommandGenerator.GenerateTableMappingCreateCommand(
                        Kusto.Data.Ingestion.IngestionMappingKind.Csv,
                        table,
                        tableMappingName,
                        new[] {
                new ColumnMapping() { ColumnName = "id", Properties = new Dictionary<string, string>() { { MappingConsts.Ordinal, "0" } } },
                new ColumnMapping() { ColumnName = "date", Properties =  new Dictionary<string, string>() { { MappingConsts.Ordinal, "1" } } },
                new ColumnMapping() { ColumnName = "time", Properties = new Dictionary<string, string>() { { MappingConsts.Ordinal, "2" } } },
                new ColumnMapping() { ColumnName = "sym", Properties =  new Dictionary<string, string>() { { MappingConsts.Ordinal, "3" } } },
                new ColumnMapping() { ColumnName = "qty", Properties =  new Dictionary<string, string>() { { MappingConsts.Ordinal, "4" } } },
                new ColumnMapping() { ColumnName = "px", Properties =  new Dictionary<string, string>() { { MappingConsts.Ordinal, "5" } } }
                    });

                kustoClient.ExecuteControlCommand(databaseName, command);
            }
        }

        private static string CreateADXTableFromDefinition(string databaseName, string table,
    KustoConnectionStringBuilder kustoConnectionStringBuilder, IDictionary<string, string> tableDefinition)
        {
            var command = "";
            var tuple = new Tuple<string, string>[tableDefinition.Count()];
            int cnt = 0;
            foreach (var keyvaluepair in tableDefinition)
            {
                tuple[cnt] = new Tuple<string, string>(keyvaluepair.Key, keyvaluepair.Value);
                    cnt++;
            }

            using (var kustoClient = KustoClientFactory.CreateCslAdminProvider(kustoConnectionStringBuilder))
            {
                 command =
                    CslCommandGenerator.GenerateTableCreateCommand(
                        table,tuple);
                var tablePolicyAlterCommand = CslCommandGenerator.GenerateTableAlterStreamingIngestionPolicyCommand(table, isEnabled: true);

                kustoClient.ExecuteControlCommand(databaseName, command);

                kustoClient.ExecuteControlCommand(databaseName, tablePolicyAlterCommand);

            }
            return command;
        }

        public static void CreateTableMappingFromDefinition(string databaseName, string table, 
            string tableMappingName, KustoConnectionStringBuilder kustoConnectionStringBuilder,
             IDictionary<string, string> tableDefinition)
        {

            using (var kustoClient = KustoClientFactory.CreateCslAdminProvider(kustoConnectionStringBuilder))
            {
                var columnMappings = new List<ColumnMapping>();
                int cnt = 0;
                foreach (var keyvaluepair in tableDefinition)
                {
                    columnMappings.Add(new ColumnMapping() 
                    { 
                        ColumnName = keyvaluepair.Key, 
                        Properties = new Dictionary<string, string>() { { MappingConsts.Ordinal, cnt.ToString() } } });
                        cnt++; 
                }
                var command =
                    CslCommandGenerator.GenerateTableMappingCreateCommand(
                        Kusto.Data.Ingestion.IngestionMappingKind.Csv,
                        table,
                        tableMappingName, columnMappings);

                kustoClient.ExecuteControlCommand(databaseName, command);
            }
        }

        private static void ValidateIngestion(string databaseName, string table, KustoConnectionStringBuilder kustoConnectionStringBuilder)
        {
            using (var cslQueryProvider = KustoClientFactory.CreateCslQueryProvider(kustoConnectionStringBuilder))
            {
                var query = $"{table} | count";

                var results = cslQueryProvider.ExecuteQuery<long>(databaseName, query);
                Console.WriteLine(results.Single());
            }
        }

    }
}
