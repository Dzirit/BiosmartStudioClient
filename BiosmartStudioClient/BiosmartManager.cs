using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Extensions.Configuration;
using NLog;

namespace BiosmarStudioClient
{
    public class BiosmartManager
    {
        private string server;
        private bool fullLogmonitoring;
        private string orgName;
        private string scanner;
        ILogger logger;
        public BiosmartManager(IConfiguration configuration)
        {
            try
            {
                logger = LogManager.GetCurrentClassLogger();
                server = configuration["bsAdressHttp"];
                orgName = configuration["Organization"];
                scanner = configuration["ScannerForIdentify"];
                fullLogmonitoring = bool.Parse(configuration["fullLogMonitoring"]);
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                var enc1251 = Encoding.GetEncoding(1251);
                Console.InputEncoding = enc1251;
            }
            catch (Exception e)
            {
                logger.Error($"Exception in constructor {nameof(BiosmartManager)}: {e}");
                throw;
            }
        }
        private HttpClient InitClient(string uri)
        {
            var client = new HttpClient();
            client.BaseAddress = new Uri(uri);
            return client;
        }
        private async Task<string> DoRequestToBiosmart(string request)
        {
            var client = InitClient(server);
            var data = new StringContent(request, Encoding.UTF8, "text/xml");
            var postRequest = await client.PostAsync("1c", data);
            logger.Trace($"Sent to {client.BaseAddress}:\n {postRequest.Content}");
            var answer = await postRequest.Content.ReadAsStringAsync();
            logger.Trace($"Received:{answer} ");
            client?.Dispose();
            return answer;
        }
        #region Users methods
        public async Task<string> GetOrganization()
        {
            try
            {
                var answer = await DoRequestToBiosmart(RequestGetOrganizations());
                return FindOrganization(answer, orgName);
            }
            catch (Exception e)
            {
                logger.Error($"Исключение в методе {nameof(GetOrganization)}:\n {e}");
                return string.Empty;
            }
        }
        public async Task<string> GetController()
        {
            try
            {
                var answer = await DoRequestToBiosmart(RequestGetControllers());
                return FindController(answer, scanner);
            }
            catch (Exception e)
            {
                logger.Error($"Исключение в методе {nameof(GetController)}:\n {e}");
                return string.Empty;
            }
        }

        public async Task<List<Template>> GetTemplates()
        {
            var result = new List<Template>();
            try
            {
                //var ans =await DoRequestToBiosmart(RequestGetTemplates("3799"));

                var answer = await DoRequestToBiosmart(RequestEmployees());
                var ids = ParseUserId(answer);
                ids
                .Select(id =>
                {
                    var ans = DoRequestToBiosmart(RequestGetTemplates(id)).Result;
                    var templates = ParseRequstedTemplates(ans);
                    return templates;
                })
                .Where(templates =>
                (templates != null
                && templates.Count > 0))
                .Select(templates =>
                {
                    result.AddRange(templates);
                    return templates;
                })
                .ToList()
                ;
                //await ids
                //.ToObservable()
                //.Select(id => Observable.FromAsync(async () =>
                // {
                //     var ans = await DoRequestToBiosmart(RequestGetTemplates(id));
                //     var templates = ParseRequstedTemplates(ans);
                //     return templates;
                // }))
                //.Concat()
                //.Where(templates =>
                //(templates != null && templates.Count > 0))
                ////.SelectMany(templates=>tr)???
                //.Subscribe(templates => result.AddRange(templates));

                //.Append(templates)
                //ids.ForEach(id =>
                //{
                //    answer = DoRequestToBiosmart(RequestGetTemplates(id)).Result;
                //    temp = ParseRequstedTemplates(answer);
                //    if (temp != null)
                //        result.AddRange(temp);
                //});
                
            }
            catch (Exception e)
            {
                logger.Error($"Исключение в методе {nameof(GetTemplates)}:\n {e}");
                throw;
            }
            return result;
        }
        #endregion
        #region Answer parsers
        private string FindOrganization(string answer, string firm)
        {
            try
            {
                var xml = XDocument.Parse(answer);
                return (string)xml.Root.Elements("answer")
                    .Single(a => a.FirstAttribute.Value == "6")
                    .Elements("RECORD")
                    .SingleOrDefault(r => r.Attribute("name").Value.Contains(firm))?
                    .Attribute("id");
            }
            catch (Exception e)
            {
                //var m = MethodBase.GetCurrentMethod().DeclaringType.Name;
                logger.Error($"Исключение в методе {nameof(FindOrganization)}:\n {e}");
                //throw;
            }
            return string.Empty;
        }
        private string FindUserName(string answer)
        {
            try
            {
                var xml = XDocument.Parse(answer);
                return (string)xml.Root
                    .Elements("answer")
                    .Single(a => a.FirstAttribute.Value == "5")
                    .Element("RECORD")
                    .Elements("FIELD")
                    .Single(r => r.Attribute("name").Value == "first_name");
            }
            catch (Exception e)
            {
                logger.Error($"Исключение в методе {nameof(FindUserName)}:\n {e}");
            }
            return string.Empty;
        }

