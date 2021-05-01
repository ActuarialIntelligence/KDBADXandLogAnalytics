using demo.infrastructure.connectionsnetcore;
using demo.infrastructure.EventHandlers;
using demo.Infrastructure.connectors;
using System;
using System.Configuration;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

class KDBQueryPipeline
{

    private static void SendDataToHubTest(string path)
    {
        //ReadFileFromLocationIntoMemmoryAndSendToEventHub(path);
    }
    /// <summary>
    /// Open to Query injection, needs Security Mechanisms
    /// </summary>
    public static void Main()
    {
        var onlyCreate = true;
        //SendDataToHubTest(@"C:\data\tab.csv");
        string path = @"C:\data\table.csv";
        string writePath = @"C:\data\tables.csv";
        var isBatch = false; // if false means stream is true
        var createTables = false;
        var isMulti = true;
        var getFromStorage = false;// if false next must be true
        var isToADX = true; //if true csv directly used
        // ONLY Setting this to batch to create batch and stream policies and avoiding
        // blob and directly streaming from the file as a data pipeline works as stream 
        // Suggestion: Consider constructing tables with additional
        // calculated/aggregated columns directly at Q) in memmory level 
        // 'number clock cycles it takes to compute vs the time to access memory' is what one wishes to optimize
        // https://code.kx.com/q/basics/math/
        KXQueryParser.KxQuery("localhost", 5000, "AFRICA/rajiyer:");
        Console.WriteLine("Enter to continue with preparations...\n");
        Console.ReadLine();
        Console.WriteLine("{0} Date Time \n", DateTime.Now.ToString());

        KXQueryParser.OptimizedMoveQueryAndWriteToFile("localhost", 5000, "AFRICA/rajiyer:",
                "save `:/data/table.csv", "table");
        if (!onlyCreate)
        {
            var blobURI = "";
            if (getFromStorage)
            {
                blobURI = ReadFileFromLocationIntoMemmoryAndSendToBlobStorage(path, writePath, isToADX);
                Console.WriteLine(blobURI);
            }

            //var eventHubData = ReadFileFromLocationIntoMemmoryAndSendToEventHub(path,writePath);
            // AppID, SecretValue, TenantID 

            // set to true for batch and batch policy creation
            //set to true to create tables            
            SendStreamDirectlyToADXTable(path, writePath,
                "https://ingest-adexcluster.westcentralus.kusto.windows.net",
                "https://adexcluster.westcentralus.kusto.windows.net/",
                "",
                "",
                "MultiTable_CSV_Mapping", "EventHubs", "MultiTable", "",
                "", "",
                isBatch, isMulti, createTables, getFromStorage,
                blobURI);

            Console.WriteLine("ENTER to end session..");
            //File.Delete(path);
            //File.Delete(writePath);
            Console.ReadLine();
        }
    }

    private static void SendStreamDirectlyToADXTable(string path, string writePath, 
        string ingestURI,string kustoConnectURI, string engineURI, string dmsURI,
             string tableMapping,
            string dataBase, string table, string AppclientID, string AppKey, 
            string key, bool isBatch,bool isMulti, bool createTables, bool getFromStorage,
            string blobPath)
    {
        Stream ADXTableData;
        while (true)
        {
            if (File.Exists(path))
            {
                Task.Delay(10000).Wait();
                var sr = new StreamReader(path);
                var all = sr.ReadToEnd();
                sr.Close();
                var res = all.Replace(',', '\t'); // Pipe delimited for ADX Read from Blob
                var sw = new StreamWriter(writePath);
                sw.Write(res);
                sw.Close();
                while (true)
                {
                    if (File.Exists(writePath))
                    {
                        Console.WriteLine("{0} File exists!! \n", path);
                        Console.WriteLine("{0} Date Time \n", DateTime.Now.ToString());
                        Console.WriteLine("Reading into memmory.. \n" + path);
                        Task.Delay(10000).Wait();

                        // Calling the ReadAllBytes() function 
                        var tempBytes = File.ReadAllBytes(path); // only in the case of ADX, direct CSV
                        ADXTableData = new MemoryStream(tempBytes);

                        Console.WriteLine("{0} Date Time \n", DateTime.Now.ToString());
                        Console.WriteLine("Read Successful!! \n");
                        Console.WriteLine("Sending to ADX");
                        ADXConnection.KustoIngest(ingestURI,kustoConnectURI,engineURI,dmsURI,dataBase, table, 
                            tableMapping, ADXTableData, AppclientID,AppKey,key, 
                            isBatch,isMulti, createTables,getFromStorage, blobPath);
                        Console.WriteLine("{0} Date Time \n", DateTime.Now.ToString());
                        Console.WriteLine("Send Successful!! \n");
                        break;
                    }
                }
                break;
            }
        }
    }

