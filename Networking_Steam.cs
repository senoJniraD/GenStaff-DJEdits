using System;
using System.Collections.Concurrent;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using ModelLib;
using Steamworks;
using TacticalAILib;
using static GSBPGEMG.Game1;

namespace GSBPGEMG
{
    public class Networking_Steam
    {
        public static uint AppID = 1128490; // 480
        public bool SteamRunning { get => SteamAPI.IsSteamRunning() && (localID >= 1); }
        public string SteamLaunchSessionParameters;
        public static bool RunWithoutSteam;

        private Thread steamThread;
        private const int steamSendReliableFlag = /*---*/0b00001000;
        private const int steamSendAutoRestartFlag = /**/0b00100000;

        public ulong localID { get; private protected set; }
        public string localName { get; private protected set; }

        private enum RichPresenceKeys{ steam_display, UserOne, UserTwo, ScenarioName, Turn }
        private string[] richPresenceValues;
        private bool richPresencePendingChange;

        public ConcurrentDictionary<ulong, NetworkPlayer> allNetworkPlayers;
        ConcurrentQueue<NetworkMessage> networkMessagesToSend;
        ConcurrentQueue<NetworkMessage> networkMessagesReceived;

        int sendBufferSize;
        IntPtr sendBufferPtr;
        int sendBufferCircularPosition;
        private IntPtr[] receivedBufferPointers;

        public enum GameDataType
        {
            None,
            InviteToSession,
            SessionHeartbeat,
            LeavingSession,

            CurrentGameDetails,
            DownloadRequest,
            DownloadResponse

            //GameData
        }
        
        public static BinaryWriter GameDataWriter = new(new MemoryStream());
        public static BinaryReader GameDataReader = new(new MemoryStream());

        public class NetworkPlayer
        {
            public string Name = "";
            public bool IsFriend;
            public bool IsOnline;
            public bool IsRunningGame;
            public bool IsConnected;
            public bool IsInSameOnlineGame;

            public CSteamID steamID;
            public EPersonaState steamPersonaState;
            public SteamNetworkingIdentity steamNetworkingIdentity;
            public ESteamNetworkingConnectionState steamConnectionState;
            public SteamNetConnectionRealTimeStatus_t steamRealTimeStatus;

            public bool sessionInvite;
            public bool sessionInviteReceived;
            public bool sessionGameInviteAccepted;
            public bool sessionLeave;

            public bool autoAcceptInvite = true;

            public object[] sentNetworkMessageObjects;
            public double[] lastSendTimes;
            public object[] receivedNetworkMessageObjects;
            public double[] lastReceiveTimes;

            public bool logHeartbeat;

            public SavedGameData SavedGameDetails;

            public byte[] SavedGameDownloadedData;
            public SavedGameData SavedGameDownloaded;
            public bool MergeInProgress;

            public NetworkPlayer(CSteamID CSteamID)
            {
                steamID = CSteamID;
                steamNetworkingIdentity = new SteamNetworkingIdentity();
                steamNetworkingIdentity.SetSteamID(steamID);
                sentNetworkMessageObjects = new object[typeof(GameDataType).GetEnumNames().Length][];
                lastSendTimes = new double[typeof(GameDataType).GetEnumNames().Length];
                Array.Fill(lastSendTimes, double.MinValue);
                receivedNetworkMessageObjects = new object[typeof(GameDataType).GetEnumNames().Length][];
                lastReceiveTimes = new double[typeof(GameDataType).GetEnumNames().Length];
            }

            public void Update(Game1 game)
            {
                SteamNetworkingMessages.GetSessionConnectionInfo(ref steamNetworkingIdentity,
                    out SteamNetConnectionInfo_t pConnectionInfo, out steamRealTimeStatus);
                if (pConnectionInfo.m_eState != steamConnectionState)
                {
                    steamConnectionState = pConnectionInfo.m_eState;
                    Logging.Write("Connection State Changed " + steamConnectionState + " >> " + Name, Logging.MessageType.SteamEventsReceive);
                }

                steamPersonaState = SteamFriends.GetFriendPersonaState(steamID);
                SteamFriends.GetFriendGamePlayed(steamID, out FriendGameInfo_t friendGameInfo);

                Name = SteamFriends.GetFriendPersonaName(steamID);
                IsFriend = SteamFriends.HasFriend(steamID, EFriendFlags.k_EFriendFlagImmediate);
                IsOnline = steamPersonaState == EPersonaState.k_EPersonaStateOnline;
                IsRunningGame = AppID == friendGameInfo.m_gameID.AppID().m_AppId;
                IsConnected = pConnectionInfo.m_eState == ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Connected;
                IsInSameOnlineGame = game.CurrentSavedGameData.SavedDataUniqueID == SavedGameDetails?.SavedDataUniqueID;
            }

