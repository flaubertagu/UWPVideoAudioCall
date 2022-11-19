using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VideoReceiverClientApp
{
    public static class SignalRConn
    {
        public static HubConnection connection;
        public static void init()
        {
            //--------------------------------------------
            string url = "http://localhost:5000/video";
            connection = new HubConnectionBuilder()
            .WithUrl(url)
            .Build();

            connection.Closed += async (error) =>
            {
                await Task.Delay(new Random().Next(0, 5) * 1000);
                await connection.StartAsync();
            };
            //--------------------------------------------
        }
    }
}
