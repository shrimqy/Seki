using System.Security.Cryptography;
using Org.BouncyCastle.Crypto;
using System.Text.Json;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Asn1.Sec;
using System.IO;


public class KeyStorage
{
    private readonly string _keyPairPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Seki",
        "keypair.dat");

    private class KeyPairData
    {
        public byte[] PrivateKey { get; set; } = [];
        public byte[] PublicKey { get; set; } = [];
    }

    public void StoreKeyPair(AsymmetricCipherKeyPair keyPair)
    {
        try
        {
            var privateKey = ((ECPrivateKeyParameters)keyPair.Private).D.ToByteArrayUnsigned();
            var publicKey = ((ECPublicKeyParameters)keyPair.Public).Q.GetEncoded(false);

            var keyPairData = new KeyPairData
            {
                PrivateKey = privateKey,
                PublicKey = publicKey
            };

            // Serialize to JSON
            string jsonString = JsonSerializer.Serialize(keyPairData);
            byte[] serializedData = System.Text.Encoding.UTF8.GetBytes(jsonString);

            // Encrypt using DPAPI
            byte[] encryptedData = ProtectedData.Protect(
                serializedData,
                optionalEntropy: null,
                scope: DataProtectionScope.CurrentUser);

            Directory.CreateDirectory(Path.GetDirectoryName(_keyPairPath)!);
            File.WriteAllBytes(_keyPairPath, encryptedData);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error storing keypair: {ex}");
            throw;
        }
    }

    public AsymmetricCipherKeyPair? RetrieveKeyPair()
    {
        try
        {
            if (!File.Exists(_keyPairPath))
                return null;

            byte[] encryptedData = File.ReadAllBytes(_keyPairPath);
            byte[] decryptedData = ProtectedData.Unprotect(
                encryptedData,
                optionalEntropy: null,
                scope: DataProtectionScope.CurrentUser);

            string jsonString = System.Text.Encoding.UTF8.GetString(decryptedData);
            var keyPairData = JsonSerializer.Deserialize<KeyPairData>(jsonString);

            if (keyPairData == null)
                return null;

            // Reconstruct the key pair
            var ecParams = SecNamedCurves.GetByName("secp256r1");
            var ecDomainParameters = new ECDomainParameters(ecParams.Curve, ecParams.G, ecParams.N, ecParams.H);

            // Create private key parameters
            var privateKeyParameters = new ECPrivateKeyParameters(
                new Org.BouncyCastle.Math.BigInteger(1, keyPairData.PrivateKey),
                ecDomainParameters);

            // Create public key parameters
            var point = ecParams.Curve.DecodePoint(keyPairData.PublicKey);
            var publicKeyParameters = new ECPublicKeyParameters(point, ecDomainParameters);

            return new AsymmetricCipherKeyPair(publicKeyParameters, privateKeyParameters);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error retrieving keypair: {ex}");
            return null;
        }
    }
} 