            public string GetRealTimeStats()
            {
                if (IsConnected)
                    return $"Ping {steamRealTimeStatus.m_nPing}ms  |  " +
                        $"In {(int)Math.Ceiling(steamRealTimeStatus.m_flInPacketsPerSec)}P/s {(int)Math.Ceiling(steamRealTimeStatus.m_flInBytesPerSec)} B/s  |  " +
                        $"Out {(int)Math.Ceiling(steamRealTimeStatus.m_flOutPacketsPerSec)} P/s {(int)Math.Ceiling(steamRealTimeStatus.m_flOutBytesPerSec)} B/s  |  " +
                        $"Loss {(int)Math.Clamp(100f - (steamRealTimeStatus.m_flConnectionQualityRemote * 100), 0, 100)}% ";
                else
                    return "Connection State: " + steamConnectionState.ToString().Replace("k_ESteamNetworkingConnectionState", "");
            }

            public bool TimeToSend(GameDataType type, double minTimeInterval)
            {
                if (GameTimeRef.TotalGameTime.TotalSeconds >= (lastSendTimes[(int)type] + minTimeInterval))
                {
                    lastSendTimes[(int)type] = GameTimeRef.TotalGameTime.TotalSeconds;
                    return true;
                }
                return false;
            }
        }

        public struct NetworkMessage
        {
            public NetworkPlayer networkPlayer;
            public int channelNo;
            public int dataType;
            public byte[] data;

            public NetworkMessage(NetworkPlayer networkPlayer, int channelNo, byte[] data)
            {
                this.networkPlayer = networkPlayer;
                this.channelNo = channelNo;
                this.data = data;
            }
        }

        public Networking_Steam()
        {
            localName = "";
            richPresenceValues = new string[5];
            allNetworkPlayers = new ConcurrentDictionary<ulong, NetworkPlayer>();
            GameDataWriter = new BinaryWriter(new MemoryStream());
            GameDataReader = new BinaryReader(new MemoryStream());
            networkMessagesToSend = new ConcurrentQueue<NetworkMessage>();
            networkMessagesReceived = new ConcurrentQueue<NetworkMessage>();
            sendBufferSize = 10000000;
            sendBufferPtr = Marshal.AllocHGlobal(sendBufferSize);
            receivedBufferPointers = new nint[32];
        }

        public static bool Initialize()
        {
#if DEBUG && !DEBUGWITHOUTSTEAM
            //return RunWithoutSteam = true;
#endif

            if (SteamAPI.Init() == false)
            {
                if (System.Windows.Forms.MessageBox.Show(
                    "Steam is not currently running. While the game can be run without Steam please note the differences below.\n\n" +
                    "Features only available while running Steam:\n" +
                    "  - Play online with Steam friends\n" +
                    "  - Receive notifications (game requests and turn taken)\n" +
                    "  - Download user generated content (via Steam workshop)\n" +
                    "  - TO DO: Achievements, cloud saves, etc... \n\n" +
                     "Features always available (with or without Steam running):\n" +
                    "  - Play against the AI\n" +
                    "  - Play with friends locally (same device) or remotely (via email)\n\n" +
                    "Do you wish to proceed with starting Steam? Please select \"Yes\" to startup Steam or \"No\" to continue without Steam.",
                    "Start Steam?", System.Windows.Forms.MessageBoxButtons.YesNo) == System.Windows.Forms.DialogResult.Yes)
                {
                    SteamAPI.RestartAppIfNecessary((AppId_t)AppID);
                    return false;
                }
                else
                {
                    RunWithoutSteam = true;
                }
            }

            return true;
        }

