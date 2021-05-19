using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Reflection.Metadata;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using System.Xml.XPath;

namespace BiosmarStudioClient
{
    class BiosmartClient //socket version
    {
        private TcpClient client;
        private NetworkStream stream;
        private string server;
        private int port;
        public BiosmartClient(string server, int port)
        {
            try
            {
                this.server = server;
                this.port = port;
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                var enc1251 = Encoding.GetEncoding(1251);
                Console.InputEncoding = enc1251;
                client = new TcpClient(server, port);
            }
            catch (ArgumentNullException e)
            {
                Console.WriteLine($"ArgumentNullException: {e}");
            }
            catch (SocketException e)
            {
                Console.WriteLine($"SocketException: {e}");
            }
        }

        public void SendRequest(string message)
        {
            client = new TcpClient(server, port);
            byte[] data = Encoding.UTF8.GetBytes(message);
            stream = client.GetStream();
            stream.Write(data, 0, data.Length);
            Console.WriteLine("Sent:\n {0}", message);
        }

        public string ReadAnswer()
        {
            var data = new byte[1024 * 1024];
            string responseData = string.Empty;
            stream.Read(data, 0, data.Length);
            var dataWithoutNull = NullRemover(data);//it solve problem with xml parsing
            responseData = Encoding.UTF8.GetString(dataWithoutNull, 0, dataWithoutNull.Length);
            Console.WriteLine("Received:\n {0}", responseData);
            CloseConnection();
            return responseData;
        }
        private byte[] NullRemover(byte[] dataStream)
        {
            int i;
            byte[] temp = new byte[dataStream.Length];
            for (i = 0; i < dataStream.Length - 1; i++)
            {
                if (dataStream[i] == 0x00) break;
                temp[i] = dataStream[i];
            }
            byte[] NullLessDataStream = new byte[i];
            for (i = 0; i < NullLessDataStream.Length; i++)
            {
                NullLessDataStream[i] = temp[i];
            }
            return NullLessDataStream;
        }
        public List<string> ParseOrganizations(string answer)
        {
            List<string> orgs = new List<string>();
            var xml = XDocument.Parse(answer);
            var xRoot = xml.Root;
            var xAnswer = xRoot.Element("answer");
            if (xAnswer.Attribute("type").Value == "6")
            {
                foreach (var rec in xAnswer.Descendants("RECORD"))
                    orgs.Add(rec.Attribute("id").Value);
            }
            else
            {
                Console.WriteLine("Ответ на другую команду!");
            }
            return orgs;
        }
        public string ParseUserId(string answer)
        {
            string userId="";
            var xml = XDocument.Parse(answer);
            var xRoot = xml.Root;
            var xAnswer = xRoot.Element("answer");
            if (xAnswer.Attribute("type").Value == "5")
            {
                foreach (var rec in xAnswer.Descendants("RECORD"))
                    userId=rec.Attribute("id").Value;
            }
            else
            {
                Console.WriteLine("Ответ на другую команду!");
            }
            return userId;
        }

