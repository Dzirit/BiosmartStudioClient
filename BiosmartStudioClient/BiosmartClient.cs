using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace BiosmarStudioClient
{
    class BiosmartClient
    {
        TcpClient client;
        NetworkStream stream;
        public BiosmartClient(string server, int port)
        {
            try
            {
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                var enc1251 = Encoding.GetEncoding(1251);
                Console.InputEncoding = enc1251;
                client = new TcpClient(server, port);
            }
            catch (ArgumentNullException e)
            {
                Console.WriteLine("ArgumentNullException: {0}", e);
            }
            catch (SocketException e)
            {
                Console.WriteLine("SocketException: {0}", e);
            }
        }

        public void SendRequest(string message)
        {
            // Translate the passed message into ASCII and store it as a Byte array.
            Byte[] data = System.Text.Encoding.ASCII.GetBytes(message);
            // Get a client stream for reading and writing.
            // Stream stream = client.GetStream();
            stream = client.GetStream();
            // Send the message to the connected TcpServer.
            stream.Write(data, 0, data.Length);
            Console.WriteLine("Sent:\n {0}", message);
            Debug.WriteLine("Sent:\n {0}", message);
            // Receive the TcpServer.response.

        }
        public void ReadAnswer()
        {
            // Buffer to store the response bytes.
            var data = new Byte[1024 * 1024];
            // String to store the response ASCII representation.
            string responseData = String.Empty;
            // Read the first batch of the TcpServer response bytes.
            var bytes = stream.Read(data, 0, data.Length);
            responseData = System.Text.Encoding.UTF8.GetString(data, 0, bytes);

            Console.WriteLine("Received:\n {0}", responseData);
            Debug.WriteLine("Received:\n {0}", responseData);
        }
        private byte[] ConvertXmlToByteArray(XElement xml, Encoding encoding)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                XmlWriterSettings settings = new XmlWriterSettings();
                // Add formatting and other writer options here if desired
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
        public string RequestEmployees()
        {
            XElement krecept = new XElement("KRECEPT", new XElement("REQUEST", new XAttribute("type", 5), new XElement("RECORD", new XAttribute("operation", 0))));
            var arr = ConvertXmlToByteArray(krecept, new UTF8Encoding());
            var str = Encoding.UTF8.GetString(arr);
            return str;
        }
        public string RequestStartEventMonitoring(string controllerId)
        {
            XElement krecept =
                new XElement("KRECEPT",
                new XElement("REQUEST", new XAttribute("type", 17),
                new XElement("RECORD", new XAttribute("id", controllerId), new XAttribute("operation", 1), new XAttribute("client_id", "mon1"))));
            var arr = ConvertXmlToByteArray(krecept, new UTF8Encoding());
            var str = Encoding.UTF8.GetString(arr);
            return str;
        }
        public string RequestStopEventMonitoring(string controllerId)
        {
            XElement krecept =
                new XElement("KRECEPT",
                new XElement("REQUEST", new XAttribute("type", 17),
                new XElement("RECORD", new XAttribute("id", controllerId), new XAttribute("operation", 3), new XAttribute("client_id", "mon1"))));
            var arr = ConvertXmlToByteArray(krecept, new UTF8Encoding());
            var str = Encoding.UTF8.GetString(arr);
            return str;
        }
        public string RequestControllers()
        {
            XElement krecept =
                new XElement("KRECEPT",
                new XElement("REQUEST", new XAttribute("type", 2)));
            var arr = ConvertXmlToByteArray(krecept, new UTF8Encoding());
            var str = Encoding.UTF8.GetString(arr);
            return str;
        }
        public string RequestMonitoringEvents()
        {
            XElement krecept =
                new XElement("KRECEPT",
                new XElement("REQUEST", new XAttribute("type", 17),
                new XElement("RECORD", new XAttribute("operation", 0), new XAttribute("client_id", "mon1"))));
            var arr = ConvertXmlToByteArray(krecept, new UTF8Encoding());
            var str = Encoding.UTF8.GetString(arr);
            return str;
        }
        public string RequestEmployeesCards()
        {
            XElement krecept = new XElement("KRECEPT", new XElement("REQUEST", new XAttribute("type", 9), new XElement("RECORD", new XAttribute("operation", 0))));
            var arr = ConvertXmlToByteArray(krecept, new UTF8Encoding());
            var str = Encoding.UTF8.GetString(arr);
            return str;
        }
        public string RequestAddEmployeeCard()
        {
            XElement krecept = new XElement("KRECEPT", new XElement("REQUEST", new XAttribute("type", 9), new XElement("RECORD", new XAttribute("operation", 1), new XAttribute("ca_ow_id", "bs67200118"), new XAttribute("ca_value", "1167200117"))));
            var arr = ConvertXmlToByteArray(krecept, new UTF8Encoding());
            var str = Encoding.UTF8.GetString(arr);
            return str;
        }

        public string CreateAddEmployeeRequest(string firstName, string lastName, string middleName)
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
                new XElement("RECORD",/* new XAttribute("id", bsid),*/ new XAttribute("operation", 1),
                new XElement("FIELD", new XAttribute("name", "last_name"), lastName),
                new XElement("FIELD", new XAttribute("name", "org_id"),/* new XAttribute("dbid", "bs67200101"),*/ "bs67200099"),
                new XElement("FIELD", new XAttribute("name", "first_name"), firstName),
                new XElement("FIELD", new XAttribute("name", "middle_name"), middleName)
                //конец запроса
                )));
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