        public void Update(Game1 game)
        {
            if (RunWithoutSteam)
                return;

            if (steamThread == null)
            {
                steamThread = new Thread(SteamUpdateThread);
                steamThread.Start();
                return;
            }

            GameInstance instance = game.Instance;
            SavedGameData currentSavedGameData = game.CurrentSavedGameData;

            // Send
            foreach (NetworkPlayer player in allNetworkPlayers.Values)
            {
                if ((!player.IsOnline || !player.IsFriend || !player.IsRunningGame) && !player.IsConnected)
                    continue;

                if (!player.IsConnected)
                {
                    if (player.TimeToSend(GameDataType.SessionHeartbeat, 60d))
                        Game1.SteamServices.SessionInviteSend(player);
                    continue;
                }

                if (player.TimeToSend(GameDataType.SessionHeartbeat, 5d))
                {
                    GameDataWriter.Write((byte)GameDataType.SessionHeartbeat);
                    SendData(player, 0);
                    player.lastSendTimes[(int)GameDataType.SessionHeartbeat] = GameTimeRef.TotalGameTime.TotalSeconds;
                }

                player.sentNetworkMessageObjects[(int)GameDataType.CurrentGameDetails] ??= new object[1] { DateTime.MinValue };
                object[] messageObjects = (object[])player.sentNetworkMessageObjects[(int)GameDataType.CurrentGameDetails];
                if ((player.TimeToSend(GameDataType.CurrentGameDetails, 15d)) ||
                    (game.CurrentSavedGameData.LastUpdateTime != (DateTime)((object[])player.sentNetworkMessageObjects[(int)GameDataType.CurrentGameDetails])[0]))
                {
                    GameDataWriter.Write((byte)GameDataType.CurrentGameDetails);
                    GameDataWriter.Write((byte)game.GameState);
                    if (game.GameState == GameStates.Gameplay)
                    {
                        byte[] currentGameDetails = currentSavedGameData.SaveAsByteArray(game.Instance, SavedGameData.LoadedStatus.GameDetailsOnly);
                        GameDataWriter.Write(currentGameDetails.Length);
                        GameDataWriter.Write(currentGameDetails);

                    }
                    ((object[])player.sentNetworkMessageObjects[(int)GameDataType.CurrentGameDetails])[0] = currentSavedGameData.LastUpdateTime;
                    SendData(player, 0);
                }

                player.sentNetworkMessageObjects[(int)GameDataType.DownloadRequest] ??= new object[1] { "" };
                messageObjects = (object[])player.sentNetworkMessageObjects[(int)GameDataType.DownloadRequest];
                if ((currentSavedGameData.SavedDataUniqueID ==
                    ((SavedGameData)((object[])player.receivedNetworkMessageObjects[(int)GameDataType.CurrentGameDetails])?[1])?.SavedDataUniqueID) &&
                    currentSavedGameData.DownloadRequired(player.SavedGameDetails?.TurnsProgress) &&
                    !player.MergeInProgress && game.UpdatedSavedGameData == null &&
                    player.TimeToSend(GameDataType.DownloadRequest, 10d))
                {
                    //GameDataWriter.Write((byte)GameDataType.DownloadRequest);
                    //GameDataWriter.Write(CurrentSavedGameData.SavedDataUniqueID);
                    //((object[])player.sentNetworkMessageObjects[(int)GameDataType.DownloadRequest])[0] = CurrentSavedGameData.TurnsProgressState;
                    //SendData(player, 0);
                    //player.mergeInProgress = true;
                }

                if ((player.receivedNetworkMessageObjects[(int)GameDataType.DownloadRequest] != null) &&
                    player.TimeToSend(GameDataType.DownloadResponse, 1d))
                {
                    messageObjects = (object[])player.sentNetworkMessageObjects[(int)GameDataType.DownloadResponse];
                    if (messageObjects == null)
                    {
                        byte[] bytes = currentSavedGameData.SaveAsByteArray(game.Instance, SavedGameData.LoadedStatus.FullGameInstance);
                        player.sentNetworkMessageObjects[(int)GameDataType.DownloadResponse] = messageObjects = [bytes, 0];
                    }

                    byte[] data = (byte[])messageObjects[0];
                    int position = (int)messageObjects[1];
                    int maxBytesToSend = 100000;
                    int noOfBytesToSend = Math.Min((data.Length - position), maxBytesToSend);
                    byte[] bytesToSend = new byte[noOfBytesToSend];
                    Array.Copy(data, position, bytesToSend, 0, noOfBytesToSend);

                    GameDataWriter.Write((byte)GameDataType.DownloadResponse);
                    GameDataWriter.Write(currentSavedGameData.SavedDataUniqueID);
                    GameDataWriter.Write(data.Length);
                    GameDataWriter.Write(bytesToSend.Length);
                    GameDataWriter.Write(bytesToSend);
                    SendData(player, 0);

                    ((object[])player.sentNetworkMessageObjects[(int)GameDataType.DownloadResponse])[1] = position + noOfBytesToSend;
                    if ((position + noOfBytesToSend) == data.Length)
                    {
                        player.receivedNetworkMessageObjects[(int)GameDataType.DownloadRequest] = null;
                        player.sentNetworkMessageObjects[(int)GameDataType.DownloadResponse] = null;
                    }
                }


                //if (!player.IsInSameOnlineGame)
                //    continue;

                //                if (player.isInCurrentPvPGame)
                //                {
                //#if DEBUG
                //                    if (Debug.steamGameTestData != null)
                //                    {
                //                        GameDataWriter.Write((byte)GameDataType.GameData);
                //                        GameDataWriter.Write((ushort)Debug.steamGameTestData.Value.side);
                //                        GameDataWriter.Write((short)Debug.steamGameTestData.Value.unitID);
                //                        GameDataWriter.Write((ushort)Debug.steamGameTestData.Value.location.X);
                //                        GameDataWriter.Write((ushort)Debug.steamGameTestData.Value.location.Y);
                //                        GameDataWriter.Write(Debug.steamGameTestData.Value.facing);
                //                        SendData(player, 0);
                //                        Debug.steamGameTestData = null;
                //                    }
                //#endif
                //                }
            }

            // Receive
            while (ReceivedData(out NetworkMessage message))
            {
                GameDataReader.BaseStream.Position = 0;
                GameDataReader.BaseStream.Write(message.data, 0, message.data.Length);
                GameDataReader.BaseStream.Position = 0;

                GameDataType type = (GameDataType)GameDataReader.ReadByte();
                NetworkPlayer networkPlayer = message.networkPlayer;
                networkPlayer.lastReceiveTimes[(int)type] = GameTimeRef.TotalGameTime.TotalSeconds;
                switch (type)
                {
                    case GameDataType.SessionHeartbeat:
                        break;

                    case GameDataType.CurrentGameDetails:
                        object[] messageObjects = (object[])networkPlayer.receivedNetworkMessageObjects[(int)GameDataType.CurrentGameDetails] ??
                            [0, new SavedGameData()];

                        messageObjects[0] = (GameStates)GameDataReader.ReadByte();
                        if ((GameStates)messageObjects[0] == GameStates.Gameplay)
                        {
                            int byteArrayLength = GameDataReader.ReadInt32();
                            networkPlayer.SavedGameDownloadedData = GameDataReader.ReadBytes(byteArrayLength);



                            SavedGamesCollection.PrioritySavedGamesToCheck.Enqueue((SavedGamesCollection.EntryOrigin.OnlinePlayer,
                                networkPlayer, SavedGameData.LoadedStatus.GameDetailsOnly));
                        }
                        else
                        {
                            messageObjects[1] = new SavedGameData();
                        }

                        networkPlayer.receivedNetworkMessageObjects[(int)GameDataType.CurrentGameDetails] = messageObjects;
                        break;

                    case GameDataType.DownloadRequest:
                        ulong requestSavedDataUniqueID = GameDataReader.ReadUInt64();
                        networkPlayer.receivedNetworkMessageObjects[(int)GameDataType.DownloadRequest] = new object[] { requestSavedDataUniqueID };
                        break;

                    case GameDataType.DownloadResponse:
                        ulong responseSavedDataUniqueID = GameDataReader.ReadUInt64(); // TODO (noted by MT) confirm id
                        int dataLength = GameDataReader.ReadInt32();
                        int noOfBytesToReceive = GameDataReader.ReadInt32();
                        byte[] bytesReceived = GameDataReader.ReadBytes(noOfBytesToReceive);

                        object[] downloadMessageObjects = (object[])networkPlayer.receivedNetworkMessageObjects[(int)GameDataType.DownloadResponse] ??
                            [new byte[dataLength], 0];
                        byte[] data = (byte[])downloadMessageObjects[0];
                        int position = (int)downloadMessageObjects[1];
                        
                        Array.Copy(bytesReceived, 0, data, position, noOfBytesToReceive);
                        networkPlayer.receivedNetworkMessageObjects[(int)GameDataType.DownloadResponse] = downloadMessageObjects;

                        ((object[])networkPlayer.receivedNetworkMessageObjects[(int)GameDataType.DownloadResponse])[1] = position + noOfBytesToReceive;
                        if ((position + noOfBytesToReceive) == data.Length)
                        {
                            networkPlayer.SavedGameDownloadedData = data;
                            SavedGamesCollection.PrioritySavedGamesToCheck.Enqueue((SavedGamesCollection.EntryOrigin.OnlinePlayer,
                                networkPlayer, SavedGameData.LoadedStatus.FullGameInstance));
                            networkPlayer.sentNetworkMessageObjects[(int)GameDataType.DownloadRequest] = null;
                            networkPlayer.receivedNetworkMessageObjects[(int)GameDataType.DownloadResponse] = null;
                        }
                        break;

                        //                    case GameDataType.GameData:

                        //                        Sides side = (Sides)GameDataReader.ReadUInt16();
                        //                        int unitID = GameDataReader.ReadInt16();
                        //                        if (unitID >= 0)
                        //                        {
                        //                            List<MATEUnit> units = (side == ActiveArmy.Side) ? ActiveArmy.Units : OpponentArmy.Units;
                        //                            if (unitID < units.Count)
                        //                            {
                        //                                units[unitID].SetAllValuesByNewLocation(new PointI(GameDataReader.ReadUInt16(), GameDataReader.ReadUInt16()));
                        //                                units[unitID].SetAllValuesByNewFacing(GameDataReader.ReadDouble());
                        //                            }
                        //                        }
                        //                        else
                        //                        {
                        //#if DEBUG
                        //                            Debug.steamSideChange = side;
                        //#endif
                        //                        }
                        //break;
                }
            }

            // Rich Presence
            if (!richPresencePendingChange)
            {
                string[] lastRichPresenceValues = (string[])richPresenceValues.Clone();
                for (int i = 0; i < richPresenceValues.Length; i++)
                    richPresenceValues[i] = "";

                if (game.GameState == GameStates.Gameplay)
                {
                    switch (currentSavedGameData.GameOpponentType)
                    {
                        case SavedGameData.GameOpponentTypes.AI:
                            richPresenceValues[(int)RichPresenceKeys.steam_display] = "#Playing AI";
                            break;

                        case SavedGameData.GameOpponentTypes.LocalMultiplayer:
                            richPresenceValues[(int)RichPresenceKeys.steam_display] = "#Playing Local Multiplayer";
                            break;

                        case SavedGameData.GameOpponentTypes.SteamPlayer:
                        case SavedGameData.GameOpponentTypes.PlayByEmail:
                            richPresenceValues[(int)RichPresenceKeys.steam_display] = "#Playing User";
                            SavedGameArmyData savedArmy = currentSavedGameData.SavedArmies[game.Instance.OpponentArmy.Index];
                            if ((savedArmy.SteamID >= 1) && (savedArmy.TurnsTaken >= 1))
                                richPresenceValues[(int)RichPresenceKeys.UserTwo] = savedArmy.SteamName;
                            break;
                    }
                    richPresenceValues[(int)RichPresenceKeys.UserOne] = localName;
                    richPresenceValues[(int)RichPresenceKeys.ScenarioName] = game.Instance.ScenarioName;
                    richPresenceValues[(int)RichPresenceKeys.Turn] = currentSavedGameData.GameTurn + " / " + game.Instance.ScenarioNumTurns;
                }
                else
                {
                    richPresenceValues[(int)RichPresenceKeys.steam_display] = "#Main Menu";
                }

                for (int i = 0; i < richPresenceValues.Length; i++)
                {
                    if (richPresenceValues[i] != lastRichPresenceValues[i])
                    {
                        richPresencePendingChange = true;
                        break;
                    }
                }
            }
        }

