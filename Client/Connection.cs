﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Windows;

namespace Client
{
    
    class Connection
    {
        private UdpClient _udpClient;
        private TcpClient _tcpClient;
        private IPEndPoint _ip;
        private ECDiffieHellmanCng _exch;
        private byte[] _publicKey;

        public Connection()
        {
            _ip = new IPEndPoint(IPAddress.Parse("192.168.168.132"), 3000);
            _udpClient = new UdpClient();
            _tcpClient = new TcpClient();
            _udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _udpClient.DontFragment = true;
            _udpClient.Connect(_ip);

            _exch = new ECDiffieHellmanCng(256);
            _exch.KeyDerivationFunction = ECDiffieHellmanKeyDerivationFunction.Hash;
            _exch.HashAlgorithm = CngAlgorithm.Sha256;
            _publicKey = _exch.PublicKey.ToByteArray();
            _tcpClient.NoDelay = true;
        }

        public void SendClipboard(byte[] data)
        {
            if (data != null)
            {
                StreamWriter stream = new StreamWriter(_tcpClient.GetStream());
                stream.WriteLine("2");
                stream.Flush();
                byte[] len = BitConverter.GetBytes(data.Length);
                _tcpClient.GetStream().Write(len,0,len.Length);
                _tcpClient.GetStream().Write(data,0,data.Length);
            }
           
        }


        public Object GetClipboard()
        {
            StreamWriter stream = new StreamWriter(_tcpClient.GetStream());
            stream.WriteLine("1");
            stream.Flush();
            byte [] len = new byte[sizeof(int)];
            _tcpClient.GetStream().Read(len, 0, sizeof (int));
            int length = BitConverter.ToInt32(len, 0);
            int read = 0;
            if (length <= 0)
                return null;
            byte [] data = new byte[length];
            while (read < length)
            {
                read += _tcpClient.GetStream().Read(data, read, length - read);
            }
            using (var memStream = new MemoryStream())
            {
                var binForm = new BinaryFormatter();
                memStream.Write(data, 0, data.Length);
                memStream.Seek(0, SeekOrigin.Begin);
                var obj = binForm.Deserialize(memStream);
                return obj;
            }
        }


        public bool TcpConnectAndLogin(string username, string domain, string password)
        {
                _tcpClient.Connect(_ip);
                _tcpClient.GetStream().Write(_publicKey,0,_publicKey.Length);
                byte[] serverPublicKey = new byte[72];
                _tcpClient.GetStream().Read(serverPublicKey, 0, 72);
                byte[] derivedKey = _exch.DeriveKeyMaterial(CngKey.Import(serverPublicKey, CngKeyBlobFormat.EccPublicBlob));
                StreamWriter stream = new StreamWriter(_tcpClient.GetStream());
                stream.WriteLine("0");
                stream.Flush();
                stream.WriteLine(username);
                stream.Flush();
                stream.WriteLine(domain);
                stream.Flush();
                Aes aes = new AesCryptoServiceProvider();
                aes.Key = derivedKey;
                byte[] bytes = new byte[aes.BlockSize/8];
                bytes.Initialize();
                System.Buffer.BlockCopy(username.ToCharArray(), 0, bytes, 0, bytes.Length > username.Length * sizeof(char) ? username.Length * sizeof(char): bytes.Length);
                aes.IV = bytes;
                ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
                MemoryStream ms = new MemoryStream(64);
                CryptoStream csEncrypt = new CryptoStream(ms, encryptor, CryptoStreamMode.Write);
                byte[] passArr = Encoding.UTF8.GetBytes(password);
                csEncrypt.Write(passArr,0,passArr.Length);
                csEncrypt.Close();
                byte[] tosend = ms.ToArray();
                
                _tcpClient.GetStream().Write(tosend,0,tosend.Length);
                byte [] auth = new byte[sizeof(bool)];
                _tcpClient.GetStream().Read(auth, 0, sizeof (bool));
                return BitConverter.ToBoolean(auth, 0);
        }

        public void Send(byte[] toSend, bool keyboard)
        {
            try
            {
                byte[] sending = new byte[toSend.Length + sizeof (bool)];
                byte[] isKeybd = BitConverter.GetBytes(keyboard);
                isKeybd.CopyTo(sending, 0);
                toSend.CopyTo(sending, sizeof (bool));
                _udpClient.SendAsync(sending, sending.Length);
                //Console.WriteLine("sent");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

      
    }
}
