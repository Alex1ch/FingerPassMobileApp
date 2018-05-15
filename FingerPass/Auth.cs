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
using System.Threading.Tasks;
using Android.Hardware.Fingerprints;
using Android.Support.V4.Hardware.Fingerprint;
using static Android.Hardware.Fingerprints.FingerprintManager;

namespace FingerPass
{
    [Activity(Label = "FingerPass")]
    public class Auth : Activity
    {
        string login;
        RSACryptoServiceProvider rsa_device;
        RSACryptoServiceProvider rsa_server;
        KeyStore keyStore;
        string output;
        static string keyStoreAESReplace = "FingerPass.AES.";
        FingerprintManagerCompat.CryptoObject cryptoObject;
        TextView message;
        bool active;
        Button auth;


        public bool Active { get => active; set => active = value; }
        public RSACryptoServiceProvider Rsa_device { get => rsa_device; set => rsa_device = value; }
        public RSACryptoServiceProvider Rsa_server { get => rsa_server; set => rsa_server = value; }
        public string Output { get => output; set => output = value; }
        public TextView Message { get => message; set => message = value; }
        public Button AuthButton { get => auth; set => auth = value; }

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

        Cipher GetCipher()
        {
            keyStore = KeyStore.GetInstance("AndroidKeyStore");
            keyStore.Load(null);
            if (!keyStore.IsKeyEntry(keyStoreAESReplace + login))
            {
                Output = "Store doesn't contain key";
                return null;
            }

            var key = keyStore.GetKey(keyStoreAESReplace + login, null);

            Cipher cipher = Cipher.GetInstance("AES/CBC/Pkcs7Padding");
            var device_iv = ReadFromFile("aes.iv");
            cipher.Init(Javax.Crypto.CipherMode.DecryptMode, key, new IvParameterSpec(device_iv));

            return cipher;
        }

        bool GetRSACipher()
        {
            try
            {
                var cipher = cryptoObject.Cipher;

                var key = keyStore.GetKey(keyStoreAESReplace + login, null);
                
                var rsa_enc_key = ReadFromFile("rsa.enc");
                
                var rsa_decrypted = cipher.DoFinal(rsa_enc_key);

                var rsa_decrypted_string = Encoding.UTF8.GetString(rsa_decrypted);

                string[] splits = rsa_decrypted_string.Split(new string[] {"<SPLIT>"},StringSplitOptions.None);
                
                Rsa_server = new RSACryptoServiceProvider();
                Rsa_server.FromXmlString(splits[1]);

                Rsa_device = new RSACryptoServiceProvider();
                Rsa_device.FromXmlString(splits[0]);

                cipher.Dispose();
                return true;
            }
            catch(Exception e){
                Output = "Error in getting cipher object, try to reassign device\nException"+e.Message + "\n" + e.InnerException;
                return false;
            }
        }


