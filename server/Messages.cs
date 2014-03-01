﻿// this file is auto-generated by tt
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;

namespace Server.Message
{
    public interface IMessage
    {
        void WriteTo(BinaryWriter writer);
        void ReadFrom(BinaryReader reader);
    }

    public class EnterWorldMsg : IMessage
    {
        public const int TypeId = 1000;
        public string Name { get; set; }
        
        public EnterWorldMsg()
        {
        }
        
        public EnterWorldMsg(string name)
        {
            Name = name;
        }
        
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(TypeId);
            if (string.IsNullOrEmpty(Name)) writer.Write(0);
            else
            {
                var bytes = Encoding.UTF8.GetBytes(Name);
                writer.Write(bytes.Length);
                writer.Write(bytes);
            }
        }
        
        public void ReadFrom(BinaryReader reader)
        {
            {
                var length = reader.ReadInt32();
                var bytes = reader.ReadBytes(length);
                Name = Encoding.UTF8.GetString(bytes);
            }
        }
    }
    
    public class SpawnMsg : IMessage
    {
        public const int TypeId = 1001;
        public int Id { get; set; }
        public string Name { get; set; }
        public CharacterResourceMsg CharacterResource { get; set; }
        public UpdatePositionMsg UpdatePosition { get; set; }
        public UpdateHpMsg UpdateHp { get; set; }
        
        public SpawnMsg()
        {
            CharacterResource = new CharacterResourceMsg();
            UpdatePosition = new UpdatePositionMsg();
            UpdateHp = new UpdateHpMsg();
        }
        
        public SpawnMsg(int id, string name, CharacterResourceMsg characterResource, UpdatePositionMsg updatePosition, UpdateHpMsg updateHp)
        {
            Id = id;
            Name = name;
            CharacterResource = characterResource;
            UpdatePosition = updatePosition;
            UpdateHp = updateHp;
        }
        
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(TypeId);
            writer.Write(Id);
            if (string.IsNullOrEmpty(Name)) writer.Write(0);
            else
            {
                var bytes = Encoding.UTF8.GetBytes(Name);
                writer.Write(bytes.Length);
                writer.Write(bytes);
            }
            CharacterResource.WriteTo(writer);
            UpdatePosition.WriteTo(writer);
            UpdateHp.WriteTo(writer);
        }
        
        public void ReadFrom(BinaryReader reader)
        {
            Id = reader.ReadInt32();
            {
                var length = reader.ReadInt32();
                var bytes = reader.ReadBytes(length);
                Name = Encoding.UTF8.GetString(bytes);
            }
            reader.ReadInt32(); // throw type-id
            CharacterResource.ReadFrom(reader);
            reader.ReadInt32(); // throw type-id
            UpdatePosition.ReadFrom(reader);
            reader.ReadInt32(); // throw type-id
            UpdateHp.ReadFrom(reader);
        }
    }
    
    public class MoveMsg : IMessage
    {
        public const int TypeId = 1002;
        public int Id { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double Dir { get; set; }
        public double Speed { get; set; }
        public double Time { get; set; }
        
        public MoveMsg()
        {
        }
        
        public MoveMsg(int id, double x, double y, double dir, double speed, double time)
        {
            Id = id;
            X = x;
            Y = y;
            Dir = dir;
            Speed = speed;
            Time = time;
        }
        
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(TypeId);
            writer.Write(Id);
            writer.Write(X);
            writer.Write(Y);
            writer.Write(Dir);
            writer.Write(Speed);
            writer.Write(Time);
        }
        
        public void ReadFrom(BinaryReader reader)
        {
            Id = reader.ReadInt32();
            X = reader.ReadDouble();
            Y = reader.ReadDouble();
            Dir = reader.ReadDouble();
            Speed = reader.ReadDouble();
            Time = reader.ReadDouble();
        }
    }
    
    public class UpdatePositionMsg : IMessage
    {
        public const int TypeId = 1003;
        public int Id { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double Dir { get; set; }
        public double Speed { get; set; }
        public double Time { get; set; }
        public bool InstanceMove { get; set; }
        
        public UpdatePositionMsg()
        {
        }
        
        public UpdatePositionMsg(int id, double x, double y, double dir, double speed, double time, bool instanceMove)
        {
            Id = id;
            X = x;
            Y = y;
            Dir = dir;
            Speed = speed;
            Time = time;
            InstanceMove = instanceMove;
        }
        
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(TypeId);
            writer.Write(Id);
            writer.Write(X);
            writer.Write(Y);
            writer.Write(Dir);
            writer.Write(Speed);
            writer.Write(Time);
            writer.Write(InstanceMove);
        }
        
        public void ReadFrom(BinaryReader reader)
        {
            Id = reader.ReadInt32();
            X = reader.ReadDouble();
            Y = reader.ReadDouble();
            Dir = reader.ReadDouble();
            Speed = reader.ReadDouble();
            Time = reader.ReadDouble();
            InstanceMove = reader.ReadBoolean();
        }
    }
    
    public class DespawnMsg : IMessage
    {
        public const int TypeId = 1004;
        public int Id { get; set; }
        
        public DespawnMsg()
        {
        }
        
        public DespawnMsg(int id)
        {
            Id = id;
        }
        
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(TypeId);
            writer.Write(Id);
        }
        
        public void ReadFrom(BinaryReader reader)
        {
            Id = reader.ReadInt32();
        }
    }
    
    public class WorldInfoMsg : IMessage
    {
        public const int TypeId = 1005;
        public int Id { get; set; }
        public int WorldId { get; set; }
        public double ServerNow { get; set; }
        public List<SpawnMsg> SpawnList { get; private set; }
        
        public WorldInfoMsg()
        {
            SpawnList = new List<SpawnMsg>();
        }
        
        public WorldInfoMsg(int id, int worldId, double serverNow, List<SpawnMsg> spawnList)
        {
            Id = id;
            WorldId = worldId;
            ServerNow = serverNow;
            SpawnList = spawnList;
        }
        
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(TypeId);
            writer.Write(Id);
            writer.Write(WorldId);
            writer.Write(ServerNow);
            writer.Write(SpawnList.Count);
            foreach (var each in SpawnList) each.WriteTo(writer);
        }
        
        public void ReadFrom(BinaryReader reader)
        {
            Id = reader.ReadInt32();
            WorldId = reader.ReadInt32();
            ServerNow = reader.ReadDouble();
            {
                var count = reader.ReadInt32();
                SpawnList.AddRange(Enumerable.Range(0, count).Select(_ => new SpawnMsg()));
                foreach (var each in SpawnList)
                {
                    reader.ReadInt32(); // throw type-id
                    each.ReadFrom(reader);
                }
            }
        }
    }
    
    public class ChatMsg : IMessage
    {
        public const int TypeId = 1006;
        public int Id { get; set; }
        public string Name { get; set; }
        public string Message { get; set; }
        
        public ChatMsg()
        {
        }
        
        public ChatMsg(int id, string name, string message)
        {
            Id = id;
            Name = name;
            Message = message;
        }
        
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(TypeId);
            writer.Write(Id);
            if (string.IsNullOrEmpty(Name)) writer.Write(0);
            else
            {
                var bytes = Encoding.UTF8.GetBytes(Name);
                writer.Write(bytes.Length);
                writer.Write(bytes);
            }
            if (string.IsNullOrEmpty(Message)) writer.Write(0);
            else
            {
                var bytes = Encoding.UTF8.GetBytes(Message);
                writer.Write(bytes.Length);
                writer.Write(bytes);
            }
        }
        
        public void ReadFrom(BinaryReader reader)
        {
            Id = reader.ReadInt32();
            {
                var length = reader.ReadInt32();
                var bytes = reader.ReadBytes(length);
                Name = Encoding.UTF8.GetString(bytes);
            }
            {
                var length = reader.ReadInt32();
                var bytes = reader.ReadBytes(length);
                Message = Encoding.UTF8.GetString(bytes);
            }
        }
    }
    
    public class CharacterResourceMsg : IMessage
    {
        public const int TypeId = 1007;
        public int Id { get; set; }
        public int ResourceId { get; set; }
        
        public CharacterResourceMsg()
        {
        }
        
        public CharacterResourceMsg(int id, int resourceId)
        {
            Id = id;
            ResourceId = resourceId;
        }
        
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(TypeId);
            writer.Write(Id);
            writer.Write(ResourceId);
        }
        
        public void ReadFrom(BinaryReader reader)
        {
            Id = reader.ReadInt32();
            ResourceId = reader.ReadInt32();
        }
    }
    
    public class InteractMsg : IMessage
    {
        public const int TypeId = 1008;
        public int InteractId { get; set; }
        
        public InteractMsg()
        {
        }
        
        public InteractMsg(int interactId)
        {
            InteractId = interactId;
        }
        
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(TypeId);
            writer.Write(InteractId);
        }
        
        public void ReadFrom(BinaryReader reader)
        {
            InteractId = reader.ReadInt32();
        }
    }
    
    public class UpdateHpMsg : IMessage
    {
        public const int TypeId = 1009;
        public int Id { get; set; }
        public int MaxHp { get; set; }
        public int Hp { get; set; }
        
        public UpdateHpMsg()
        {
        }
        
        public UpdateHpMsg(int id, int maxHp, int hp)
        {
            Id = id;
            MaxHp = maxHp;
            Hp = hp;
        }
        
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(TypeId);
            writer.Write(Id);
            writer.Write(MaxHp);
            writer.Write(Hp);
        }
        
        public void ReadFrom(BinaryReader reader)
        {
            Id = reader.ReadInt32();
            MaxHp = reader.ReadInt32();
            Hp = reader.ReadInt32();
        }
    }
    
    public class AlertMsg : IMessage
    {
        public const int TypeId = 1010;
        public string Message { get; set; }
        
        public AlertMsg()
        {
        }
        
        public AlertMsg(string message)
        {
            Message = message;
        }
        
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(TypeId);
            if (string.IsNullOrEmpty(Message)) writer.Write(0);
            else
            {
                var bytes = Encoding.UTF8.GetBytes(Message);
                writer.Write(bytes.Length);
                writer.Write(bytes);
            }
        }
        
        public void ReadFrom(BinaryReader reader)
        {
            {
                var length = reader.ReadInt32();
                var bytes = reader.ReadBytes(length);
                Message = Encoding.UTF8.GetString(bytes);
            }
        }
    }
    
    public class VoiceMsg : IMessage
    {
        public const int TypeId = 3000;
        public int Id { get; set; }
        public byte[] Mp3 { get; set; }
        
        public VoiceMsg()
        {
        }
        
        public VoiceMsg(int id, byte[] mp3)
        {
            Id = id;
            Mp3 = mp3;
        }
        
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(TypeId);
            writer.Write(Id);
            writer.Write(Mp3.Length);
            writer.Write(Mp3);
        }
        
        public void ReadFrom(BinaryReader reader)
        {
            Id = reader.ReadInt32();
            {
                var length = reader.ReadInt32();
                Mp3 = reader.ReadBytes(length);
            }
        }
    }
    
    public class ServerMsg : IMessage
    {
        public const int TypeId = 4000;
        public string Host { get; set; }
        public int Port { get; set; }
        public string Name { get; set; }
        
        public ServerMsg()
        {
        }
        
        public ServerMsg(string host, int port, string name)
        {
            Host = host;
            Port = port;
            Name = name;
        }
        
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(TypeId);
            if (string.IsNullOrEmpty(Host)) writer.Write(0);
            else
            {
                var bytes = Encoding.UTF8.GetBytes(Host);
                writer.Write(bytes.Length);
                writer.Write(bytes);
            }
            writer.Write(Port);
            if (string.IsNullOrEmpty(Name)) writer.Write(0);
            else
            {
                var bytes = Encoding.UTF8.GetBytes(Name);
                writer.Write(bytes.Length);
                writer.Write(bytes);
            }
        }
        
        public void ReadFrom(BinaryReader reader)
        {
            {
                var length = reader.ReadInt32();
                var bytes = reader.ReadBytes(length);
                Host = Encoding.UTF8.GetString(bytes);
            }
            Port = reader.ReadInt32();
            {
                var length = reader.ReadInt32();
                var bytes = reader.ReadBytes(length);
                Name = Encoding.UTF8.GetString(bytes);
            }
        }
    }
    
    public class RequestServerMsg : IMessage
    {
        public const int TypeId = 4001;
        
        public RequestServerMsg()
        {
        }
        
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(TypeId);
        }
        
        public void ReadFrom(BinaryReader reader)
        {
        }
    }
    
    public class ServersMsg : IMessage
    {
        public const int TypeId = 4002;
        public List<ServerMsg> ServerList { get; private set; }
        
        public ServersMsg()
        {
            ServerList = new List<ServerMsg>();
        }
        
        public ServersMsg(List<ServerMsg> serverList)
        {
            ServerList = serverList;
        }
        
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(TypeId);
            writer.Write(ServerList.Count);
            foreach (var each in ServerList) each.WriteTo(writer);
        }
        
        public void ReadFrom(BinaryReader reader)
        {
            {
                var count = reader.ReadInt32();
                ServerList.AddRange(Enumerable.Range(0, count).Select(_ => new ServerMsg()));
                foreach (var each in ServerList)
                {
                    reader.ReadInt32(); // throw type-id
                    each.ReadFrom(reader);
                }
            }
        }
    }
    
    public class InterChatLoginMsg : IMessage
    {
        public const int TypeId = 4003;
        public string Name { get; set; }
        
        public InterChatLoginMsg()
        {
        }
        
        public InterChatLoginMsg(string name)
        {
            Name = name;
        }
        
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(TypeId);
            if (string.IsNullOrEmpty(Name)) writer.Write(0);
            else
            {
                var bytes = Encoding.UTF8.GetBytes(Name);
                writer.Write(bytes.Length);
                writer.Write(bytes);
            }
        }
        
        public void ReadFrom(BinaryReader reader)
        {
            {
                var length = reader.ReadInt32();
                var bytes = reader.ReadBytes(length);
                Name = Encoding.UTF8.GetString(bytes);
            }
        }
    }
    
    public class InterChatMsg : IMessage
    {
        public const int TypeId = 4004;
        public string Name { get; set; }
        public string Message { get; set; }
        public long Ticks { get; set; }
        
        public InterChatMsg()
        {
        }
        
        public InterChatMsg(string name, string message, long ticks)
        {
            Name = name;
            Message = message;
            Ticks = ticks;
        }
        
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(TypeId);
            if (string.IsNullOrEmpty(Name)) writer.Write(0);
            else
            {
                var bytes = Encoding.UTF8.GetBytes(Name);
                writer.Write(bytes.Length);
                writer.Write(bytes);
            }
            if (string.IsNullOrEmpty(Message)) writer.Write(0);
            else
            {
                var bytes = Encoding.UTF8.GetBytes(Message);
                writer.Write(bytes.Length);
                writer.Write(bytes);
            }
            writer.Write(Ticks);
        }
        
        public void ReadFrom(BinaryReader reader)
        {
            {
                var length = reader.ReadInt32();
                var bytes = reader.ReadBytes(length);
                Name = Encoding.UTF8.GetString(bytes);
            }
            {
                var length = reader.ReadInt32();
                var bytes = reader.ReadBytes(length);
                Message = Encoding.UTF8.GetString(bytes);
            }
            Ticks = reader.ReadInt64();
        }
    }
    
    public class InterChatCommandMsg : IMessage
    {
        public const int TypeId = 4005;
        public int TypeCode { get; set; }
        public string Content { get; set; }
        
        public InterChatCommandMsg()
        {
        }
        
        public InterChatCommandMsg(int typeCode, string content)
        {
            TypeCode = typeCode;
            Content = content;
        }
        
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(TypeId);
            writer.Write(TypeCode);
            if (string.IsNullOrEmpty(Content)) writer.Write(0);
            else
            {
                var bytes = Encoding.UTF8.GetBytes(Content);
                writer.Write(bytes.Length);
                writer.Write(bytes);
            }
        }
        
        public void ReadFrom(BinaryReader reader)
        {
            TypeCode = reader.ReadInt32();
            {
                var length = reader.ReadInt32();
                var bytes = reader.ReadBytes(length);
                Content = Encoding.UTF8.GetString(bytes);
            }
        }
    }
    
    public class AlivePingMsg : IMessage
    {
        public const int TypeId = 4006;
        
        public AlivePingMsg()
        {
        }
        
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(TypeId);
        }
        
        public void ReadFrom(BinaryReader reader)
        {
        }
    }
    
    public class AlivePongMsg : IMessage
    {
        public const int TypeId = 4007;
        
        public AlivePongMsg()
        {
        }
        
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(TypeId);
        }
        
        public void ReadFrom(BinaryReader reader)
        {
        }
    }
    
    public static class MessageFactory
    {
        public static IMessage Create(int typeId)
        {
            switch (typeId)
            {
                case 1000: return new EnterWorldMsg();
                case 1001: return new SpawnMsg();
                case 1002: return new MoveMsg();
                case 1003: return new UpdatePositionMsg();
                case 1004: return new DespawnMsg();
                case 1005: return new WorldInfoMsg();
                case 1006: return new ChatMsg();
                case 1007: return new CharacterResourceMsg();
                case 1008: return new InteractMsg();
                case 1009: return new UpdateHpMsg();
                case 1010: return new AlertMsg();
                case 3000: return new VoiceMsg();
                case 4000: return new ServerMsg();
                case 4001: return new RequestServerMsg();
                case 4002: return new ServersMsg();
                case 4003: return new InterChatLoginMsg();
                case 4004: return new InterChatMsg();
                case 4005: return new InterChatCommandMsg();
                case 4006: return new AlivePingMsg();
                case 4007: return new AlivePongMsg();
            }
            return null;
        }
    }
}

