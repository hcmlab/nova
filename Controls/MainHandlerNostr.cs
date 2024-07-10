﻿using System;

using System.Text;
using WebSocket = WebSocketSharp.WebSocket;
using Newtonsoft.Json.Linq;
using Secp256k1Net;
using Newtonsoft.Json;

using System.Runtime.InteropServices;
using System.Collections.Generic;
using WebSocketSharp;
using System.Linq;
using ThirdParty.BouncyCastle.Utilities.IO.Pem;
using ssi.Tools;
using MathNet.Numerics.LinearAlgebra.Factorization;



namespace ssi


{
    public partial class MainHandler
    {


        public static class Secp256k1Wrapper
        {
            private const string DllName = "libsecp256k1"; // Adjust this if needed based on your OS
       

            [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
            public static extern IntPtr secp256k1_context_create(uint flags);

            [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
            public static extern void secp256k1_context_destroy(IntPtr context);

            [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
             public static extern int secp256k1_schnorrsig_sign32(IntPtr context, byte[] signature, byte[] message, IntPtr keypair, byte[] nonce);

            [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
            public static extern int secp256k1_schnorrsig_verify(IntPtr context, byte[] signature, byte[] message, int length, byte[] pubkey);


            [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
            public static extern int secp256k1_keypair_create(IntPtr context, IntPtr keypair, byte[] seckey);


            public static byte[] SignSchnorr(byte[] message, byte[] seckey, byte[] nonce)
            {
                const uint SECP256K1_CONTEXT_SIGN = (1 << 0);

                IntPtr context = secp256k1_context_create(SECP256K1_CONTEXT_SIGN);
                byte[] signature = new byte[64];

                IntPtr keypair = Marshal.AllocHGlobal(96);
 
                int res = secp256k1_keypair_create(context, keypair, seckey);
                Console.WriteLine("RES: " + res);

                int result = secp256k1_schnorrsig_sign32(context, signature, message, keypair, nonce);
                secp256k1_context_destroy(context);

                return signature;
            }

            public static bool VerifySchnorr(byte[] message, byte[] signature, byte[] pubkey)
            {   
                
                const uint SECP256K1_CONTEXT_VERIFY = 1 << 0;
                IntPtr context = secp256k1_context_create(SECP256K1_CONTEXT_VERIFY);
                if (context == IntPtr.Zero)
                    throw new Exception("Failed to create secp256k1 context for verification.");

                int result = secp256k1_schnorrsig_verify(context, signature, message, 32, pubkey);

                secp256k1_context_destroy(context);

                return result == 1;
            }


        }


        public class NostrTime
        {
    
            public static long Now() {
                return ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds();
            }

        }

        public class SchnorrSigner
            {

                public static byte[] SignMessage(byte[] messageBytes, string privateKeyHex)
                {
                    //var messageBytes = Encoding.UTF8.GetBytes(message);
                    var privateKeyBytes = HexStringToByteArray(privateKeyHex);

                    // Generate a 32-byte nonce (random or derived as needed)
                    var nonce = new byte[32];
                    new Random().NextBytes(nonce);

                    var signatureBytes = Secp256k1Wrapper.SignSchnorr(messageBytes, privateKeyBytes, nonce);
                
                    return signatureBytes;
                }

            public static bool VerifyMessage(byte[] messageBytes, byte[] signature, byte[] pubkey)
            {

                bool isValid = Secp256k1Wrapper.VerifySchnorr(messageBytes, signature, pubkey);
                Console.WriteLine(isValid);

                return isValid;
            }


            private static byte[] HexStringToByteArray(string hex)
                {
                    int NumberChars = hex.Length;
                    byte[] bytes = new byte[NumberChars / 2];
                    for (int i = 0; i < NumberChars; i += 2)
                        bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
                    return bytes;
                }


                public static byte[] getPublickey(string private_kex)
                {

                    var privateKey = HexToByteArray(private_kex);

                    var secp256k1 = new Secp256k1();
                    var publicKey = new byte[Secp256k1.PUBKEY_LENGTH];
                    secp256k1.PublicKeyCreate(publicKey, privateKey);


                    return publicKey;
                }

                public static string getPublickeyHex(string private_kex)
                {

                    var privateKey = HexToByteArray(private_kex);

                    var secp256k1 = new Secp256k1();
                    var publicKey = new byte[Secp256k1.PUBKEY_LENGTH];
                    secp256k1.PublicKeyCreate(publicKey, privateKey);



                    var serializedCompressedPublicKey = new byte[Secp256k1.SERIALIZED_COMPRESSED_PUBKEY_LENGTH];
                    secp256k1.PublicKeySerialize(serializedCompressedPublicKey, publicKey, Flags.SECP256K1_EC_COMPRESSED);



                    var hexStringPublic = BitConverter.ToString(serializedCompressedPublicKey).ToLower();
                    hexStringPublic = hexStringPublic.Replace("-", "");
                    hexStringPublic = hexStringPublic.Substring(2, hexStringPublic.Length - 2);
                    Console.WriteLine(hexStringPublic);
                    return hexStringPublic;
                }


                public static byte[] HexToByteArray(string hex)
                {
                    hex = hex.Replace(" ", "").Replace("-", "");

                    var numberChars = hex.Length;
                    var bytes = new byte[numberChars / 2];
                    for (var i = 0; i < numberChars; i += 2)
                        bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
                    return bytes;
                }


                public static KeyPairHex createKeys()
                {
                    var secp256k1 = new Secp256k1();
                    var privateKey = new byte[Secp256k1.PRIVKEY_LENGTH];
                    var rnd = System.Security.Cryptography.RandomNumberGenerator.Create();
                    do { rnd.GetBytes(privateKey); }
                    while (!secp256k1.SecretKeyVerify(privateKey));

                    var hexStringPrivate = BitConverter.ToString(privateKey).ToLower();
                    hexStringPrivate = hexStringPrivate.Replace("-", "");
                    Console.WriteLine(hexStringPrivate);

                    string hexStringPublic = SchnorrSigner.getPublickeyHex(hexStringPrivate);


                    return new KeyPairHex()
                    {
                        PrivateKeyHex = hexStringPrivate,
                        PublicKeyHex = hexStringPublic
                    };


                }

            }

        public class KeyPairHex
        {
            public string PrivateKeyHex;
            public string PublicKeyHex;
            
        }


        public struct NIP89 {
            public string name;
            public string decription;
            public string lud16;
            public string image;

             }


    public class DVM
        {
            public DVM() { }  
            
            public static NIP89 parseNIP89(string nip89content)
            {

                dynamic json = JsonConvert.DeserializeObject<dynamic>(nip89content);
                NIP89 nip89 = new NIP89();
                JValue token = json["name"];
                if (token != null) 
                {
                    nip89.name = token.Value.ToString();
                }
                token = json["about"];
                if (token != null)
                {
                    nip89.decription = token.Value.ToString();
                }
                token = json["picture"];
                if (token != null)
                {
                    nip89.image = token.Value.ToString();
                }
                token = json["image"];
                if (token != null)
                {
                    nip89.image = token.Value.ToString();
                }
                token = json["lud16"];
                if (token != null)
                {
                    nip89.lud16 = token.Value.ToString();
                }



                return nip89;
            }
        }

            public class NostrEvent
        {
            string publickey;
            long created_at;
            int kind;
            JArray tags;
            string content;

            public NostrEvent(string content, int kind, JArray tags, string publickey, long created_at = default)
            {
                if (created_at == default)
                {
                    created_at = NostrTime.Now();
                }

                this.publickey = publickey;
                this.created_at = created_at;
                this.kind = kind;
                this.tags = tags;
                this.content = content;



            }

            public JArray UnsignedEvent()
            {

                JArray evt = new JArray
                    {
                        0,
                        this.publickey,
                        this.created_at,
                        this.kind,
                        this.tags,
                        this.content
                    };

                return evt;
            }
        }

        public class NostrClient
        {

            public List<WebSocket> Relays = new List<WebSocket>();
            public NostrSigner signer;
            List<string> seenEvents = new List<string>();




            private enum SslProtocols
            {
                Tls = 192,
                Tls11 = 768,
                Tls12 = 3072
            }

            public NostrClient(NostrSigner signer)
            {
                this.signer = signer;
            }

            public void addRelay(string relay)
            {
                var ws = new WebSocket(relay);
                ws.Log.Level = LogLevel.Debug;
                var sslProtocol = (System.Security.Authentication.SslProtocols)(SslProtocols.Tls12 | SslProtocols.Tls11 | SslProtocols.Tls);
                ws.SslConfiguration.EnabledSslProtocols = sslProtocol;
                ws.OnOpen += (sender2, e2) =>
                Console.WriteLine("connected");

                ws.OnError += (sender2, e2) =>
                    Console.WriteLine(e2.Message);

                ws.OnMessage += (sender, ex) =>
                {
                   Console.Write("From relay: " + ws.Url + ": ");
                    dynamic note = extractNote(ex.Data);
                    if (note != null && !checkSeenEvents(note))
                    {
                        OnResponse(sender, note);

                    }


                };
                Relays.Add(ws);
            }

            public void connect()
            {
                foreach (WebSocket relay in Relays)
                {
                    relay.Connect();
                }

            }

            public void disconnect()
            {
                foreach (WebSocket relay in Relays)
                {
                    relay.Close();
                }

            }

            public void send(JObject signedEvent)
            {
                foreach (WebSocket relay in Relays)
                {
                    JArray array = new JArray
                    {
                        "EVENT",
                        signedEvent
                    };
                    Console.WriteLine(array.ToString());
                    relay.Send(array.ToString());
                }

            }
            public void get_events(JObject filter)
            {
                string subscriptionid = "asdjnasdlkjashdajskdhasjdasd";
                foreach (WebSocket relay in Relays)
                {
                    JArray array = new JArray
                    {
                        "REQ",
                        subscriptionid,
                        filter
                    };

                    Console.WriteLine(array.ToString());
                    relay.Send(array.ToString());
                    Console.ReadKey(true);
                }

            }

            public void get_events_of(JObject filter)
            {
                string subscriptionid = "asdjnasdlkjashdajskdhasjdasd";
                foreach (WebSocket relay in Relays)
                {
                    JArray array = new JArray
                    {
                        "REQ",
                        subscriptionid,
                        filter
                    };

                    Console.WriteLine(array.ToString());
                    relay.Send(array.ToString());
                   
                }

            }




            public dynamic extractNote(string evt)
            {
                dynamic json = JsonConvert.DeserializeObject<dynamic>(evt);
                try
                { 
                    if(json[0] == "EVENT")
                    {
                        dynamic note = JsonConvert.DeserializeObject<dynamic>(json[2].ToString());
                        return note;
                    }
                    else
                    {
                        return null;
                    }

                    //if (json[0] != "EOSE" && json[0] != "OK")
                }
                catch
                {
                    return null;
                }
               
            }

            public bool checkSeenEvents(dynamic note)
            {
                try
                {
                    //dynamic note = extractNote(evt);
                    string id = note["id"];
                    if (!seenEvents.Contains(id))
                    {
                        seenEvents.Add(id);
                        return false;
                    }
                    else
                    {
                        return true;
                    }
                }

                catch (Exception ex)
                {
                    return true;
                }
            }


            public event EventHandler<dynamic> OnResponse;

        }

        public class NostrSigner
        {
            public string PrivateKey;
            public NostrSigner(string privatekey)
            {
                this.PrivateKey = privatekey;
            }

            public JObject Sign(JArray evt)
            {

                string stringifiedevent = JsonConvert.SerializeObject(evt);

                Console.WriteLine(stringifiedevent);
                var msgBytes = Encoding.UTF8.GetBytes(stringifiedevent);
                var msgHash = System.Security.Cryptography.SHA256.Create().ComputeHash(msgBytes);
                string id = BitConverter.ToString(msgHash).ToLower().Replace("-", "");

                // Sign the message
                byte[] signatureBytes = SchnorrSigner.SignMessage(msgHash, this.PrivateKey);
                string signature = BitConverter.ToString(signatureBytes).Replace("-", "").ToLower();


                byte[] publickey = SchnorrSigner.getPublickey(this.PrivateKey);
                bool isValid = SchnorrSigner.VerifyMessage(msgHash, signatureBytes, publickey);
                Console.WriteLine("Valid Signature: " + isValid);

                // Output the signature
                Console.WriteLine("Schnorr Signature C Library: " + signature);

                JObject signed_event = new JObject
                    {
                        { "id", id  },
                        { "pubkey", evt[1] },
                        { "created_at", evt[2] },
                        { "kind", evt[3] },
                        { "tags", evt[4] },
                        { "content", evt[5] },
                        { "sig", signature }
                    };


                return signed_event;

            }

        }

        public static JToken getTag(JArray tags, string parseBy)
        {

            foreach (var tag in tags
                .Where(obj => obj[0].Value<string>() == parseBy))
            {
                return tag;
            }
            return null;
        }
    }
}