        private string FindUser(string answer)
        {
            try
            {
                var xml = XDocument.Parse(answer);
                return (string)xml.Root
                    .Elements("answer")
                    .Single(a => a.FirstAttribute.Value == "17")
                    .ElementsOrNull("RECORD")?
                    .Where(r => r.Attribute("event")?.Value == "64")
                    .LastOrDefault()?
                    .Attribute("ow_id");
            }
            catch (Exception e)
            {
                logger.Error($"Исключение в методе {nameof(FindUser)}:\n {e}");
            }
            return string.Empty;
        }
        private string FindController(string answer, string controller)
        {
            try
            {
                var xml = XDocument.Parse(answer);
                return (string)xml.Root
                    .Elements("answer")
                    .Single(a => a.FirstAttribute.Value == "2")
                    .Elements("RECORD")
                    .SingleOrDefault(r => r.Attribute("name").Value.Contains(controller) && r.Element("FIELD").Value == "0")?
                    .Attribute("id");
            }
            catch (Exception e)
            {
                logger.Error($"Исключение в методе {nameof(FindController)}:\n {e}");
            }
            return string.Empty;
        }

        private List<string> ParseOrganizations(string answer)
        {
            try
            {
                return XDocument
                    .Parse(answer).Root
                    .Elements("answer")
                    .Single(a => a.FirstAttribute.Value == "6")
                    .Elements("RECORD")
                    .Select(rec =>
                    {
                        var id = rec.Attribute("id").Value;
                        return id;
                    })
                    .ToList();
            }
            catch (Exception e)
            {
                logger.Error($"Исключение в методе {nameof(ParseOrganizations)}:\n {e}");
            }
            return new List<string>();
        }

