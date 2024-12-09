using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.Nist;
using Org.BouncyCastle.Asn1.Sec;
using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Agreement;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;
using System.Numerics;
using System.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Sefirah.App.Helpers
{
    public class SSLHelper
    {
        public static async Task<X509Certificate2> CreateECDSACertificate()
        {
            // Create ECDiffieHellman for key agreement
            using var ecdh = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);

            // Create ECDSA using the same parameters
            using var ecdsa = ECDsa.Create(ecdh.ExportParameters(true));

            var parameters = ecdsa.ExportParameters(true);

            var subjectName = new X500DistinguishedName("CN=KumoSeki");
            CertificateRequest certRequest = new(subjectName, ecdsa, HashAlgorithmName.SHA256);

            // Add certificate extensions
            certRequest.CertificateExtensions.Add(
                new X509BasicConstraintsExtension(true, false, 0, true));

            certRequest.CertificateExtensions.Add(
                new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment, true));

            DateTimeOffset notBefore = DateTimeOffset.Now;
            DateTimeOffset notAfter = DateTimeOffset.Now.AddYears(10);
            X509Certificate2 certificate = certRequest.CreateSelfSigned(notBefore, notAfter);

            // Ensure the certificate is exportable
            certificate = new X509Certificate2(
                certificate.Export(X509ContentType.Pfx),
                password: null as SecureString,
                X509KeyStorageFlags.Exportable | X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.EphemeralKeySet);

            string certPath = Path.Combine(Windows.Storage.ApplicationData.Current.LocalFolder.Path, "KumoSeki.pfx");

            // Export the certificate with private key
            byte[] certData = certificate.Export(X509ContentType.Pfx);

            // Save to application local storage
            await File.WriteAllBytesAsync(certPath, certData);
            Debug.WriteLine($"Certificate saved to: {certPath}");
            // Store the original parameters for ECDH
            StoredParameters = parameters;


            return certificate;
        }

        private static async Task<X509Certificate2> GetOrCreateCertificateAsync(string certificateFileName)
        {
            string certPath = Path.Combine(Windows.Storage.ApplicationData.Current.LocalFolder.Path, certificateFileName);

            if (File.Exists(certPath))
            {
                try
                {
                    return new X509Certificate2(certPath);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to load certificate: {ex.Message}");
                }
            }

            return await CreateECDSACertificate();
        }


        // Store the parameters for ECDH operations
        private static ECParameters? StoredParameters;

        public static AsymmetricCipherKeyPair GenerateKeys()
        {
            var ecParameters = NistNamedCurves.GetByName("P-256");
            var ecSpec = new ECDomainParameters(ecParameters.Curve, ecParameters.G, ecParameters.N, ecParameters.H,
                ecParameters.GetSeed());
            var keyPairGenerator = GeneratorUtilities.GetKeyPairGenerator("ECDH");
            keyPairGenerator.Init(new ECKeyGenerationParameters(ecSpec, new SecureRandom()));

            return keyPairGenerator.GenerateKeyPair();
        }

        public static BigInteger DeriveSharedSecret(ECDiffieHellman ecdh, ECDiffieHellmanPublicKey peerPublicKey)
        {
            ECDHCBasicAgreement basicAgreement = new();
            ECParameters privParameters = ecdh.ExportParameters(includePrivateParameters: true);
            X9ECParameters curve = NistNamedCurves.GetByOid(new DerObjectIdentifier(privParameters.Curve.Oid.Value));
            ECDomainParameters ecParams = new(curve.Curve, curve.G, curve.N, curve.H, curve.GetSeed());
            var bcPrivateParams = new ECPrivateKeyParameters(
                algorithm: "ECDHC",
                d: ToBCBigInteger(privParameters.D),
                parameters: ecParams);
            basicAgreement.Init(bcPrivateParams);
            ECParameters peerParameters = peerPublicKey.ExportParameters();
            ECPoint q = peerParameters.Q;
            var bcPeerParameters = new ECPublicKeyParameters(
                algorithm: "ECDHC",
                q: ecParams.Curve.CreatePoint(ToBCBigInteger(q.X), ToBCBigInteger(q.Y)),
                parameters: ecParams);
            var secret = basicAgreement.CalculateAgreement(bcPeerParameters);
            return ToBigInteger(secret.ToByteArrayUnsigned());
        }

        public byte[] GetDerivedKeyFromCert(X509Certificate2 publicCertificate, X509Certificate2 privateCertificate)
        {
            byte[] derivedKey;

            using (var privateKey = (ECDsaCng)privateCertificate.GetECDsaPrivateKey())
            using (var publicKey = (ECDsaCng)publicCertificate.GetECDsaPublicKey())
            {
                var publicParams = publicKey.ExportParameters(false);

                var publicCng = ECDiffieHellman.Create(publicParams);
                using var diffieHellman = new ECDiffieHellmanCng(privateKey.Key);
                derivedKey = diffieHellman.DeriveKeyMaterial(publicCng.PublicKey);
            }

            return derivedKey;
        }

        public static int GetDerivedKey(string publicKeyBase64, X509Certificate2 privateCertificate)
        {
            try
            {
                // Decode the base64 public key
                byte[] publicKeyBytes = Convert.FromBase64String(publicKeyBase64);
                Debug.WriteLine($"Received Public Key Bytes Length: {publicKeyBytes.Length}");
                Debug.WriteLine($"Public Key Bytes Hex: {BitConverter.ToString(publicKeyBytes)}");

                // Create ECParameters structure for the incoming public key
                var publicKeyParams = new ECParameters
                {
                    Curve = ECCurve.NamedCurves.nistP256,
                    Q = new ECPoint
                    {
                        X = publicKeyBytes.Skip(1).Take(32).ToArray(),
                        Y = publicKeyBytes.Skip(33).Take(32).ToArray()
                    }
                };

                // Get the private key parameters from the certificate
                using var privateKey = privateCertificate.GetECDsaPrivateKey();
                Debug.WriteLine($"Received private Key: {publicKeyBytes.Length}");

                //var privateKeyParams = privateKey.ExportParameters(true);

                // Create new ECDiffieHellman instance with the private key parameters
                //using var diffieHellman = ECDiffieHellman.Create();
                //diffieHellman.ImportParameters(new ECParameters
                //{
                //    Curve = ECCurve.NamedCurves.nistP256,
                //    D = privateKeyParams.D, // Private key
                //    Q = privateKeyParams.Q  // Public key point
                //});

                //// Create ECDiffieHellman for the public key
                //using var publicKey = ECDiffieHellman.Create();
                //publicKey.ImportParameters(publicKeyParams);

                //// Derive the shared secret
                //byte[] sharedKey = diffieHellman.DeriveKeyMaterial(publicKey.PublicKey);
                //Debug.WriteLine($"Shared Key Length: {sharedKey.Length}");
                //Debug.WriteLine($"Shared Key: {BitConverter.ToString(sharedKey)}");

                // Hash the shared key and reduce to a 6-digit number
                //byte[] hashedKey = SHA256.HashData(sharedKey);
                //int derivedKey = Math.Abs(BitConverter.ToInt32(hashedKey, 0)) % 1000000;
                //Debug.WriteLine($"Derived Key: {derivedKey}");
                return 56;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Full Exception: {ex}");
                Debug.WriteLine($"Exception Message: {ex.Message}");
                Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
                throw;
            }
        }

        public static int DeriveSharedSecret(string androidPublicKeyBase64, X509Certificate2 localCertificate)
        {
            try
            {
                // Decode the base64 public key
                byte[] androidPublicKeyRaw = Convert.FromBase64String(androidPublicKeyBase64);
                Debug.WriteLine($"Received Public Key Bytes Length: {androidPublicKeyRaw.Length}");
                Debug.WriteLine($"Public Key Bytes Hex: {BitConverter.ToString(androidPublicKeyRaw)}");

                // Create ECPoint from the raw Android public key
                // The first byte (0x04) indicates uncompressed format
                byte[] xCoord = new byte[32];
                byte[] yCoord = new byte[32];
                Buffer.BlockCopy(androidPublicKeyRaw, 1, xCoord, 0, 32);
                Buffer.BlockCopy(androidPublicKeyRaw, 33, yCoord, 0, 32);

                // Create ECParameters for the public key
                var curve = ECCurve.CreateFromValue("1.2.840.10045.3.1.7"); // prime256v1/P-256 OID
                var publicKeyParams = new ECParameters
                {
                    Curve = curve,
                    Q = new ECPoint
                    {
                        X = xCoord,
                        Y = yCoord
                    }
                };

                using (var privateKey = localCertificate.GetECDsaPrivateKey())
                {
                    // Make the private key exportable
                    var privateKeyCng = (ECDsaCng)privateKey;
                    byte[] exportPolicyBytes = BitConverter.GetBytes((int)(
                        CngExportPolicies.AllowExport | CngExportPolicies.AllowPlaintextExport));
                    var exportPolicy = new CngProperty(
                        "Export Policy",
                        exportPolicyBytes,
                        CngPropertyOptions.Persist);
                    privateKeyCng.Key.SetProperty(exportPolicy);

                    // Get the private key parameters
                    var privateKeyParams = privateKeyCng.ExportParameters(true);

                    // Create ECDH objects for key agreement
                    using (var privateEcdh = ECDiffieHellman.Create(privateKeyParams))
                    using (var publicEcdh = ECDiffieHellman.Create(publicKeyParams))
                    {
                        // Derive the shared secret
                        byte[] sharedSecret = privateEcdh.DeriveKeyMaterial(publicEcdh.PublicKey);

                        //// Hash the shared key and reduce to a 6-digit number
                        byte[] hashedKey = SHA256.HashData(sharedSecret);
                        int derivedKey = Math.Abs(BitConverter.ToInt32(hashedKey, 0)) % 1000000;
                        Debug.WriteLine($"Derived Key: {derivedKey}");
                        return derivedKey;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Full Exception: {ex}");
                Debug.WriteLine($"Exception Message: {ex.Message}");
                Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
                throw;
            }
        }

        private static ECDiffieHellmanCngPublicKey ExtractECPublicKey(byte[] publicKeyBytes)
        {
            // Decode the X.509 SubjectPublicKeyInfo
            var keyInfo = new AsnEncodedData(publicKeyBytes);
            var ecdsaPublicKey = ECDsa.Create();
            ecdsaPublicKey.ImportSubjectPublicKeyInfo(publicKeyBytes, out _);

            // Export the raw EC public key in EccPublicBlob format
            byte[] rawKey = ecdsaPublicKey.ExportSubjectPublicKeyInfo();
            return (ECDiffieHellmanCngPublicKey)ECDiffieHellmanCngPublicKey.FromByteArray(rawKey, CngKeyBlobFormat.EccPublicBlob);
        }

        private static BigInteger ToBigInteger(ReadOnlySpan<byte> span)
        {
            return new BigInteger(span, isUnsigned: true, isBigEndian: true);
        }

        private static Org.BouncyCastle.Math.BigInteger ToBCBigInteger(byte[] span)
        {
            return new Org.BouncyCastle.Math.BigInteger(1, span);
        }

        private const string NCryptExportPolicyProperty = "Export Policy";
        private const string SignatureAlgorithm = "Sha256WithECDSA";
        private static readonly ECCurve MsCurve = ECCurve.NamedCurves.nistP256;
        private static readonly DerObjectIdentifier BcCurve = SecObjectIdentifiers.SecP256r1; // must correspond with MsCurve

        //private X509Certificate2 CreateECSDACertificate(string certificateName,
        //string issuerCertificateName,
        //TimeSpan lifetime,
        //AsymmetricKeyParameter issuerPrivateKey,
        //string certificateFriendlyName = null)
        //{
        //    // Generating Random Numbers
        //    var randomGenerator = new CryptoApiRandomGenerator();
        //    var random = new SecureRandom(randomGenerator);

        //    var signatureFactory = new Asn1SignatureFactory("SHA256WithECDSA", issuerPrivateKey, random);

        //    // The Certificate Generator
        //    var certificateGenerator = new X509V3CertificateGenerator();


        //    // Issuer and Subject Name
        //    var subjectDistinguishedName = new X509Name($"CN={certificateName}");
        //    var issuerDistinguishedName = new X509Name($"CN={issuerCertificateName}");
        //    certificateGenerator.SetSubjectDN(subjectDistinguishedName);
        //    certificateGenerator.SetIssuerDN(issuerDistinguishedName);

        //    // Valid For
        //    var notBefore = DateTime.UtcNow.Date;
        //    var notAfter = notBefore.Add(lifetime);

        //    certificateGenerator.SetNotBefore(notBefore);
        //    certificateGenerator.SetNotAfter(notAfter);

        //    //key generation
        //    var keyGenerationParameters = new KeyGenerationParameters(random, 256);
        //    var keyPairGenerator = new ECKeyPairGenerator();
        //    keyPairGenerator.Init(keyGenerationParameters);
        //    var subjectKeyPair = keyPairGenerator.GenerateKeyPair();

        //    certificateGenerator.SetPublicKey(subjectKeyPair.Public);

        //    var certificate = certificateGenerator.Generate(signatureFactory);

        //    var store = new Pkcs12Store();
        //    var certificateEntry = new X509CertificateEntry(certificate);
        //    store.SetCertificateEntry(certificateName, certificateEntry);
        //    store.SetKeyEntry(certificateName, new AsymmetricKeyEntry(subjectKeyPair.Private), [certificateEntry]);

        //    X509Certificate2 x509;

        //    using (var pfxStream = new MemoryStream())
        //    {
        //        store.Save(pfxStream, null, new SecureRandom());
        //        pfxStream.Seek(0, SeekOrigin.Begin);
        //        x509 = new X509Certificate2(pfxStream.ToArray());
        //    }

        //    x509.FriendlyName = certificateFriendlyName;

        //    return x509;
        //}

    }
}

