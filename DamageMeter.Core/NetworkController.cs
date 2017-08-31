﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using DamageMeter.Sniffing;
using Tera.Game;
using Tera.Game.Abnormality;
using Tera.Game.Messages;
using Message = Tera.Message;
using OpcodeId = System.UInt16;
using Tera.PacketLog;
using System.Globalization;

namespace DamageMeter
{
    public class NetworkController
    {
        public delegate void ConnectedHandler(string serverName);

        public delegate void GuildIconEvent(Bitmap icon);
        public delegate void UpdateUiHandler(Tuple<List<ParsedMessage>, Dictionary<OpcodeId, OpcodeEnum>> message);
        public event UpdateUiHandler TickUpdated;
        private static NetworkController _instance;

        private bool _keepAlive = true;
        private long _lastTick;
        internal MessageFactory MessageFactory = new MessageFactory();
        internal bool NeedInit = true;
        public Server Server;
        internal UserLogoTracker UserLogoTracker = new UserLogoTracker();

        private NetworkController()
        {
            TeraSniffer.Instance.NewConnection += HandleNewConnection;
            TeraSniffer.Instance.EndConnection += HandleEndConnection;
            var packetAnalysis = new Thread(PacketAnalysisLoop);
            packetAnalysis.Start();
        }

        public PlayerTracker PlayerTracker { get; internal set; }

        public NpcEntity Encounter { get; private set; }
        public NpcEntity NewEncounter { get; set; }

        public bool TimedEncounter { get; set; }

        public string LoadFileName { get; set; }
        public bool NeedToSave { get; set; }

        public static NetworkController Instance => _instance ?? (_instance = new NetworkController());

        public EntityTracker EntityTracker { get; internal set; }
        public bool SendFullDetails { get; set; }

        public event GuildIconEvent GuildIconAction;

        public void Exit()
        {
            _keepAlive = false;
            TeraSniffer.Instance.Enabled = false;
            Thread.Sleep(500);
            Application.Exit();
        }

        internal void RaiseConnected(string message)
        {
            Connected?.Invoke(message);
        }
            
        public event ConnectedHandler Connected;

        protected virtual void HandleEndConnection()
        {
            NeedInit = true;
            MessageFactory = new MessageFactory();
            Connected?.Invoke("no server");
            OnGuildIconAction(null);
        }

        protected virtual void HandleNewConnection(Server server)
        {
            Server = server;
            NeedInit = true;
            MessageFactory = new MessageFactory();
            Connected?.Invoke(server.Name);
        }
        public Dictionary<OpcodeId, OpcodeEnum> UiUpdateKnownOpcode = new Dictionary<OpcodeId, OpcodeEnum>();
        public List<ParsedMessage> UiUpdateData = new List<ParsedMessage>();
        private void UpdateUi()
        {
            _lastTick = DateTime.UtcNow.Ticks;
            var currentLastPacket = OpcodeFinder.Instance.PacketCount;
            TickUpdated?.Invoke(new Tuple<List<ParsedMessage>, Dictionary<OpcodeId, OpcodeEnum>> (UiUpdateData, UiUpdateKnownOpcode));
            UiUpdateData = new List<ParsedMessage>();
            UiUpdateKnownOpcode = new Dictionary<OpcodeId, OpcodeEnum>();
        }

        private uint Version;

        private void SaveLog()
        {
            if (!NeedToSave) { return; }
            if(IsNetwork == 2)
            {
                MessageBox.Show("Saving saved log is retarded");
            }
            NeedToSave = false;
            var header = new LogHeader { Region =  Version.ToString()};
            PacketLogWriter writer = new PacketLogWriter(string.Format("{0}.TeraLog", DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss_"+Version, CultureInfo.InvariantCulture)), header);
            foreach(var message in OpcodeFinder.Instance.AllPackets)
            {
                writer.Append(message.Value);
            }
            writer.Dispose();
            MessageBox.Show("Saved");
        }

        private int IsNetwork = 0;
        private void PacketAnalysisLoop()
        {
     
            while (_keepAlive)
            {
               LoadFile();
                SaveLog();
                // Don't update the UI if too much packet to process: aka log loading & world boss
                if (TeraSniffer.Instance.Packets.Count < 2000)
                {
                    CheckUpdateUi();
                }
                var successDequeue = TeraSniffer.Instance.Packets.TryDequeue(out var obj);
                if (!successDequeue)
                {
                    Thread.Sleep(1);
                    continue;
                }
                
                if (IsNetwork == 0) { IsNetwork = 1; }
                if(IsNetwork == 2 && TeraSniffer.Instance.Connected)
                {
                    throw new Exception("Not allowed to record network while reading log file");
                }
                var message = MessageFactory.Create(obj);
                if(message is C_CHECK_VERSION)
                {
                    Version = (message as C_CHECK_VERSION).Versions[0];
                }
                OpcodeFinder.Instance.Find(message);
            }
        }

        public void CheckUpdateUi()
        {
            var second = DateTime.UtcNow.Ticks;
            if (second - _lastTick < TimeSpan.TicksPerSecond) { return; }
            UpdateUi();
        }

        internal virtual void OnGuildIconAction(Bitmap icon)
        {
            GuildIconAction?.Invoke(icon);
        }

        void LoadFile()
        {
            if (LoadFileName == null) { return; }
            if(IsNetwork != 0) { throw new Exception("Not allowed to load a log file while recording in the network"); }
            IsNetwork = 2;
            LogReader.LoadLogFromFile(LoadFileName).ForEach(x => TeraSniffer.Instance.Packets.Enqueue(x));
            LoadFileName = null;

        }
    }
}