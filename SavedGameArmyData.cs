using System;
using System.Collections.Generic;
using TacticalAILib;

namespace GSBPGEMG
{
    [System.Diagnostics.DebuggerDisplay("{ToString(),nq}")]
    public class SavedGameArmyData
    {
        public int Index;
        public string SteamName = "";
        public ulong SteamID;
        public string Email;
        public string PasswordHash;
        public DateTime LastUpdateTime;
        public int TurnsTaken;

        public int PlannedEventsForTurn = 1;
        public List<Event_DirectOrdersSent> PlannedEvents = [];
        //public List<Event_CorpsOrdersSent> PlannedCorpsEvents = []; // TODO (noted by MT)

        public bool AssignedToAPlayer { get => SteamID >= 1 || PasswordHash != null; }

        public string PlayerName(GameInstance instance)
        {
            if (SteamName?.Length >= 1)
                return SteamName;
            else if (SteamID >= 1)
                return Steamworks.SteamFriends.GetFriendPersonaName(new() { m_SteamID = SteamID });
            else if
                (Email?.Length >= 1)
                return Email;
            else
                return instance.AllArmies[Index].Side.ToString() + " Player";
        }

        public void SetPlannedEvents(GameInstance instance)
        {
            List<Event_DirectOrdersSent> currentEventsDirectOrders = [];
            for (int i = instance.CurrentEvents.PlannedEventsStartIndex; i < instance.CurrentEvents.Count; i++)
            {
                Event_DirectOrdersSent directOrdersEvent = instance.CurrentEvents[i] as Event_DirectOrdersSent;
                if (directOrdersEvent?.Unit.Army.Index == Index)
                    currentEventsDirectOrders.Add(directOrdersEvent);
            }

            if (PlannedEventsForTurn >= instance.CurrentGameTurn)
            {
                for (int i = 0; i < PlannedEvents.Count; i++)
                    if (currentEventsDirectOrders.Exists(x => x.ObjectiveCommand.Index == PlannedEvents[i].ObjectiveCommand.Index) == false)
                        instance.CurrentEvents.AddEvent(PlannedEvents[i], clonedEvent: true);

                for (int i = 0; i < currentEventsDirectOrders.Count; i++)
                    if (PlannedEvents.Exists(x => x.ObjectiveCommand.Index == currentEventsDirectOrders[i].ObjectiveCommand.Index) == false)
                        PlannedEvents.Add(currentEventsDirectOrders[i]);
            }
            else
            {
                PlannedEvents.Clear();
                PlannedEvents.AddRange(currentEventsDirectOrders);
            }

            PlannedEventsForTurn = instance.CurrentGameTurn;
        }

        public void MergePlannedEvents(SavedGameArmyData savedArmy)
        {
            if (PlannedEventsForTurn < savedArmy.PlannedEventsForTurn)
                PlannedEvents.Clear();

            for (int i = 0; i < savedArmy.PlannedEvents.Count; i++)
                if (PlannedEvents.Exists(x => x.ObjectiveCommand.Index == savedArmy.PlannedEvents[i].ObjectiveCommand.Index) == false)
                    PlannedEvents.Add(savedArmy.PlannedEvents[i]);
        }

        public void SerializePlayerDetails(SerializerData serializer)
        {
            serializer.WriteByte(Index);

            serializer.WriteString(SteamName);
            serializer.WriteUInt64(SteamID);
            serializer.WriteString(Email);
            serializer.WriteString(PasswordHash);
            serializer.WriteUInt64((ulong)LastUpdateTime.Ticks);
            serializer.WriteUInt16(TurnsTaken);
        }

        public void DeserializePlayerDetails(SerializerData deserializer)
        {
            Index = deserializer.ReadByte();
            SteamName = deserializer.ReadString();
            SteamID = deserializer.ReadUInt64();
            Email = deserializer.ReadString();
            PasswordHash = deserializer.ReadString();
            LastUpdateTime = new DateTime((long)deserializer.ReadUInt64());
            TurnsTaken = deserializer.ReadUInt16();
        }

        public void SerializePlannedEvents(SerializerEvent serializer)
        {
            serializer.WriteUInt16(PlannedEventsForTurn);
            serializer.WriteUInt16(PlannedEvents.Count);
            for (int i = 0; i < PlannedEvents.Count; i++)
                serializer.WriteEvent(PlannedEvents[i]);
        }

        public void DeserializePlannedEvents(SerializerEvent deserializer)
        {
            PlannedEventsForTurn = deserializer.ReadUInt16();
            int plannedEventsCount = deserializer.ReadUInt16();
            for (int i = 0; i < plannedEventsCount; i++)
                PlannedEvents.Add(deserializer.ReadEvent() as Event_DirectOrdersSent);
        }

        public override string ToString() => $"{Index}: Turns Taken {TurnsTaken}, {PlannedEvents} Planned Events For Turn {PlannedEventsForTurn}";
    }
}
