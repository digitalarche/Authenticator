﻿using System;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.Core;
using Windows.Storage.Streams;
using Domain.Extensions;
using Domain.Utilities;
using Domain.Protocols;

namespace Domain
{
    public class OTP
    {
        private CryptographicKey cKey;

        public OTP(string key)
        {
            MacAlgorithmProvider provider = MacAlgorithmProvider.OpenAlgorithm(MacAlgorithmNames.HmacSha1);

            IBuffer keyMaterial = CryptographicBuffer.CreateFromByteArray(key.ToBytesBase32());
            cKey = provider.CreateKey(keyMaterial);
        }

        private byte[] HMACSHA1(byte[] value)
        {
            byte[] hash;

            IBuffer data = CryptographicBuffer.CreateFromByteArray(value);
            IBuffer buffer = CryptographicEngine.Sign(cKey, data);

            string signature = CryptographicBuffer.EncodeToHexString(buffer);

            CryptographicBuffer.CopyToByteArray(buffer, out hash);
            return hash;
        }

        private byte[] Reverse(byte[] source)
        {
            Array.Reverse(source);

            return source;
        }

        public string Code
        {
            get
            {
                return Generate();
            }
        }

        public bool IsValid(string totp)
        {
            return totp.Equals(Generate());
        }

        private string Generate()
        {
            byte[] code = BitConverter.GetBytes(TOTPUtilities.TimeSource / TOTP.INTERVAL);

            if (BitConverter.IsLittleEndian)
                code = Reverse(code);

            byte[] hash = HMACSHA1(code);

            // the last 4 bits of the mac say where the code starts (e.g. if last 4 bit are 1100, we start at byte 12)
            int start = hash[19] & 0x0f;

            // extract those 4 bytes
            byte[] bytes = new byte[4];
            Array.Copy(hash, start, bytes, 0, 4);

            if (BitConverter.IsLittleEndian)
                bytes = Reverse(bytes);

            uint fullcode = BitConverter.ToUInt32(bytes, 0) & 0x7fffffff;

            // we use the last x DIGITS of this code in radix 10
            uint codemask = (uint)Math.Pow(10, TOTP.DIGITS);

            string totp = (fullcode % codemask).ToString();

            // .NETmf has no required format string
            while (totp.Length != TOTP.DIGITS)
                totp = "0" + totp;

            return totp;
        }
    }
}
