using System;
using System.Threading;
using System.Threading.Tasks;
using Server.Models;

namespace Server.Hub
{
    public class TestHub : Microsoft.AspNetCore.SignalR.Hub
    {
        public static SendReciveRecord Srr = new SendReciveRecord();
        public static string SendTime = string.Empty;
        public static string AllRecevieTime = string.Empty;

        public Task Broadcast(string data)
        {
            return Clients.All.SendAsync("BroadcastInvoke", new[] {data});
        }

        public Task BroadcastRecevie(string data)
        {
            return Task.Run(() => { Interlocked.Increment(ref Srr.BroadcastRecevieCount); });
        }

        public Task SendGroup(string groupName)
        {
            return Clients.Group(groupName)
                .SendAsync("SendGroup", new[] {$"小组：{groupName}  群发时间:{DateTime.Now:HH:mm:ss fffff}"});
        }

        public Task SendOne(string connectionId, string message)
        {
            Interlocked.Increment(ref Srr.CliendSend);
            return Clients.Client(connectionId).SendAsync("SendOne", new[] {message});
        }

        public Task InsertGroup(string connectionId, string groupName)
        {
            return Groups.AddAsync(connectionId, groupName);
        }

        public Task InsertGroups(string connectionId, string[] groupNames)
        {
            return Task.Run(() =>
            {
                foreach (var groupName in groupNames)
                {
                    Groups.AddAsync(connectionId, groupName);
                }
            });
        }

        public static int Count;

        public override Task OnConnectedAsync()
        {
            Interlocked.Increment(ref Srr.Connected);
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            Interlocked.Decrement(ref Srr.Disconnected);
            return base.OnDisconnectedAsync(exception);
        }
    }
}
