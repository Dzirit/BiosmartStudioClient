using System;
using System.Reactive.Linq;

namespace BiosmarStudioClient
{
    class Program
    {
        static void Main(string[] args)
        {
            var bs = new BiosmartClient("127.0.0.1",60003);
            var timer = Observable.Interval(TimeSpan.FromMilliseconds(1000));
            var timerDisposable = timer.Subscribe(x =>
            {
                bs.SendRequest(bs.RequestMonitoringEvents());
                bs.ReadAnswer();
            });
            Console.ReadLine();
            bs.CloseConnection();
        }
    }
}