        private byte[] ConvertXmlToByteArray(XElement xml, Encoding encoding)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                XmlWriterSettings settings = new XmlWriterSettings();
                settings.Encoding = encoding;
                settings.Indent = false;
                //settings.OmitXmlDeclaration = true; // No prolog
                using (XmlWriter writer = XmlWriter.Create(stream, settings))
                {
                    xml.Save(writer);
                }
                stream.WriteByte(0);
                return stream.ToArray();
            }
        }
        public string RequestAddTemplates(List<PalmTemplate> palmTemplates)
        {
            XElement krecept =
                new XElement("KRECEPT",
                new XElement("REQUEST",
                    new XAttribute("type", 16)));
            foreach (var palmTemplate in palmTemplates)
            {
                XElement record = new XElement("RECORD",
                         new XAttribute("operation", 1),
                                new XAttribute("templ_data", palmTemplate.Template),
                                new XAttribute("quality", palmTemplate.Quality),
                                new XAttribute("id", palmTemplate.UserId),
                                new XAttribute("templ_format", 2),
                                new XAttribute("finger_no", palmTemplate.HandType),
                                new XAttribute("check_type", "full"));
                krecept.Element("REQUEST")
                        .Add(record);
            }
            
                //конец запроса
            var arr = ConvertXmlToByteArray(krecept, new UTF8Encoding());
            var str = System.Text.Encoding.UTF8.GetString(arr);
            return str;
        }
        public string RequestEmployees()
        {
            XElement krecept = new XElement("KRECEPT",
                new XElement("REQUEST",
                    new XAttribute("type", 5),
                            new XElement("RECORD",
                        new XAttribute("operation", 0))));
            var arr = ConvertXmlToByteArray(krecept, new UTF8Encoding());
            var str = Encoding.UTF8.GetString(arr);
            return str;
        }

        public string RequestStartEventMonitoring(string controllerId)
        {
            XElement krecept =
                new XElement("KRECEPT",
                new XElement("REQUEST", 
                    new XAttribute("type", 17),
                                new XElement("RECORD",
                             new XAttribute("id", controllerId),
                                    new XAttribute("operation", 1), 
                                    new XAttribute("client_id", "mon1"))));
            var arr = ConvertXmlToByteArray(krecept, new UTF8Encoding());
            var str = Encoding.UTF8.GetString(arr);
            return str;
        }

        public string RequestStopEventMonitoring(string controllerId)
        {
            XElement krecept =
                new XElement("KRECEPT",
                new XElement("REQUEST", 
                    new XAttribute("type", 17),
                                new XElement("RECORD", 
                             new XAttribute("id", controllerId),
                                    new XAttribute("operation", 3), 
                                    new XAttribute("client_id", "mon1"))));
            var arr = ConvertXmlToByteArray(krecept, new UTF8Encoding());
            var str = Encoding.UTF8.GetString(arr);
            return str;
        }

        public string RequestControllers()
        {
            XElement krecept =
                new XElement("KRECEPT",
                new XElement("REQUEST",
                    new XAttribute("type", 2)));
            var arr = ConvertXmlToByteArray(krecept, new UTF8Encoding());
            var str = Encoding.UTF8.GetString(arr);
            return str;
        }

        public string RequestMonitoringEvents()
        {
            XElement krecept =
                new XElement("KRECEPT",
                new XElement("REQUEST", 
                    new XAttribute("type", 17),
                                new XElement("RECORD",
                             new XAttribute("operation", 0), 
                                    new XAttribute("client_id", "mon1"))));
            var arr = ConvertXmlToByteArray(krecept, new UTF8Encoding());
            var str = Encoding.UTF8.GetString(arr);
            return str;
        }

        public string RequestEmployeesCards()
        {
            XElement krecept = new XElement("KRECEPT",
                            new XElement("REQUEST",
                                new XAttribute("type", 9), 
                                            new XElement("RECORD", 
                                            new XAttribute("operation", 0))));
            var arr = ConvertXmlToByteArray(krecept, new UTF8Encoding());
            var str = Encoding.UTF8.GetString(arr);
            return str;
        }

        public string RequestAddEmployeeCard()
        {
            XElement krecept = new XElement("KRECEPT",
                            new XElement("REQUEST",
                                new XAttribute("type", 9), 
                                            new XElement("RECORD", 
                                         new XAttribute("operation", 1), 
                                                new XAttribute("ca_ow_id", "bs67200118"),
                                                new XAttribute("ca_value", "1167200117"))));
            var arr = ConvertXmlToByteArray(krecept, new UTF8Encoding());
            var str = Encoding.UTF8.GetString(arr);
            return str;
        }

        public string RequestAddEmployee(string orgId,string firstName, string lastName=null, string middleName=null)
        {
            XElement krecept = new XElement("KRECEPT",
                new XElement("REQUEST",
                // запрос об организациях
                //new XAttribute("type", 6),
                //new XElement("RECORD", new XAttribute("operation", 0))
                //конец запроса
                //запрос информации о сотрудниках
                //new XAttribute("type", 5),
                //new XElement("RECORD", new XAttribute("operation", 0))
                //конец запроса
                //запрос на добавление сотрудника
                new XAttribute("type", 5),
                             new XElement("RECORD",
                              new XAttribute("operation", 1),
                                        new XElement("FIELD",
                                        new XAttribute("name", "last_name"), lastName),
                                        new XElement("FIELD", 
                                        new XAttribute("name", "org_id"),/* new XAttribute("dbid", "bs67200101"),*/ orgId),
                                        new XElement("FIELD", 
                                        new XAttribute("name", "first_name"), firstName),
                                        new XElement("FIELD",
                                        new XAttribute("name", "middle_name"), middleName)
                //конец запроса
                )));
            var arr = ConvertXmlToByteArray(krecept, new UTF8Encoding());
            var str = System.Text.Encoding.UTF8.GetString(arr);
            return str;
        }

        public string RequestGetOrganizations()
        {
            XElement krecept = new XElement("KRECEPT",
                new XElement("REQUEST",
                // запрос об организациях
                        new XAttribute("type", 6),
                                    new XElement("RECORD",
                                    new XAttribute("operation", 0))));
                //конец запроса
            var arr = ConvertXmlToByteArray(krecept, new UTF8Encoding());
            var str = System.Text.Encoding.UTF8.GetString(arr);
            return str;
        }

        public void CloseConnection()
        {
            stream?.Close();
            client?.Close();
        }
    }
}
