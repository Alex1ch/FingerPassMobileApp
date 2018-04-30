using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using Java.Math;
using Java.Security;
using Javax.Crypto;
using Javax.Crypto.Spec;
using System.Security.Cryptography;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Encodings;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Security;

namespace FingerPass
{
    [Activity(Label = "Auth")]
    public class Auth : Activity
    {
        string login;
        RSACryptoServiceProvider rsa_device;
        RSACryptoServiceProvider rsa_server;
        KeyStore keyStore;
        string output;
        static string keyStoreAESReplace = "FingerPass.AES.";
        string serverxml;


        static byte[] ReadFromFile(string filename)
        {
            byte[] iv;
            try
            {
                var documentsPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal);
                var filePath = Path.Combine(documentsPath, filename);
                using (FileStream fs = File.Open(filePath, FileMode.Open))
                {
                    iv = new byte[fs.Length];
                    fs.Read(iv, 0, iv.Length);
                }
            }
            catch
            {
                throw new Exception("Can't read IV from file");
            }
            return iv;
        }


        bool GetCipher()
        {
            try
            {
                keyStore = KeyStore.GetInstance("AndroidKeyStore");
                keyStore.Load(null);
                if (!(keyStore.ContainsAlias(keyStoreAESReplace+login) &&keyStore.IsKeyEntry(keyStoreAESReplace+login)))
                {
                    output = "Store doesn't contain key";
                    return false;
                }

                var key = keyStore.GetKey(keyStoreAESReplace + login, null);

                Cipher cipher = Cipher.GetInstance("AES/CBC/Pkcs7Padding");
                var device_iv = ReadFromFile("aes.device.iv");
                cipher.Init(Javax.Crypto.CipherMode.DecryptMode,key, new IvParameterSpec(device_iv));

                var rsa_dev_enc_key = ReadFromFile("rsa.device.enc");
                var rsa_dev_decrypted = cipher.DoFinal(rsa_dev_enc_key);


                cipher = Cipher.GetInstance("AES/CBC/Pkcs7Padding");
                var server_iv = ReadFromFile("aes.server.iv");
                cipher.Init(Javax.Crypto.CipherMode.DecryptMode, key, new IvParameterSpec(server_iv));

                var rsa_serv_enc_key = ReadFromFile("rsa.server.enc");
                var rsa_serv_decrypted = cipher.DoFinal(rsa_serv_enc_key);


                rsa_server = new RSACryptoServiceProvider();
                rsa_server.FromXmlString(Encoding.UTF8.GetString(rsa_serv_decrypted));

                rsa_device = new RSACryptoServiceProvider();
                rsa_device.FromXmlString(Encoding.UTF8.GetString(rsa_dev_decrypted));
                
                return true;
            }
            catch(Exception e){
                output = "Error in getting cipher object";
                return false;
            }
        }


        bool Authenticate() {
            TcpClient tcpClient = new TcpClient("fingerpass.ru", 6284);
            SslStreamRW sslStreamRW = new SslStreamRW(tcpClient, "fingerpass.ru");

            sslStreamRW.WriteString("<AUTH>");
            string filename = "user.config.cfg";
            var documentsPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal);
            var filePath = Path.Combine(documentsPath, filename);

            using (FileStream fs = File.Open(filePath, FileMode.Open))
            {
                try
                {
                    StreamReader sr = new StreamReader(fs);
                    login = sr.ReadLine();
                    sslStreamRW.WriteString(login);
                }
                catch
                {
                    output = "Can't locally parse";
                    sslStreamRW.Disconnect("Device can't locally access Server key");
                    return false;
                }
            }

            if (!GetCipher())
            {
                sslStreamRW.Disconnect("Device can't get key");
                return false;
            }
            

            byte[] encrypted_from_server;
            if (!sslStreamRW.ReadBytes(out encrypted_from_server)) {  output = sslStreamRW.DisconnectionReason; return false; }

            
            try
            {

                output = new BigInteger(encrypted_from_server).ToString()+"\n";

                byte[] decrypted_from_server = rsa_device.Decrypt(encrypted_from_server,true);

                output += new BigInteger(decrypted_from_server).ToString();
                


            }
            catch(Exception e)
            {
                output += "Device can't decrypt message. Exception: " + e.Message;
                sslStreamRW.Disconnect("Device can't decrypt message. Exception: "+e.Message);
                return false;
            }
            return true;
        }


        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.Auth);

            Button auth = FindViewById<Button>(Resource.Id.Send_auth);
            TextView message = FindViewById<TextView>(Resource.Id.Message);

            auth.Click += delegate
            {
                Authenticate();
                message.Text = output;
            };
        }
    }
}