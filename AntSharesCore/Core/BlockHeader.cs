﻿using AntShares.Cryptography;
using AntShares.IO;
using System;
using System.IO;

namespace AntShares.Core
{
    public class BlockHeader : IEquatable<BlockHeader>, ISignable
    {
        public const uint Version = Block.Version;
        public UInt256 PrevBlock;
        public UInt256 MerkleRoot;
        public uint Timestamp;
        public const uint Bits = Block.Bits;
        public ulong Nonce;
        public UInt160 NextMiner;
        public byte[] Script;

        [NonSerialized]
        private UInt256 _hash = null;
        public UInt256 Hash
        {
            get
            {
                if (_hash == null)
                {
                    _hash = new UInt256(this.ToArray().Sha256().Sha256());
                }
                return _hash;
            }
        }

        byte[][] ISignable.Scripts
        {
            get
            {
                return new byte[][] { Script };
            }
            set
            {
                if (value.Length != 1)
                    throw new ArgumentException();
                Script = value[0];
            }
        }

        void ISerializable.Deserialize(BinaryReader reader)
        {
            ((ISignable)this).DeserializeUnsigned(reader);
            this.Script = reader.ReadBytes((int)reader.ReadVarInt());
        }

        void ISignable.DeserializeUnsigned(BinaryReader reader)
        {
            if (reader.ReadUInt32() != Version)
                throw new FormatException();
            this.PrevBlock = reader.ReadSerializable<UInt256>();
            this.MerkleRoot = reader.ReadSerializable<UInt256>();
            this.Timestamp = reader.ReadUInt32();
            if (reader.ReadUInt32() != Bits)
                throw new FormatException();
            this.Nonce = reader.ReadUInt64();
            this.NextMiner = reader.ReadSerializable<UInt160>();
        }

        public bool Equals(BlockHeader other)
        {
            if (ReferenceEquals(this, other)) return true;
            if (ReferenceEquals(null, other)) return false;
            return Hash.Equals(other.Hash);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as BlockHeader);
        }

        public static BlockHeader FromTrimmedData(byte[] data, int index)
        {
            using (MemoryStream ms = new MemoryStream(data, index, data.Length - index, false))
            using (BinaryReader reader = new BinaryReader(ms))
            {
                return reader.ReadSerializable<BlockHeader>();
            }
        }

        public override int GetHashCode()
        {
            return Hash.GetHashCode();
        }

        UInt160[] ISignable.GetScriptHashesForVerifying()
        {
            if (PrevBlock == UInt256.Zero)
                return new UInt160[] { NextMiner };
            BlockHeader prev_header = Blockchain.Default.GetHeader(PrevBlock);
            if (prev_header == null) throw new InvalidOperationException();
            return new UInt160[] { prev_header.NextMiner };
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            ((ISignable)this).SerializeUnsigned(writer);
            writer.WriteVarInt(Script.Length); writer.Write(Script);
        }

        void ISignable.SerializeUnsigned(BinaryWriter writer)
        {
            writer.Write(Version);
            writer.Write(PrevBlock);
            writer.Write(MerkleRoot);
            writer.Write(Timestamp);
            writer.Write(Bits);
            writer.Write(Nonce);
            writer.Write(NextMiner);
        }

        public VerificationResult Verify()
        {
            if (Hash == Blockchain.GenesisBlock.Hash) return VerificationResult.OK;
            if (!Blockchain.Default.ContainsBlock(PrevBlock))
                return VerificationResult.LackOfInformation;
            return this.VerifySignature();
        }
    }
}
