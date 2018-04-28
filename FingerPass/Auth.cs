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
using Android.Views;
using Android.Widget;
using Java.Math;
using Java.Security;
using Javax.Crypto;

namespace FingerPass
{
    [Activity(Label = "Auth")]
    public class Auth : Activity
    {
        string login;
        Cipher cipher;
        KeyStore keyStore;
        string output;

        bool GetCipher()
        {
            try
            {
                keyStore = KeyStore.GetInstance("AndroidKeyStore");
                keyStore.Load(null);
                if (!keyStore.ContainsAlias(login))
                {
                    output = "Store doesn't contain key";
                    return false;
                }
                cipher = Cipher.GetInstance("RSA/ECB/PKCS1Padding", "BC");

                
                var key = keyStore.GetCertificate(login);
                cipher.Init(CipherMode.EncryptMode, key);
                output = cipher.BlockSize.ToString() + "\n";
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

            byte[] encrypted_from_server;
            if (!sslStreamRW.ReadBytes(out encrypted_from_server)) {  output = sslStreamRW.DisconnectionReason; return false; }

            byte[] generated;
            if (!sslStreamRW.ReadBytes(out generated)) { output = sslStreamRW.DisconnectionReason; return false; }

            if (!GetCipher())
            {
                output = "Device can't get key!";
                sslStreamRW.Disconnect("Device can't get key");
                return false;
            }
            
            try
            {
                //byte[] decrypted_from_server=cipher.DoFinal(encrypted_from_server);

                output = new BigInteger(encrypted_from_server).ToString()+"\n";
                output += new BigInteger(cipher.DoFinal(generated)).ToString();
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