        private void SteamUpdateThread()
        {
            //try
            //{
                SteamLaunchSessionParameters = SteamApps.GetLaunchQueryParam("_sessionid");
                Callback<SteamNetworkingMessagesSessionRequest_t>.Create(CallbackSessionRequest);
                Callback<SteamNetworkingMessagesSessionFailed_t>.Create(CallbackSessionFailed);

                while (true)
                {
                    // Players
                    localID = SteamUser.GetSteamID().m_SteamID;
                    localName = SteamFriends.GetPersonaName();
                    int friendCount = SteamFriends.GetFriendCount(EFriendFlags.k_EFriendFlagImmediate);
                    for (int i = 0; i < friendCount; i++)
                    {
                        CSteamID friendCSteamID = SteamFriends.GetFriendByIndex(i, EFriendFlags.k_EFriendFlagImmediate);
                        if (allNetworkPlayers.ContainsKey(friendCSteamID.m_SteamID) == false)
                            allNetworkPlayers.TryAdd(friendCSteamID.m_SteamID, new NetworkPlayer(friendCSteamID));
                    }
                    foreach (NetworkPlayer networkPlayer in allNetworkPlayers.Values)
                        networkPlayer.Update(GameRef);

                    // Send
                    while (networkMessagesToSend.TryDequeue(out NetworkMessage networkMessage))
                    {
                        if ((sendBufferCircularPosition + networkMessage.data.Length) >= sendBufferSize)
                            sendBufferCircularPosition = 0;
                        IntPtr sendBufferPositionPtr = sendBufferPtr + sendBufferCircularPosition;
                        Marshal.Copy(networkMessage.data, 0, sendBufferPositionPtr, networkMessage.data.Length);
                        sendBufferCircularPosition += networkMessage.data.Length;

                        EResult eResult = SteamNetworkingMessages.SendMessageToUser(ref networkMessage.networkPlayer.steamNetworkingIdentity,
                            sendBufferPositionPtr, (uint)networkMessage.data.Length, steamSendReliableFlag | steamSendAutoRestartFlag, networkMessage.channelNo);
                        if (eResult != EResult.k_EResultOK)
                        {
                            Logging.Write("Send Result " + eResult + " >> " + networkMessage.networkPlayer.Name, Logging.MessageType.SteamEventsSend);
                        }
                    }

                    // Receive
                    int noReceived = SteamNetworkingMessages.ReceiveMessagesOnChannel(0, receivedBufferPointers, receivedBufferPointers.Length);
                    if (noReceived > 0)
                    {
                        for (int i = 0; i < noReceived; i++)
                        {
                            SteamNetworkingMessage_t steamMessage =
                                Marshal.PtrToStructure<SteamNetworkingMessage_t>(receivedBufferPointers[i]);
                            if (allNetworkPlayers.TryGetValue(steamMessage.m_identityPeer.GetSteamID().m_SteamID,
                                out NetworkPlayer networkPlayer))
                            {
                                networkPlayer.IsConnected = true;
                                NetworkMessage networkMessage = new NetworkMessage(networkPlayer, steamMessage.m_nChannel, new byte[steamMessage.m_cbSize]);
                                byte[] copiedData = new byte[steamMessage.m_cbSize];
                                Marshal.Copy(steamMessage.m_pData, copiedData, 0, steamMessage.m_cbSize);
                                networkMessage.data = copiedData;
                                networkMessagesReceived.Enqueue(networkMessage);
                            }
                            SteamNetworkingMessage_t.Release(receivedBufferPointers[i]);
                        }
                    }

                    // Rich presence
                    if (richPresencePendingChange)
                    {
                        for (int i = 0; i < richPresenceValues.Length; i++)
                            SteamFriends.SetRichPresence(((RichPresenceKeys)i).ToString(), richPresenceValues[i]);
                        richPresencePendingChange = false;
                    }

                    // Callbacks
                    SteamAPI.RunCallbacks();

                    // Continue or shutdown
                    if (!Logging.ShutdownLoggingThread)
                        Thread.Sleep(200);
                    else
                        break;
                }
            //}
            //catch (Exception e)
            //{
            //    Logging.Write(e, true, "Steam Failed", e.Source + "\n" + e.Data);
            //}

            try { SteamAPI.Shutdown(); }
            catch { }
        }

