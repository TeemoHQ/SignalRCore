using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Serialization;

namespace Client
{
    class Program
    {
        private static long _conectedCount = 0;
        private static long _disconectedCount = 0;
        private static SendReciveRecord _srr = new SendReciveRecord();
        private static SignalrArgs _signalrArgs;
        private static RedisHelper _redisHelper;
        private static bool _sendGroupFlag;
        private static bool _openflag;
        static void Main(string[] args)
        {
            InitConfig(args);

            Task.Run(() =>
            {
                while (true)
                {
                    Console.WriteLine($@"当前连接数:{_conectedCount}，已经断开连接数:{_disconectedCount}");
                    Thread.Sleep(2000);
                }
            });

            Run().Wait();
            Console.ReadKey();
        }
        private static void InitConfig(string[] args)
        {
            var builder = new ConfigurationBuilder()
                .AddCommandLine(args);
            var configuration = builder.Build();
            _signalrArgs = new SignalrArgs
            {
                Connections = int.Parse(configuration["Connections"]),
                Url = configuration["Url"],
                MessageRate = int.Parse(configuration["MessageRate"]),
                RedisConnectString = configuration["RedisConnectString"],
                SendPollTime = int.Parse(configuration["SendPollTime"]),
                ConnectInterval= int.Parse(configuration["ConnectInterval"]),
            };

            RedisHelper.ConnectionString = _signalrArgs.RedisConnectString;
            _redisHelper = new RedisHelper();
            var str = string.Empty;
            string name = Dns.GetHostName();
            IPAddress[] ipadrlist = Dns.GetHostAddresses(name);
            foreach (IPAddress ipa in ipadrlist)
            {
                if (ipa.AddressFamily == AddressFamily.InterNetwork)
                {
                    str = ipa.ToString();
                    break;
                }
            }
            _srr.Ip = string.IsNullOrEmpty(str) ? Guid.NewGuid().ToString() : str;
        }

        private static async Task Run()
        {
            while (_conectedCount < _signalrArgs.Connections)
            {
                await ConnectSingle();
                await Task.Delay(_signalrArgs.ConnectInterval);
            }
        }

        private static async Task ConnectSingle()
        {
            try
            {
                var connection = new HubConnectionBuilder()
                    .WithUrl(_signalrArgs.Url)
                    .WithConsoleLogger(LogLevel.Error)
                    .Build();
               
                connection.On<string>("SendOne", SingleMessageReceive);
                connection.On<string>("SendGroup", GroupMessageReceive);
                connection.On<string>("BroadcastInvoke", m =>
                {
                    connection.InvokeAsync("BroadcastRecevie", m);
                });
                var sendCts = new CancellationTokenSource();
                connection.Closed += (s) =>
                {
                    Interlocked.Increment(ref _disconectedCount);
                    sendCts.Cancel();
                    connection.DisposeAsync();
                };
                await connection.StartAsync();
                Interlocked.Increment(ref _conectedCount);
                // await LoadTest( connection);
            }
            catch (AggregateException aex) when (aex.InnerExceptions.All(e => e is OperationCanceledException))
            {
            }
            catch (OperationCanceledException)
            {
            }



        }

        private static async Task LoadTest(HubConnection connection)
        {
            await GroupAction(connection);
            var sendThread = new Thread(() =>
            {
                var dateTimeTemp = DateTime.Now;
                while (true)
                {
                    if ((DateTime.Now - dateTimeTemp).Minutes <= _signalrArgs.SendPollTime)
                    {
                        Interlocked.Increment(ref _srr.SingleSend);
                        connection.InvokeAsync("SendOne", $"SendOneInfo");
                        Thread.Sleep(_signalrArgs.MessageRate * 1000);
                    }
                    else
                    {
                        break;
                    }
                }
            });
            sendThread.Start();
        }

        private static async Task GroupAction(HubConnection connection)
        {
            if (_conectedCount + 1 <= 100)
            {
                await connection.InvokeAsync("InsertGroup", "Group1");
            }
            if (_conectedCount == _signalrArgs.Connections && !_sendGroupFlag)
            {
                _sendGroupFlag = true;
                await connection.InvokeAsync("SendGroup", "Group1");
            }
        }
        private static void SingleMessageReceive(string obj)
        {
            Interlocked.Increment(ref _srr.SingleReceive);
            SaveDataToRedis();
        }
        private static void GroupMessageReceive(string obj)
        {
            Interlocked.Increment(ref _srr.GroupReceive);
        }

        private static void SaveDataToRedis()
        {
            if (!_openflag)
            {
                _openflag = true;
                Task.Run(() =>
                {
                    while (true)
                    {
                        _srr.Time = DateTime.Now.ToString("MM-dd HH:mm:ss");
                        _redisHelper.SetSort(_srr.Ip, JsonConvert.SerializeObject(_srr), double.Parse(DateTime.Now.ToString("HHmmss")));
                        Thread.Sleep(5000);
                    }
                });
            }
        }
    }
}