        public bool Authenticate() {
            TcpClient client = new TcpClient();

            try
            {
                client.Connect("fingerpass.ru", 6284);
            }
            catch
            {
                Output = "Error:\nCan't connect to server";
                client.Close();
                return false;
            }
            SslStreamRW sslStreamRW = new SslStreamRW(client, "fingerpass.ru");

            try
            {
                sslStreamRW.WriteString("<AUTH>");
                sslStreamRW.WriteString(login);
            }
            catch (Exception e)
            {
                Output += "Error in sending message. Exception: " + e.Message;
                sslStreamRW.Disconnect("Error in sending message. Exception: " + e.Message);
                return false;
            }
            
            if (!GetRSACipher())
            {
                Output = "Can't decrypt Key";
                sslStreamRW.Disconnect("Device can't decrypt key");
                cryptoObject.Dispose();
                return false;
            }
            cryptoObject.Dispose();

            byte[] encrypted_from_server, encrypted_to_server;
            if (!sslStreamRW.ReadBytes(out encrypted_from_server)) { Output = "Error\n" + sslStreamRW.DisconnectionReason; sslStreamRW.DisconnectNoMessage(); return false; }

            try
            {
                Output = new BigInteger(encrypted_from_server).ToString() + "\n";

                byte[] decrypted_from_server = Rsa_device.Decrypt(encrypted_from_server, true);

                Rsa_device.Dispose();

                Output += new BigInteger(decrypted_from_server).ToString();

                encrypted_to_server = Rsa_server.Encrypt(decrypted_from_server, true);

                Rsa_server.Dispose();
            }
            catch (Exception e)
            {
                Output += "Device can't decrypt message. Exception: " + e.Message;
                sslStreamRW.Disconnect("Device can't decrypt message. Exception: " + e.Message);
                Rsa_server.Dispose();
                Rsa_device.Dispose();
                return false;
            }


            if (!sslStreamRW.WriteBytes(encrypted_to_server)) { Output = "Error:\nCan't send data to server"; sslStreamRW.Disconnect(); return false; }

            string result;
            if (!sslStreamRW.ReadString(out result)) { Output = "Error\n" + sslStreamRW.DisconnectionReason; sslStreamRW.Disconnect(); return false; }

            if (result=="<APPROVED>") {
                Output = "Authenticated!";
                sslStreamRW.Disconnect();
            }

            return true;
        }


        protected override void OnCreate(Bundle savedInstanceState)
        {
            active = true;
            

            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.Auth);

            AuthButton = FindViewById<Button>(Resource.Id.Send_auth);
            Message = FindViewById<TextView>(Resource.Id.Message);
            AuthButton.Enabled = false;

            string filename = "username";
            var documentsPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal);
            var filePath = Path.Combine(documentsPath, filename);

            using (FileStream fs = File.Open(filePath, FileMode.Open))
            {
                try
                {
                    StreamReader sr = new StreamReader(fs);
                    login = sr.ReadLine();
                }
                catch
                {
                    Output = "Can't parse username, probably, device not assign";
                    AuthButton.Activated = false;
                    return;
                }
            }
            
            FingerprintManagerCompat fingerPrintManager = FingerprintManagerCompat.From(this);
            FingerprintManagerCompat.AuthenticationCallback authenticationCallback = new AuthCallback(this);
            FingerprintManagerCompat fingerprintManager = FingerprintManagerCompat.From(this);
            var cancellationSignal = new Android.Support.V4.OS.CancellationSignal();
            
            var cipher = GetCipher();

            if (cipher == null)
            {
                Message.Text = "Can't get aes key cipher";
            }

            cryptoObject = new FingerprintManagerCompat.CryptoObject(cipher);

            Message.Text = "Place your fingertip on the fingerprint scanner to verify your identity";
            fingerprintManager.Authenticate(cryptoObject, 0, cancellationSignal, authenticationCallback, null);

            AuthButton.Click += delegate {
                if (active) return;
                AuthButton.Enabled = false;

                using (FileStream fs = File.Open(filePath, FileMode.Open))
                {
                    try
                    {
                        StreamReader sr = new StreamReader(fs);
                        login = sr.ReadLine();
                    }
                    catch
                    {
                        Output = "Can't parse username, probably, device not assign";
                        return;
                    }
                }

                fingerPrintManager = FingerprintManagerCompat.From(this);
                authenticationCallback = new AuthCallback(this);
                fingerprintManager = FingerprintManagerCompat.From(this);
                cancellationSignal = new Android.Support.V4.OS.CancellationSignal();

                cipher = GetCipher();

                if (cipher == null)
                {
                    Message.Text = "Can't get aes key cipher";
                }

                cryptoObject = new FingerprintManagerCompat.CryptoObject(cipher);

                Message.Text = "Place your fingertip on the fingerprint scanner to verify your identity";
                fingerprintManager.Authenticate(cryptoObject, 0, cancellationSignal, authenticationCallback, null);
            };
        }
        
    }
}