        public void SessionInviteSend(NetworkPlayer player)
        {
            GameDataWriter.Write((byte)GameDataType.InviteToSession);
            SendData(player, 0);
            player.sessionInvite = true;
            player.sessionInviteReceived = player.sessionGameInviteAccepted = player.sessionLeave = false;
            Logging.Write("InviteToSession >> " + player.Name, Logging.MessageType.SteamEventsSend);
        }

        public void SessionInviteAccept(NetworkPlayer player)
        {
            SteamNetworkingMessages.AcceptSessionWithUser(ref player.steamNetworkingIdentity);
            player.sessionGameInviteAccepted = true;
            player.sessionInvite = player.sessionInviteReceived = player.sessionLeave = false;
            player.sentNetworkMessageObjects = new object[typeof(GameDataType).GetEnumNames().Length][];
            player.receivedNetworkMessageObjects = new object[typeof(GameDataType).GetEnumNames().Length][];
            Logging.Write("AcceptSessionWithUser >> " + player.Name, Logging.MessageType.SteamEventsSend);
        }

        public void SessionLeave(NetworkPlayer player)
        {
            SteamNetworkingMessages.CloseSessionWithUser(ref player.steamNetworkingIdentity);
            Logging.Write("CloseSessionWithUser >> " + player.Name, Logging.MessageType.SteamEventsSend);
            player.sessionInvite = player.sessionInviteReceived = player.sessionGameInviteAccepted = player.sessionLeave = false;
        }

