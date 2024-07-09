using System;

using System.Text;
using System.Threading.Tasks;
using WebSocket = WebSocketSharp.WebSocket;
using Newtonsoft.Json.Linq;
using Secp256k1Net;
using Newtonsoft.Json;

using System.Runtime.InteropServices;


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

               // string test = BitConverter.ToString(keypair).Replace("-", "").ToLower();
    

                int result = secp256k1_schnorrsig_sign32(context, signature, message, keypair, nonce);
                secp256k1_context_destroy(context);

             



                if (result == 1)
                {
                    Console.WriteLine("Signature successfully created.");
                    // Do something with the signature
                }
                else
                {
                    Console.WriteLine("Failed to create signature.");
                }
                return signature;
            }

      
        }


    public class SchnorrSigner
        {

            public static string SignMessage(byte[] messageBytes, string privateKeyHex)
            {
                //var messageBytes = Encoding.UTF8.GetBytes(message);
                var privateKeyBytes = HexStringToByteArray(privateKeyHex);

                // Generate a 32-byte nonce (random or derived as needed)
                var nonce = new byte[32];
                new Random().NextBytes(nonce);

                var signatureBytes = Secp256k1Wrapper.SignSchnorr(messageBytes, privateKeyBytes, nonce);
                Console.WriteLine(signatureBytes);

                return BitConverter.ToString(signatureBytes).Replace("-", "").ToLower();
            }


            private static byte[] HexStringToByteArray(string hex)
            {
                int NumberChars = hex.Length;
                byte[] bytes = new byte[NumberChars / 2];
                for (int i = 0; i < NumberChars; i += 2)
                    bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
                return bytes;
            }

          


        }





            public class KeyPairHex
        {
            public string PrivateKeyHex;
            public string PublicKeyHex;
            
        }


 


        public async Task listenNostr()
        {

            var ws = new WebSocket("wss://relay.damus.io");
            ws.OnMessage += (sender, e) => Console.WriteLine("Received: " + e.Data);

            ws.Connect();

            string subscriptionid = "asdjnasdlkjashdajskdhasjdasd";
            JArray array = new JArray();
            JArray kinds = new JArray
            {
                5302
            };


            JObject filter = new JObject
            { 
                { "kinds", kinds} ,
              
            };

            array.Add("REQ");
            array.Add(subscriptionid);
            array.Add(filter);

            Console.WriteLine(array.ToString());
            ws.Send(array.ToString());

            Console.ReadKey(true);
            ws.Close();



        }



        public byte[] getPublickey(string private_kex)
        {

            var privateKey = HexToByteArray(private_kex);

            var secp256k1 = new Secp256k1();
            var publicKey = new byte[Secp256k1.PUBKEY_LENGTH];
            secp256k1.PublicKeyCreate(publicKey, privateKey);


            return publicKey;
        }

        public string getPublickeyHex(string private_kex)
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


        public byte[] HexToByteArray(string hex)
        {
            hex = hex.Replace(" ", "").Replace("-", "");

            var numberChars = hex.Length;
            var bytes = new byte[numberChars / 2];
            for (var i = 0; i < numberChars; i += 2)
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            return bytes;
        }


        public KeyPairHex createKeys()
        {
            var secp256k1 = new Secp256k1();
            var privateKey = new byte[Secp256k1.PRIVKEY_LENGTH];
            var rnd = System.Security.Cryptography.RandomNumberGenerator.Create();
            do { rnd.GetBytes(privateKey); }
            while (!secp256k1.SecretKeyVerify(privateKey));

            var hexStringPrivate = BitConverter.ToString(privateKey).ToLower();
            hexStringPrivate = hexStringPrivate.Replace("-", "");
            Console.WriteLine(hexStringPrivate);

            string hexStringPublic = getPublickeyHex(hexStringPrivate);
       

            return new KeyPairHex()
            {
                PrivateKeyHex = hexStringPrivate,
                PublicKeyHex = hexStringPublic
            }; 


        }


        public async Task sendNostr(string private_key_hex)
        {
            var secp256k1 = new Secp256k1();
            byte[] privateKey = HexToByteArray(private_key_hex);
            string publickeyhex = getPublickeyHex(private_key_hex);

            var ws = new WebSocket("wss://relay.damus.io");
            //var ws = new WebSocket("wss://nostr.mom");
            ws.OnMessage += (sender, ex) => Console.WriteLine("Nostr Received: " + ex.Data);

            ws.Connect();
         
            DateTime currentTime = DateTime.UtcNow;
            long created_at = ((DateTimeOffset)currentTime).ToUnixTimeSeconds();
            //created_at = 1720464386;
            int kind = 1;
            string content = "Hello Daki";
            JArray tags = new JArray();

            JArray evt = new JArray
            {
                  0,
                  publickeyhex,
                  created_at,
                  kind,
                  tags,
                  content
            };




            string stringifiedevent = JsonConvert.SerializeObject(evt);

            Console.WriteLine(stringifiedevent);
            var msgBytes = Encoding.UTF8.GetBytes(stringifiedevent);
            var msgHash = System.Security.Cryptography.SHA256.Create().ComputeHash(msgBytes);
            string id = BitConverter.ToString(msgHash).ToLower().Replace("-", "");

     

            // Sign the message
            string signature = SchnorrSigner.SignMessage(msgHash, private_key_hex);

           // bool isValid = SchnorrSigner2.VerifyMessage(id, signature, publickeyhex);

            // Output the signature
            Console.WriteLine("Schnorr Signature C Library: " + signature);

            JObject signed_event = new JObject
            {
                { "id", id  },
                { "pubkey", publickeyhex },
                { "created_at", created_at },
                { "kind", kind },
                { "tags", tags },
                { "content", content },
                { "sig", signature }
            };



            JArray array = new JArray
            {
                "EVENT",
                signed_event
            };

            Console.WriteLine(array.ToString());
            ws.Send(array.ToString());

            Console.ReadKey(true);
            ws.Close();
    






        }



    }
}