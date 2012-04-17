﻿using System;
using System.Data.SQLite;
using WowPacketParser.Misc;
using WowPacketParser.Enums;

namespace WowPacketParser.Loading
{
    public sealed class SQLitePacketReader : IPacketReader
    {
        readonly SQLiteConnection _connection;
        SQLiteDataReader _reader;

        public SQLitePacketReader(string fileName)
        {
            _connection = new SQLiteConnection("Data Source=" + fileName);
            _connection.Open();

            // tiawps
            // header table (`key` string primary key, value string)
            // packets table (id integer primary key autoincrement, timestamp datetime, direction integer, opcode integer, data blob)
            ReadHeader();

            using (SQLiteCommand command = _connection.CreateCommand())
            {
                command.CommandText = "SELECT opcode, timestamp, direction, data FROM packets;";
                _reader = command.ExecuteReader();
            }
        }

        void ReadHeader()
        {
            SQLiteCommand command = _connection.CreateCommand();
            command.CommandText = "SELECT key, value FROM header;";
            _reader = command.ExecuteReader();

            while (_reader.Read())
            {
                var key = _reader.GetString(0);
                var value = _reader.GetValue(1);

                if (key.ToLower() == "clientbuild")
                {
                    int build;
                    if (int.TryParse(value.ToString(), out build))
                        SetBuild(build);

                    break;
                }
            }

            _reader.Close();
        }

        static void SetBuild(int build)
        {
            if (ClientVersion.IsUndefined())
                ClientVersion.SetVersion((ClientVersionBuild)build);
        }

        public bool CanRead()
        {
            return _reader.Read();
        }

        public Packet Read(int number, string fileName)
        {
            var opcode = _reader.GetInt32(0);
            var time = _reader.GetDateTime(1);
            var direction = (Direction)_reader.GetInt32(2);
            object blob = _reader.GetValue(3);

            if (DBNull.Value.Equals(blob))
                return null;

            var data = (byte[])blob;

            using (var packet = new Packet(data, opcode, time, direction, number, fileName, null))
                return packet;
        }

        public void Dispose()
        {
            if (_reader != null)
                _reader.Close();

            if (_connection != null)
                _connection.Close();
        }
    }
}
