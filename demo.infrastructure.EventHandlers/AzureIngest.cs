using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace demo.infrastructure.EventHandlers
{
    public static class AzureIngest
    {
        private const string connectionString = "Endpoint=sb://eventhubkdb2.servicebus.windows.net/;SharedAccessKeyName=withcapturepolicy;SharedAccessKey=sEX7kHYez9FnZBYDeFbGTAxEKwor1x6OH5/cmfuOKkQ=;EntityPath=kdbdatawithcaptureoption";//"connectionstring";
        //private const string connectionStringBlob = "https://powerbiembdeddedrgguestd.blob.core.windows.net/kdbdata?sp=r&st=2021-03-15T11:53:36Z&se=2021-05-31T18:53:36Z&spr=https&sv=2020-02-10&sr=c&sig=U5WKjSJxBzGcjrkj9LSbrtMDdO2zara2v2z7P5FYtxQ%3D";//"connectionstring";
        private const string eventHubName = "kdbdatawithcaptureoption";//"EventHubName";

        public static async Task SendToEventHub(string eventData)
        {
            await using (var producerClient = new EventHubProducerClient(connectionString, eventHubName))
            {
            // Create a batch of events 
                using EventDataBatch eventBatch = await producerClient.CreateBatchAsync();

                // Add events to the batch. An event is a represented by a collection of bytes and metadata. 
                eventBatch.TryAdd(new EventData(Encoding.UTF8.GetBytes(eventData)));

                // Use the producer client to send the batch of events to the event hub
                producerClient.SendAsync(eventBatch).Wait();
                Console.WriteLine("A batch of events has been published.");                
            }
        }

        public static async Task<string> SendToBlobStore(string SavePath, byte[] fileBytes, 
            string connectionstring, string source, string dest)
        {
                CloudStorageAccount acc = CloudStorageAccount.Parse(connectionstring);
                CloudBlobClient client = acc.CreateCloudBlobClient();
                CloudBlobContainer container = client.GetContainerReference(dest);
                container.CreateIfNotExistsAsync().Wait();

                Console.WriteLine("Successfully created Blob container");
                Console.WriteLine("{0} Date Time \n", DateTime.Now.ToString());

                Console.WriteLine("Reading all Bytes of: " + SavePath);
                Console.WriteLine("{0} Date Time \n", DateTime.Now.ToString());
                var dt = DateTime.UtcNow.ToString("yyyy-MM-dd-HH:mm:ss");
                var saveKey = "table" + dt.ToString() + ".csv";
                CloudBlockBlob blob = container.GetBlockBlobReference(saveKey);
                Console.WriteLine("{0} Date Time \n", DateTime.Now.ToString());
                Console.WriteLine("Creating Blob with key: " + saveKey);
                Console.WriteLine("{0} Date Time \n", DateTime.Now.ToString());
                var stream = new MemoryStream(fileBytes);
                Console.WriteLine("Uploading stream...");
                blob.UploadFromStreamAsync(stream).Wait();
                
            var blobUri = blob.Uri.AbsoluteUri;
                var exists = blob.ExistsAsync().GetAwaiter().GetResult();
            Console.WriteLine("{0} Date Time \n", DateTime.Now.ToString());
                Console.WriteLine("Successfully Uploaded Stream!!");
            return blobUri;
        }
    }
}
