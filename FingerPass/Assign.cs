using Android.App;
using Android.Widget;
using Android.OS;
using Android.Content;
using Android.Hardware.Fingerprints;
using Android.Support.V4;
using Android.Support.V4.Hardware.Fingerprint;
using Android.Support.V4.Content;
using Android;
using System.Net.Security;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System;
using Android.Telephony;
using Android.Content.PM;
using Android.Support.V4.App;
using System.IO;
using Java.Security;
using Android.Security.Keystore;
using Java.Security.Interfaces;
using Java.Security.Spec;
using Java.Math;
using Javax.Crypto;
using Android.Util;
using System.Security.Cryptography;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Prng;
using Org.BouncyCastle.Crypto.Generators;
using Javax.Crypto.Spec;

namespace FingerPass
{
    [Activity(Label = "FingerPassAssign", Icon = "@mipmap/icon")]
    public class AssignActivity : Activity
    {
        static KeyGenerator keyGenerator;
        static string keyAlias;
        static string keyStoreAESReplace = "FingerPass.AES.";
        Cipher cipher;
        

        static bool WriteToFile(byte[] iv,string filename) {
            try {
                var documentsPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal);
                var filePath = Path.Combine(documentsPath, filename);
                using (FileStream fs = File.Open(filePath, FileMode.Create))
                {
                    fs.Write(iv, 0, iv.Length);
                    fs.Flush();
                }
            }
            catch {
                throw new Exception("Can't write IV to file");
            }
            return true;
        }

