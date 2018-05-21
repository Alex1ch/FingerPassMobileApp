﻿using Android.App;
using Android.Widget;
using Android.OS;
using Android.Support.V4.Content;
using Android;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System;
using Android.Telephony;
using Android.Support.V4.App;
using System.IO;
using Java.Security;
using Android.Security.Keystore;
using Javax.Crypto;
using System.Security.Cryptography;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Prng;
using Org.BouncyCastle.Crypto.Generators;
using Android.Hardware.Fingerprints;
using Android.Support.V4.Hardware.Fingerprint;
using System.Security.Cryptography.X509Certificates;
using Org.BouncyCastle.X509;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Asn1;

namespace FingerPass
{
    [Activity(Label = "FingerPass", Icon = "@mipmap/icon")]
    public class AssignActivity : Activity
    {
        static KeyGenerator keyGenerator;
        static string keyAlias;
        static string keyStoreAESReplace = "FingerPass.AES.";
        Cipher cipher;
        public TextView info;
        string login;
        string password;
        bool active;

        public bool Active { get => active; set => active = value; }

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
                throw new Exception("Can't write IV to file");
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
        

        void GenerateKey()
        {
            keyGenerator.Init(new KeyGenParameterSpec.Builder(keyStoreAESReplace + keyAlias,
                KeyStorePurpose.Decrypt | KeyStorePurpose.Encrypt)
                    .SetEncryptionPaddings(KeyProperties.EncryptionPaddingPkcs7)
                    .SetBlockModes(KeyProperties.BlockModeCbc)
                    .SetUserAuthenticationRequired(false)
                    .Build());

            var key = keyGenerator.GenerateKey();

            cipher = Cipher.GetInstance("AES/CBC/Pkcs7Padding");
            cipher.Init(Javax.Crypto.CipherMode.EncryptMode, key);
        }

        string GenerateRSAKey()
        {
            if (GetKeyGenerator())
            {
                X509V1CertificateGenerator certGen = new X509V1CertificateGenerator();

                X509Name CN = new X509Name("CN=" + login + "_cert");

                Gost3410KeyPairGenerator keypairgen = new Gost3410KeyPairGenerator();
                keypairgen.Init(new KeyGenerationParameters(new Org.BouncyCastle.Security.SecureRandom(new CryptoApiRandomGenerator()), 2048));

                AsymmetricCipherKeyPair keypair = keypairgen.GenerateKeyPair();

                certGen.SetSerialNumber(BigInteger.ProbablePrime(120, new Random()));
                certGen.SetIssuerDN(CN);
                certGen.SetNotAfter(DateTime.MaxValue);
                certGen.SetNotBefore(DateTime.Now.Subtract(new TimeSpan(7, 0, 0, 0)));
                certGen.SetSubjectDN(CN);
                certGen.SetPublicKey(keypair.Public);
                certGen.SetSignatureAlgorithm("GOST3411withGOST3410");

                Org.BouncyCastle.X509.X509Certificate newCert = certGen.Generate(keypair.Private);
                
                var gost_encrypted_key = PrivateKeyFactory.EncryptKey("PBEwithSHA1andDES-CBC", login.ToCharArray(), new byte[256], 1, keypair.Private);
                
                byte[] gost_keys_encrypted = cipher.DoFinal(gost_encrypted_key);
                    
                var iv =cipher.GetIV();
                WriteToFile(iv, "aes.iv");
                
                WriteToFile(gost_keys_encrypted, "gost.private");
                
                cipher.Dispose();

                return Convert.ToBase64String(newCert.GetEncoded());
            }
            return null;
        }

        public bool Assign(out string message)
        {
            SslStreamRW sslStreamRw = null;
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
                string result,  device_open_key = "=====DEVICE RSA TEST KEY=====";
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
                
                keyAlias = login;

                try
                {
                    device_open_key = GenerateRSAKey();
                }
                catch(Exception e)
                {
                    message = "Error:\nCan't generate RSA key\nException: "+e.Message+"\n"+e.InnerException;
                    sslStreamRw.Disconnect("Device can't generate RSA key");
                    return false;
                }
                
                if (device_open_key!=null)
                {
                    if (!sslStreamRw.WriteString(device_open_key)) return false;
                }
                else
                {
                    message = "Error:\nCan't generate RSA key";
                    sslStreamRw.Disconnect("Device can't generate RSA key");
                    return false;
                };
                
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
            info = FindViewById<TextView>(Resource.Id.InfoView);
            EditText loginEdit = FindViewById<EditText>(Resource.Id.LoginEdit);
            EditText passwordEdit = FindViewById<EditText>(Resource.Id.PasswordEdit);

            info.Text = "";

            string output = "";
            info.Text += output;
            info.Text += "\nDevice is ready!";

            button.Text = "Assign device";

            button.Click += delegate {
                if (active) return;
                active = true;
                login = loginEdit.Text;
                password = passwordEdit.Text;
                keyAlias = login;

                if (!GetKeyGenerator()) {
                    info.Text = "Error in KeyGenerator init";
                    return;
                }
                GenerateKey();

                FingerprintManagerCompat fingerPrintManager = FingerprintManagerCompat.From(this);
                FingerprintManagerCompat.AuthenticationCallback authenticationCallback = new AssignCallback(this);
                FingerprintManagerCompat fingerprintManager = FingerprintManagerCompat.From(this);
                var cancellationSignal = new Android.Support.V4.OS.CancellationSignal();
                    
                FingerprintManagerCompat.CryptoObject crypto = new FingerprintManagerCompat.CryptoObject(cipher);

                info.Text = "Place your fingertip on the fingerprint scanner to verify your identity";
                fingerprintManager.Authenticate(crypto, 0, cancellationSignal, authenticationCallback, null);

                cipher = crypto.Cipher;
            };

        }

    }
}