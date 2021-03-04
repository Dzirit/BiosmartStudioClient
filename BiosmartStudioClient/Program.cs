using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using BiosmartStudioClient;

namespace BiosmarStudioClient
{
    class Program
    {
        static void Main(string[] args)
        {
            var bs = new BiosmartClient("127.0.0.1",60003);
            
            bs.SendRequest(bs.RequestGetOrganizations());
            var answer=bs.ReadAnswer();
            var orgId = bs.ParseOrganizations(answer);
            bs.SendRequest(bs.RequestAddEmployee(orgId[0], "asd",DateTime.Now.ToShortTimeString()));
            answer=bs.ReadAnswer();
            var userId = bs.ParseUserId(answer);
            var t = new PalmTemplate()
            {
                HandType = 101,
                Template = "fdsfds",
                Quality = 100,
                UserId=userId
            };
            var t2 = new PalmTemplate()
            {
                HandType = 101,
                Template = "fdsfffffffds",
                Quality = 100,
                UserId = userId
            };
            var lt = new List<PalmTemplate>();
            lt.Add(t);
            lt.Add(t2);
            bs.SendRequest(bs.RequestAddTemplates(lt));
            answer = bs.ReadAnswer();
            //var timer = Observable.Interval(TimeSpan.FromMilliseconds(1000));
            //var timerDisposable = timer.Subscribe(x =>
            //{
            //    bs.SendRequest(bs.RequestMonitoringEvents());
            //    bs.ReadAnswer();
            //});
            Console.ReadLine();
            bs.CloseConnection();
        }
    }
}