    private static string ReadFileFromLocationIntoMemmoryAndSendToEventHub(string path, string writePath)
    {
        string eventHubData;
        while (true)
        {
            if (File.Exists(path))
            {
                var sr = new StreamReader(path);
                var all = sr.ReadToEnd();
                sr.Close();
                var res= all.Replace(',', '\t');
                var sw = new StreamWriter(writePath);
                sw.Write(res);
                sw.Close();
                while (true)
                {
                    if (File.Exists(writePath))
                    {
                        Console.WriteLine("{0} File exists!! \n", path);
                        Console.WriteLine("{0} Date Time \n", DateTime.Now.ToString());
                        Console.WriteLine("Reading into memmory.. \n" + writePath);
                        Task.Delay(10000).Wait();
                        var src = new StreamReader(writePath);
                        eventHubData = src.ReadToEnd();
                        src.Close();
                        
                        Console.WriteLine("{0} Date Time \n", DateTime.Now.ToString());
                        Console.WriteLine("Read Successful!! \n");
                        Console.WriteLine("Sending to Hub");
                        Task.Run(async () => await AzureIngest.SendToEventHub(eventHubData)).Wait();
                        Console.WriteLine("{0} Date Time \n", DateTime.Now.ToString());
                        Console.WriteLine("Send Successful!! \n");
                        break;
                    }
                }
                break;
            }
        }

        return eventHubData;
    }

    private static string ReadFileFromLocationIntoMemmoryAndSendToBlobStorage(string ReadPath, 
        string SavePath, bool isToADX)
    {
        var blobURI = "";
        while (true)
        {
            if (File.Exists(ReadPath))
            {
                var sr = new StreamReader(ReadPath);
                var all = sr.ReadToEnd();
                sr.Close();
                var res = all.Replace(',', '\t');
                var sw = new StreamWriter(SavePath);
                sw.Write(res);
                sw.Close();
                while (true)
                {
                    if (File.Exists(SavePath))
                    {
                        var fileBytes = File.ReadAllBytes(isToADX ==true ? ReadPath: ReadPath);
                        var connectionstring = ConfigurationManager.ConnectionStrings["BlobStorage"].ConnectionString;
                        var source = ConfigurationManager.AppSettings["source"];
                        var dest = ConfigurationManager.AppSettings["dest"];
                        Console.WriteLine("{0} Date Time \n", DateTime.Now.ToString());
                        Console.WriteLine("Read Successful!! \n");
                        Console.WriteLine("Sending to Blob storage...");
                        var task = Task.Run(async () => await AzureIngest.SendToBlobStore(SavePath,
                            fileBytes, connectionstring, source, dest));
                        blobURI = task.GetAwaiter().GetResult();
                        Console.WriteLine("{0} Date Time \n", DateTime.Now.ToString());
                        Console.WriteLine("Send Successful!! \n");
                        break;
                    }
                }
                break;
            }
        }

        return blobURI;
    }

    private static void SendDataToPortTest(string host, int port, byte[] bytes)
    {
        #region reuse
        //KXQueryParser.Subscribe("", 5000, "", EventHub.SendToEventHub);
        //var str = @"GET /url HTTP/1.1\r\n Host: www.servername.com\r\nAccept: image / gif, image / jpeg, */*\r\nAccept-Language: en-us\r\nAccept-Encoding: gzip, deflate\r\nUser-Agent: Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.1)\r\n";
        //byte[] bytes = Encoding.ASCII.GetBytes(str);
        //byte[] array = new byte[512];
        //Random random = new Random();
        //random.NextBytes(array);
        //SendDataToPort("localhost", 80, bytes);
        #endregion

        TcpClient tcpclnt = new TcpClient();
        //Connecting...
        tcpclnt.Connect(host, port);
        //Connected
        Console.WriteLine("Connected!!");

        Stream stm = tcpclnt.GetStream();
        stm.Write(bytes, 0, bytes.Length);

            Console.WriteLine("Sent to Port in Bytes:" + Encoding.UTF8.GetString(bytes));
            byte[] result = new byte[512];
            stm.Read(result, 0, 512);
            Console.WriteLine(Encoding.UTF8.GetString(result));
        Console.WriteLine("Read Successful..");
        tcpclnt.Close();
    }


}