        private List<string> ParseControllers(string answer)
        {
            try
            {
                var xml = XDocument.Parse(answer);
                logger.Debug("Доступные контроллеры идентифиации");
                return xml.Root
                    .Elements("answer")
                    .Single(a => a.FirstAttribute.Value == "2")
                    .Elements("RECORD")
                    .Select(rec =>
                    {
                        var id = rec.Attribute("id").Value;
                        var name = rec.Attribute("name").Value;
                        logger.Debug($"id-{id}, name - {name}");
                        return (id, name);
                    })
                    .Where(rec => rec.name.Contains("PalmJet"))
                    .Select(rec =>
                    {
                        logger.Debug($"добавлен {rec.id}");
                        return rec.id;
                    })
                    .ToList();
            }
            catch (Exception e)
            {
                logger.Error($"Исключение в методе {nameof(ParseControllers)}:\n {e}");
            }
            return new List<string>();
        }
        private string ParseUserName(string answer)
        {
            try
            {
                var xml = XDocument.Parse(answer);
                return (string)xml.Root
                    .Elements("answer")
                    .Single(a => a.FirstAttribute.Value == "5")
                    .Element("RECORD")
                    .Elements("FIELD")
                    .SingleOrDefault(field => field.Attribute("name").Value == "first_name")
                    .Value;
                    //.Select(field =>
                    //{
                    //    var userName = field.Value;
                    //    logger.Debug($"Найдено имя {userName}");
                    //    return userName;
                    //});
            }
            catch (Exception e)
            {
                logger.Error($"Исключение в методе {nameof(ParseUserName)}:\n {e}");
            }
            return string.Empty;
        }
        private List<string> ParseUserId(string answer)
        {
            try
            {
                var xml = XDocument.Parse(answer/*,LoadOptions.None*/);
                return xml.Root
                    .Elements("answer")
                    .Single(a => a.FirstAttribute.Value == "5")
                    .ElementsOrNull("RECORD")?
                    .Select(rec =>
                    {
                        var userId = rec.Attribute("id").Value;
                        return userId;
                    })
                    .ToList();
            }
            catch (Exception e)
            {
                logger.Error($"Исключение в методе {nameof(ParseUserId)}:\n {e}");
            }
            return new List<string>();
        }
        private List<string> ParseUserIdCards(string answer)
        {
            try
            {
                var xml = XDocument.Parse(answer/*,LoadOptions.None*/);
                return xml.Root
                    .Elements("answer")
                    .Single(a => a.FirstAttribute.Value == "9")
                    .ElementsOrNull("RECORD")?
                    .Select(rec =>
                    {
                        var userId = rec.Attribute("ca_ow_id")?.Value;
                        return userId;
                    })
                    .ToList();
            }
            catch (Exception e)
            {
                logger.Error($"Исключение в методе {nameof(ParseUserIdCards)}:\n {e}");
            }
            return new List<string>();
        }
        private List<string> ParseMonitoring(string answer)
        {
            try
            {
                var xml = XDocument.Parse(answer);
                return xml.Root
                    .Elements("answer")
                    .Single(a => a.FirstAttribute.Value == "17")
                    .ElementsOrNull("RECORD")?
                    .Where(rec=> rec.Attribute("event").Value == "64")
                    .Select(rec =>
                    {
                        logger.Debug($"{answer}");
                        var userId=rec.Attribute("ow_id").Value;
                        logger.Debug($"Пользователь распознан. Id - {userId}");
                        return userId;
                    })
                    .ToList();
            }
            catch (Exception e)
            {
                logger.Error($"Исключение в методе {nameof(ParseMonitoring)}:\n {e}");
            }
            return new List<string>();
        }
        private bool ParseStartEventMonitoring(string answer)
        {
            try
            {
                var xml = XDocument.Parse(answer);
                var err = xml.Root.Elements("answer")
                    .Single(a => a.FirstAttribute.Value == "17")
                    .Descendants("RECORD")
                    .Single()
                    .Attribute("err").Value;
                return err == "1";
            }
            catch (Exception e)
            {
                logger.Error($"Исключение в методе {nameof(ParseStartEventMonitoring)}:\n {e}");
            }
            return false;
        }
        private List<Template> ParseRequstedTemplates(string answer)
        {
            try
            {
                var xml = XDocument.Parse(answer);
                return xml.Root
                    .Elements("answer")
                    .Single(a => a.FirstAttribute.Value == "16")
                    .ElementsOrNull("RECORD")?
                    //.Where(rec =>rec.Attribute("templ_format").Value == "3")//шаблоны руки Palmjet
                    .Select(rec =>
                    {
                        var format = rec.Attribute("templ_format").Value;
                        int type = int.Parse(rec.Attribute("finger_no").Value);
                        if (format == "2")
                            type -= 100;
                        return new Template()
                        {
                            TemplateId = int.Parse(rec.Attribute("templ_id").Value),
                            Type = type,
                            Quality = int.Parse(rec.Attribute("quality").Value),
                            UserId = int.Parse(rec.Attribute("id").Value),
                            Sample = Convert.FromBase64String(rec.Attribute("templ_data").Value)
                        };
                    })
                    .ToList();
            }
            catch (Exception e)
            {
                logger.Error($"Исключение в методе {nameof(ParseRequstedTemplates)}:\n {e}");
                //throw;
            }
            return new List<Template>();
        }
        private bool ParseTemplateIsAdding(string answer)
        {
            try
            {
                var xml = XDocument.Parse(answer);
                var err = xml.Root.Elements("answer")
                    .Single(a => a.FirstAttribute.Value == "16")
                    .Descendants("RECORD")
                    .Single()
                    .Attribute("err").Value;
                return err == "1";
            }
            catch (Exception e)
            {
                logger.Error($"Исключение в методе {nameof(ParseTemplateIsAdding)}:\n {e}");
            }
            return false;
        }
        private string ParseCardNumber(byte[] input)//Для сигура
        {
            var a = new byte[2] { input[2], input[3] };
            Array.Reverse(a);
            var secondPart = BitConverter.ToUInt16(a, 0);
            var firstPart = input[1];
            return $"{firstPart}{secondPart}";
        }
        #endregion
        #region Create request
        private string RequestAddTemplates(BsTemplate palmTemplates)
        {
            XElement krecept =
                new XElement("KRECEPT",
                new XElement("REQUEST",
                    new XAttribute("type", 16)));
            foreach (var palmTemplate in palmTemplates.PalmTemplates)
            {
                XElement record = new XElement("RECORD",
                         new XAttribute("operation", 1),
                                new XAttribute("templ_data", palmTemplate.Template),
                                new XAttribute("quality", palmTemplate.Quality),
                                new XAttribute("id", palmTemplates.UserId),
                                new XAttribute("templ_format", 3),
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


        private string RequestGetTemplates(string userId)
        {
            XElement krecept =
                new XElement("KRECEPT",
                new XElement("REQUEST",
                    new XAttribute("type", 16),
                                new XElement("RECORD",
                                new XAttribute("id", userId),
                                       new XAttribute("operation", 0))));
            var arr = ConvertXmlToByteArray(krecept, new UTF8Encoding());
            var str = Encoding.UTF8.GetString(arr);
            return str;
        }
        private string RequestEmployees(string id = "")
        {
            XElement krecept = new XElement("KRECEPT",
                new XElement("REQUEST",
                    new XAttribute("type", 5),
                            new XElement("RECORD",
                         new XAttribute("operation", 0),
                                new XAttribute("id", id))));
            var arr = ConvertXmlToByteArray(krecept, new UTF8Encoding());
            var str = Encoding.UTF8.GetString(arr);
            return str;
        }

        private string RequestStartEventMonitoring(string controllerId)
        {
            XElement krecept =
                new XElement("KRECEPT",
                new XElement("REQUEST",
                    new XAttribute("type", 17),
                                new XElement("RECORD",
                             new XAttribute("id", controllerId),
                                    new XAttribute("operation", 1),
                                    new XAttribute("client_id", $"mon{controllerId}"))));
            var arr = ConvertXmlToByteArray(krecept, new UTF8Encoding());
            var str = Encoding.UTF8.GetString(arr);
            return str;
        }

        private string RequestStopEventMonitoring(string controllerId)
        {
            XElement krecept =
                new XElement("KRECEPT",
                new XElement("REQUEST",
                    new XAttribute("type", 17),
                                new XElement("RECORD",
                             new XAttribute("id", controllerId),
                                    new XAttribute("operation", 3),
                                    new XAttribute("client_id", $"mon{controllerId}"))));
            var arr = ConvertXmlToByteArray(krecept, new UTF8Encoding());
            var str = Encoding.UTF8.GetString(arr);
            return str;
        }

        private string RequestGetControllers()
        {
            XElement krecept =
                new XElement("KRECEPT",
                new XElement("REQUEST",
                    new XAttribute("type", 2)));
            var arr = ConvertXmlToByteArray(krecept, new UTF8Encoding());
            var str = Encoding.UTF8.GetString(arr);
            return str;
        }

        private string RequestMonitoringEvents(string requsetId)
        {
            XElement krecept =
                new XElement("KRECEPT",
                new XElement("REQUEST",
                    new XAttribute("type", 17),
                                new XElement("RECORD",
                             new XAttribute("operation", 0),
                                    new XAttribute("client_id", $"mon{requsetId}"))));
            var arr = ConvertXmlToByteArray(krecept, new UTF8Encoding());
            var str = Encoding.UTF8.GetString(arr);
            return str;
        }
        private string RequestEmployees()
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
        private string RequestEmployeesCards()
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

        private string RequestAddEmployeeCard(string id, string card)
        {
            XElement krecept = new XElement("KRECEPT",
                            new XElement("REQUEST",
                                new XAttribute("type", 9),
                                            new XElement("RECORD",
                                         new XAttribute("operation", 1),
                                                new XAttribute("ca_ow_id", id),
                                                new XAttribute("ca_value", card))));
            var arr = ConvertXmlToByteArray(krecept, new UTF8Encoding());
            var str = Encoding.UTF8.GetString(arr);
            return str;
        }

        private string RequestAddEmployee(string orgId, int id, string firstName, string lastName = null, string middleName = null)
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
                             new XAttribute("id", id),
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

        private string RequestGetOrganizations()
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
        #endregion

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
    }
}
