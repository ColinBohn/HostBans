using System;
using System.Data;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Terraria;
using TShockAPI;
using TShockAPI.DB;
using TerrariaApi.Server;
using MySql.Data.MySqlClient;
using Mono.Data.Sqlite;

namespace HostBans
{
    [ApiVersion(1, 14)]
    public class HostBans : TerrariaPlugin
    {
        public override Version Version
        {
            get { return new Version("1.1"); }
        }
        public override string Name
        {
            get { return "HostBans"; }
        }
        public override string Author
        {
            get { return "Colin"; }
        }
        public override string Description
        {
            get { return "Allow banning hostnames with reg ex"; }
        }

        public HostBans(Main game)
            : base(game)
        {
            Order = 10;
        }
        public override void Initialize()
        {
            ServerApi.Hooks.GameInitialize.Register(this, OnInitialize);
            ServerApi.Hooks.ServerConnect.Register(this, OnConnect);
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ServerApi.Hooks.GameInitialize.Deregister(this, OnInitialize);
                ServerApi.Hooks.ServerConnect.Deregister(this, OnConnect);
            }
            base.Dispose(disposing);
        }
        public void OnInitialize(EventArgs args)
        {
            Commands.ChatCommands.Add(new Command("hostban", HostBan, "hostban"));
            SQL.SetupDB();
        }
        private void OnConnect(ConnectEventArgs args)
        {
            var player = new TSPlayer(args.Who);
            string host = SQL.GetHostFromCache(player.IP);
            if (host == null)
            {
                try
                {
                    System.Net.IPHostEntry hostname = System.Net.Dns.GetHostByAddress(player.IP);
                    host = hostname.Hostname;
                }
                catch (Exception e)
                {
                    host = player.IP;
                }
                SQL.InsertCacheEntry(host, player.IP);

            }
            SQL.HostBan ban = SQL.GetBanByHost(host);
            if (ban != null)
            {
                    TShock.Utils.ForceKick(player, string.Format("Banned for: {0}.", ban.Reason), true, false);
                    args.Handled = true;
                    return;
            }
        }
        public void HostBan(CommandArgs args)
        {
            if (args.Parameters.Count < 1)
            {
                args.Player.SendErrorMessage("Usage: /hostban [add|del] <input>");
                args.Player.SendErrorMessage("/hostban add Username RegExHost Reason");
                args.Player.SendErrorMessage("Usage: /hostban del Username");
                return;
            }
            switch (args.Parameters[0])
            {
                case "add":
                    {
                        if (args.Parameters.Count != 4)
                        {
                            args.Player.SendErrorMessage("Invalid input!");
                            return;
                        }
                        if (SQL.AddBan(args.Parameters[2], args.Parameters[1], args.Parameters[3], args.Player.Name, DateTime.Now))
                        {
                            args.Player.SendSuccessMessage("Ban has been added.");
                        }
                        else
                        {
                            args.Player.SendErrorMessage("ERROR: Adding the ban has failed.");
                        }   
                        break;
                    }
                case "del":
                    {
                        if (args.Parameters.Count != 2)
                        {
                            args.Player.SendErrorMessage("Invalid input!");
                            return;
                        }
                        if (SQL.DeleteBanByUsername(args.Parameters[1]))
                        {
                            args.Player.SendSuccessMessage("Ban has been deleted.");
                        }
                        else
                        {
                            args.Player.SendErrorMessage("Ban could not be found.");
                        }
                        break;
                    }
                default:
                    {
                        args.Player.SendErrorMessage("Invalid option!");
                        break;
                    }
            }
        }
    }
}