        private void SendData(NetworkPlayer player, int channelNo)
        {
            byte[] data = (GameDataWriter.BaseStream as MemoryStream).ToArray();
            networkMessagesToSend.Enqueue(new NetworkMessage(player, channelNo, data));
            GameDataWriter.BaseStream.Position = 0;
            GameDataWriter.BaseStream.SetLength(0);
            if (player.logHeartbeat)
                Logging.Write($"{(GameDataType)data[0]} >> {player.Name} ({data.Length} bytes)", Logging.MessageType.SteamEventsSend);
        }

        private bool ReceivedData(out NetworkMessage message)
        {
            if (networkMessagesReceived.TryDequeue(out NetworkMessage dequeuedMessage))
            {
                message = dequeuedMessage;
                message.networkPlayer.lastReceiveTimes[message.data[0]] = GameTimeRef.TotalGameTime.TotalSeconds;
                if (message.networkPlayer.logHeartbeat)
                    Logging.Write($"{(GameDataType)message.data[0]} << {message.networkPlayer.Name} ({message.data.Length} bytes)",
                        Logging.MessageType.SteamEventsReceive);
                return true;
            }
            else
            {
                message = default;
                return false;
            }
        }

        private void CallbackSessionRequest(SteamNetworkingMessagesSessionRequest_t sessionRequest)
        {
            if (allNetworkPlayers.TryGetValue(sessionRequest.m_identityRemote.GetSteamID().m_SteamID, out NetworkPlayer player))
            {
                player.sessionInviteReceived = true;
                player.sessionInvite = player.sessionGameInviteAccepted = false;
                Logging.Write("InviteToSession << " + player.Name, Logging.MessageType.SteamEventsReceive);

                if (player.autoAcceptInvite)
                    SessionInviteAccept(player);
            }
        }

        private void CallbackSessionFailed(SteamNetworkingMessagesSessionFailed_t sessionRequest)
        {
            if (allNetworkPlayers.TryGetValue(sessionRequest.m_info.m_identityRemote.GetSteamID().m_SteamID, out NetworkPlayer player))
                Logging.Write("SessionFailed << " + player.Name + " " + sessionRequest.m_info.m_szEndDebug, Logging.MessageType.SteamEventsReceive);
        }
    }
}
