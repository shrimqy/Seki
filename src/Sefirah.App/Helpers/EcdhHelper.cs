using Org.BouncyCastle.Asn1.Sec;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;
using Sefirah.App.Data.Models;
using System.Linq;

namespace Sefirah.App.Helpers;

public class ECDHHelper
{
    private static AsymmetricCipherKeyPair? _keyPair;
    private static string? _publicKeyBase64;
    private static readonly KeyStorage _keyStorage = new();
    private static readonly List<DiscoveredDevice> _devices = [];

    public static string GenerateKeys()
    {
        // Try to load existing keypair first
        _keyPair = _keyStorage.RetrieveKeyPair();

        if (_keyPair != null && _publicKeyBase64 != null)
        {
            // Regenerate the public key base64
            var qPoint = ((ECPublicKeyParameters)_keyPair.Public).Q;
            _publicKeyBase64 = Convert.ToBase64String(qPoint.GetEncoded(false));
            return _publicKeyBase64;
        }

        // Generate ECDH key pair
        var ecParams = SecNamedCurves.GetByName("secp256r1");
        var ecDomainParameters = new ECDomainParameters(ecParams.Curve, ecParams.G, ecParams.N, ecParams.H);

        var keyPairGenerator = new ECKeyPairGenerator();
        var keyGenParams = new ECKeyGenerationParameters(ecDomainParameters, new SecureRandom());
        keyPairGenerator.Init(keyGenParams);
        _keyPair = keyPairGenerator.GenerateKeyPair();
        _keyStorage.StoreKeyPair(_keyPair);

        // Extract raw EC point (uncompressed format)
        var q = ((ECPublicKeyParameters)_keyPair.Public).Q;
        var rawPublicKey = q.GetEncoded(false); // Uncompressed format
        _publicKeyBase64 = Convert.ToBase64String(rawPublicKey);

        return _publicKeyBase64;
    }

    public static DiscoveredDevice DeriveSharedSecret(DiscoveredDevice device, string androidPublicKey)
    {
        try
        {
            if (_keyPair == null)
            {
                throw new InvalidOperationException("Keys not generated");
            }

            // Check if we already have this device
            var index = _devices.FindIndex(d => d.PublicKey == androidPublicKey);
            if (index != -1)
            {
                // Update existing device with new key
                device.HashedKey = DeriveKey(androidPublicKey);
                _devices[index] = device;
                return device;
            }

            // If it's a new device, derive key and add to list
            device.HashedKey = DeriveKey(androidPublicKey);
            _devices.Add(device);
            return device;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Exception: {ex}");
            throw;
        }
    }


    private static byte[] DeriveKey(string androidPublicKey)
    {
        byte[] rawPointBytes = Convert.FromBase64String(androidPublicKey);
        var ecParams = SecNamedCurves.GetByName("secp256r1");
        var point = ecParams.Curve.DecodePoint(rawPointBytes);
        var publicKeyParameters = new ECPublicKeyParameters(point,
            new ECDomainParameters(ecParams.Curve, ecParams.G, ecParams.N, ecParams.H));

        var agreement = AgreementUtilities.GetBasicAgreement("ECDH");
        agreement.Init(_keyPair!.Private);
        var sharedSecret = agreement.CalculateAgreement(publicKeyParameters);
        var sharedSecretBytes = sharedSecret.ToByteArrayUnsigned();

        var sha256 = new Sha256Digest();
        var hashedSecret = new byte[sha256.GetDigestSize()];
        sha256.BlockUpdate(sharedSecretBytes, 0, sharedSecretBytes.Length);
        sha256.DoFinal(hashedSecret, 0);

        // var derivedKeyInt = BitConverter.ToInt32(hashedSecret, 0);
        //derivedKeyInt = Math.Abs(derivedKeyInt) % 1_000_000;
        return hashedSecret;
    }

    public static byte[]? GetSharedSecret(string publicKey)
    {
        Debug.WriteLine($"Looking for device with ID: {publicKey}");
        Debug.WriteLine($"Current devices: {string.Join(", ", _devices.Select(d => $"{d.PublicKey}:{d.HashedKey}"))}");
        return _devices?.FirstOrDefault(e => e.PublicKey.Contains(publicKey))?.HashedKey;
    }

    public static bool VerifyDevice(string androidPublicKey, byte[] expectedHashedSecret)
    {
        var derivedHashedSecret = _devices?.FirstOrDefault(e => e.PublicKey.Contains(androidPublicKey))?.HashedKey;
        if (derivedHashedSecret == null)
        {
            return false;
        }
        return derivedHashedSecret.SequenceEqual(expectedHashedSecret);
    }
}
