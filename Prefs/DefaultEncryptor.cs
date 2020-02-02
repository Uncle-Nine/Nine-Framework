﻿using System;
using System.Text;
using System.Security.Cryptography;
namespace Framework.Prefs
{

    public class DefaultEncryptor : IEncryptor
    {
        private const int IV_SIZE = 16;
        private static readonly byte[] DEFAULT_IV;
        private static readonly byte[] DEFAULT_KEY;
        private RijndaelManaged cipher;

        private byte[] iv = null;
        private byte[] key = null;

        static DefaultEncryptor()
        {
            DEFAULT_IV = Encoding.ASCII.GetBytes("5CyM5tcL3yDFiWlN");
            DEFAULT_KEY = Encoding.ASCII.GetBytes("W8fnmqMynlTJXPM1");
        }

        /// <summary>
        /// 
        /// </summary>
        public DefaultEncryptor() : this(null, null)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="iv"></param>
        public DefaultEncryptor(byte[] key, byte[] iv)
        {
            if (iv == null)
                this.iv = DEFAULT_IV;

            if (key == null)
                this.key = DEFAULT_KEY;

            CheckIV(this.iv);
            CheckKey(this.key);

#if NETFX_CORE
            SymmetricKeyAlgorithmProvider provider = SymmetricKeyAlgorithmProvider.OpenAlgorithm(SymmetricAlgorithmNames.AesCbcPkcs7);
            cryptographicKey = provider.CreateSymmetricKey(this.key.AsBuffer());
#else
            cipher = new RijndaelManaged()
            {
                Mode = CipherMode.CBC,//use CBC
                Padding = PaddingMode.PKCS7,//default PKCS7
                KeySize = 128,//default 256
                BlockSize = 128,//default 128
                FeedbackSize = 128      //default 128
            };
#endif
        }
        
        protected bool CheckKey(byte[] bytes)
        {
            if (bytes == null || (bytes.Length != 16 && bytes.Length != 24 && bytes.Length != 32))
                throw new ArgumentException("The 'Key' must be 16byte 24byte or 32byte!");
            return true;
        }
        
        protected bool CheckIV(byte[] bytes)
        {
            if (bytes == null || bytes.Length != IV_SIZE)
                throw new ArgumentException("The 'IV' must be 16byte!");
            return true;
        }
        
        public byte[] Encode(byte[] plainData)
        {
#if NETFX_CORE
            IBuffer bufferEncrypt = CryptographicEngine.Encrypt(cryptographicKey, plainData.AsBuffer(), iv.AsBuffer());
            return bufferEncrypt.ToArray();
#else
            ICryptoTransform encryptor = cipher.CreateEncryptor(key, iv);
            return encryptor.TransformFinalBlock(plainData, 0, plainData.Length);
#endif
        }
        
        public byte[] Decode(byte[] cipherData)
        {
#if NETFX_CORE
            IBuffer bufferDecrypt = CryptographicEngine.Decrypt(cryptographicKey, cipherData.AsBuffer(), iv.AsBuffer());
            return bufferDecrypt.ToArray();
#else
            ICryptoTransform decryptor = cipher.CreateDecryptor(key, iv);
            return decryptor.TransformFinalBlock(cipherData, 0, cipherData.Length);
#endif
        }
    }
}