        static byte[] ReadFromFile(string filename) {
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

        static bool GetKeyGenerator() {
            try
            {
                keyGenerator = KeyGenerator.GetInstance("AES", "AndroidKeyStore");
                return true;
            }
            catch  (KeyStoreException e){
                e.PrintStackTrace();
                return false;
            }
        }
        
        public static AsymmetricCipherKeyPair GetKeyPair()
        {
            var randomGenerator = new CryptoApiRandomGenerator();
            var secureRandom = new Org.BouncyCastle.Security.SecureRandom(randomGenerator);
            var keyGenerationParameters = new KeyGenerationParameters(secureRandom, 2048);

            var keyPairGenerator = new RsaKeyPairGenerator();
            keyPairGenerator.Init(keyGenerationParameters);
            return keyPairGenerator.GenerateKeyPair();
        }

        string GenerateNewKey()
        {
            if (GetKeyGenerator())
            {
                try
                {
                    keyGenerator.Init(new KeyGenParameterSpec.Builder(keyStoreAESReplace+keyAlias,
                        KeyStorePurpose.Decrypt | KeyStorePurpose.Encrypt)
                            .SetEncryptionPaddings(KeyProperties.EncryptionPaddingPkcs7)
                            .SetBlockModes(KeyProperties.BlockModeCbc)
                            .SetUserAuthenticationRequired(false)
                            .Build());
                    
                    var key = keyGenerator.GenerateKey();

                    RSACryptoServiceProvider RSA = new RSACryptoServiceProvider(2048);

                    cipher = Cipher.GetInstance("AES/CBC/Pkcs7Padding");
                    cipher.Init(Javax.Crypto.CipherMode.EncryptMode, key);

                    byte[] rsa_private_key = Encoding.UTF8.GetBytes(RSA.ToXmlString(true));
                    byte[] rsa_key_encrypted = cipher.DoFinal(rsa_private_key);
                    
                    var dev_iv =cipher.GetIV();
                    WriteToFile(dev_iv, "aes.device.iv");
                    WriteToFile(rsa_key_encrypted, "rsa.device.enc");

                    cipher = Cipher.GetInstance("AES/CBC/Pkcs7Padding");
                    cipher.Init(Javax.Crypto.CipherMode.EncryptMode, key);
                    var serv_iv = cipher.GetIV();
                    WriteToFile(serv_iv, "aes.server.iv");

                    return RSA.ToXmlString(false);
                }
                catch (Exception e)
                {
                    var exception = String.Format("{0}\n{1}",e.Message,e.InnerException);
                }
            }
            return null;
        }

        private bool Assign(string login, string password, out SslStreamRW sslStreamRw, out string message)
        {
            sslStreamRw = null;
            message = "Error:\nUnknown reason";

            if (ContextCompat.CheckSelfPermission(this, Manifest.Permission.ReadPhoneState) == Android.Content.PM.Permission.Denied)
            {
                ActivityCompat.RequestPermissions(this, new string[] { Manifest.Permission.ReadPhoneState }, 1);
            }

            if (ContextCompat.CheckSelfPermission(this, Manifest.Permission.ReadPhoneState) == Android.Content.PM.Permission.Denied)
            {
                message = "Error:\nThis app require READ_PHONE_STATE permission to only get device IMEI";
                return false;
            }
            
            try
            {
                string result, server_rsa_open_key, device_rsa_open_key = "=====DEVICE RSA TEST KEY=====";
                TcpClient client = new TcpClient();
                client.SendTimeout = 10000;
                try
                {
                    client.Connect("fingerpass.ru", 6284);
                }
                catch
                {
                    message = "Error:\nCan't connect to server";
                    client.Close();
                    return false;
                }
                sslStreamRw = new SslStreamRW(client, "fingerpass.ru");


                if (!sslStreamRw.WriteString("<HANDSHAKE>")) return false;
                if (!sslStreamRw.WriteString(login)) return false;
                if (!sslStreamRw.WriteString(Build.Brand + " " + Build.Model)) return false;
                if (!sslStreamRw.WriteString(((TelephonyManager)GetSystemService(TelephonyService)).DeviceId)) { message = "Error:\nHaven't permissions"; return false; }//((TelephonyManager)GetSystemService(TelephonyService)).DeviceId)

                if (!sslStreamRw.WriteString(password)) return false;
                if (!sslStreamRw.ReadString(out server_rsa_open_key)) { message = "Error:\n" + sslStreamRw.DisconnectionReason; return false; }



                keyAlias = login;

                device_rsa_open_key = GenerateNewKey();

                if (device_rsa_open_key!=null)
                {
                    if (!sslStreamRw.WriteString(device_rsa_open_key)) return false;
                }
                else
                {
                    message = "Error:\nCan't generate RSA key";
                    sslStreamRw.Disconnect("Device can't generate RSA key");
                    return false;
                };
                
                try
                {
                    WriteToFile(cipher.DoFinal(Encoding.UTF8.GetBytes(server_rsa_open_key)), "rsa.server.enc");
                }
                catch
                {
                    message = "Error:\nCan't save server RSA key";
                    sslStreamRw.Disconnect("Device can't save server RSA key");
                    return false;
                }

                if (!sslStreamRw.ReadString(out result)) { message = "Error:\n" + sslStreamRw.DisconnectionReason; return false; }
                if (result == "<ACCEPTED>")
                {
                    string filename = "username";
                    var documentsPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal);
                    var filePath = Path.Combine(documentsPath, filename);
                    using (FileStream fs = File.Open(filePath, FileMode.Create))
                    {
                        StreamWriter sw = new StreamWriter(fs);

                        sw.WriteLine(login);
                        sw.Flush();
                    }
                    

                    message = "Device is assigned";
                    sslStreamRw.Disconnect();
                    
                    return true;
                }
                else
                {
                    sslStreamRw.DisconnectNoMessage();
                    return false;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error reading message:\n" + e.Message);
                if (e.InnerException != null)
                {
                    Console.WriteLine("Inner exception: {0}", e.InnerException.Message);
                }
                sslStreamRw.DisconnectNoMessage();
                return false;
            }
        }




        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Assign);



            // Get our button from the layout resource,
            // and attach an event to it
            Button button = FindViewById<Button>(Resource.Id.SendAssign);
            TextView info = FindViewById<TextView>(Resource.Id.InfoView);
            EditText login = FindViewById<EditText>(Resource.Id.LoginEdit);
            EditText password = FindViewById<EditText>(Resource.Id.PasswordEdit);

            info.Text = "";

            string output = "";
            info.Text += output;
            info.Text += "\nDevice is ready!";

            button.Text = "Assign device";

            button.Click += delegate {
                string assignLogin = login.Text;
                string assignPassword = password.Text;
                info.Text = "Loading...";
                //button.Enabled = false;
                Task.Factory.StartNew(() => {
                    SslStreamRW sslStreamRw;
                    string message;
                    if (Assign(assignLogin, assignPassword, out sslStreamRw, out message))
                    { }
                    info.Text = message;
                    //button.Enabled = false;
                });
            };

        }

    }
}