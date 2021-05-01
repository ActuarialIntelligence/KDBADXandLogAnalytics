using qSharp;
using System;
using System.Linq;
using System.Threading.Tasks;
using kx;

namespace demo.Infrastructure.connectors
{
    public static class KDBConnection     
    {


        public static void TestCreate(Func<string, Task> func)
        {


            func("test:"+ DateTime.Now.ToString());
        }

        public static void Subscribe()
        {
            c c = new c("localhost", 5001);
            c.k("sub[`trade;`MSFT.O`IBM.N]");
            while (true)
            {
                object result = c.k();
                c.Flip flip = c.td(result);
                int nRows = c.n(flip.y[0]);
                int nColumns = c.n(flip.x);
                for (int row = 0; row < nRows; row++)
                {
                    for (int column = 0; column < nColumns; column++)
                        System.Console.Write((column > 0 ? "," : "") + c.at(flip.y[column], row));
                    System.Console.WriteLine();
                }
            }
        }
        public static void GetKDBResults(string host, int port, Func<string, Task> func)
        {
            QConnection q = new QBasicConnection(host: host,
                                                 port: port);
            try
            {
                //var result = "";
                q.Open();
                Console.WriteLine("conn: " + q + "  protocol: " + q.ProtocolVersion);

                while (true)
                {
                    Console.Write("Q)");
                    var line = Console.ReadLine();

                    if (line.Equals("\\\\"))
                    {
                        break;
                    }
                    else
                    {
                        try
                        {
                            PrintResult(q.Sync(line), func);                            
                        }
                        catch (QException e)
                        {
                            Console.WriteLine("`" + e.Message);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e);
                Console.ReadLine();
            }
            finally
            {
                q.Close();
            }
        }

        static void PrintResult(object obj, Func<string,Task> func)
        {
            if (obj == null)
            {
                Console.WriteLine("::");
            }
            else if (obj is Array)
            {
                PrintResult(obj as Array, func);
            }
            else if (obj is QDictionary)
            {
                PrintResult(obj as QDictionary, func);
            }
            else if (obj is QTable)
            {
                PrintResult(obj as QTable, func);
            }
            else
            {
                Console.WriteLine(obj);
            }
        }

        static void PrintResult(Array a, Func<string, Task> func)
        {
            Console.WriteLine(Utils.ArrayToString(a));
        }

        static void PrintResult(QDictionary d, Func<string, Task> func)
        {
            foreach (QDictionary.KeyValuePair e in d)
            {
                Console.WriteLine(e.Key + "| " + e.Value);
            }
        }

        static void PrintResult(QTable t, Func<string, Task> func)
        {
            var rowsToShow = Math.Min(t.RowsCount, 20);
            var dataBuffer = new object[1 + rowsToShow][];
            var columnWidth = new int[t.ColumnsCount];

            dataBuffer[0] = new string[t.ColumnsCount];
            for (int j = 0; j < t.ColumnsCount; j++)
            {
                dataBuffer[0][j] = t.Columns[j];
                columnWidth[j] = t.Columns[j].Length + 1;
            }

            for (int i = 1; i < rowsToShow; i++)
            {
                dataBuffer[i] = new string[t.ColumnsCount];
                for (int j = 0; j < t.ColumnsCount; j++)
                {
                    var value = t[i - 1][j].ToString();
                    dataBuffer[i][j] = value;
                    columnWidth[j] = Math.Max(columnWidth[j], value.Length + 1);
                }
            }

            var formatting = "";
            for (int i = 0; i < columnWidth.Length; i++)
            {
                formatting += "{" + i + ",-" + columnWidth[i] + "}";
            }

            Console.WriteLine(formatting, dataBuffer[0]);
            Console.WriteLine(new string('-', columnWidth.Sum()));
            for (int i = 1; i < rowsToShow; i++)
            {
                Console.WriteLine(formatting, dataBuffer[i]);
                func(dataBuffer[i].ToString());
            }
        }
    }
}
   