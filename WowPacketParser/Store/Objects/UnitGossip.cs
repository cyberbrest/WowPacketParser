using PacketParser.SQL;

namespace PacketDumper.DataStructures
{
    [DBTableName("creature_template")]
    public class UnitGossip
    {
        [DBFieldName("gossip_menu_id")]
        public uint GossipId;
    }
}
