﻿// BAMWallet by Matthew Hellyer is licensed under CC BY-NC-ND 4.0. 
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using ProtoBuf;

namespace BAMWallet.Model
{
    [ProtoContract]
    public class SendPayment
    {
        [ProtoMember(1)]
        public double Amount { get; set; }
        [ProtoMember(2)]
        public string Address { get; set; }
        [ProtoMember(3)]
        public Credentials Credentials { get; set; }
        [ProtoMember(4)]
        public double Fee { get; set; }
        [ProtoMember(5)]
        public string Memo { get; set; }
        [ProtoMember(6)]
        public SessionType SessionType { get; set; }
    }
}
