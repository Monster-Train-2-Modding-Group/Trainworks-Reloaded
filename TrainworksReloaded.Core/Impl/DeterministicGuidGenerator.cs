using System.Security.Cryptography;
using System.Text;
using TrainworksReloaded.Core.Interfaces;

namespace TrainworksReloaded.Core.Impl
{
    public class DeterministicGuidGenerator : IGuidProvider
    {
        private static readonly SHA256CryptoServiceProvider provider = new();

        public Guid GetGuidDeterministic(string key)
        {
            //Generate a byte array from a string using default encoding
            byte[] inputBytes = Encoding.Default.GetBytes(key);

            //Use bytes to generate a byte array of length 16 using MD5CryptoServiceProvider
            byte[] hashBytes = provider.ComputeHash(inputBytes);

            //Set the first nibble of the byte to be 0100
            hashBytes[7] = (byte)(0x40 | ((int)hashBytes[7] & 0xf));

            //Set the first crumb of the byte to be 10
            hashBytes[8] = (byte)(0x80 | ((int)hashBytes[8] & 0x3f));

            //Resize the array to get the first 16 bytes
            Array.Resize<byte>(ref hashBytes, 16);

            return new Guid(hashBytes);
        }
    }
}
