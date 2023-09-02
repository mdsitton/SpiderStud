using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Tls;
using Org.BouncyCastle.Tls.Crypto;
using Org.BouncyCastle.Tls.Crypto.Impl.BC;
using Org.BouncyCastle.X509;

namespace SpiderStud.Tls
{
    internal interface IBcTlsCallbacks
    {
        void OnHandshakeComplete();
    }

    internal class BcTlsServer : DefaultTlsServer
    {
        private readonly TlsVersions versions;
        public TlsVersions TlsVerions => versions;
        private readonly IBcTlsCallbacks callbacks;
        private readonly X509Certificate certificate;
        private readonly AsymmetricCipherKeyPair keyPair;
        private readonly SignatureAndHashAlgorithm algorithm;

        private readonly List<ProtocolName> protocolNames = new List<ProtocolName>() { ProtocolName.Http_1_1 };

        internal BcTlsServer(IBcTlsCallbacks callbacks, TlsVersions versions, System.Security.Cryptography.X509Certificates.X509Certificate2 certificate) : base(new BcTlsCrypto(new SecureRandom()))
        {
            this.versions = versions;
            this.callbacks = callbacks;
            this.certificate = DotNetUtilities.FromX509Certificate(certificate);
            this.keyPair = DotNetUtilities.GetKeyPair(certificate.PrivateKey);

            algorithm = new SignatureAndHashAlgorithm(HashAlgorithm.sha256, SignatureAlgorithm.rsa);
        }

        public override void NotifyHandshakeComplete()
        {
            base.NotifyHandshakeComplete();
            callbacks.OnHandshakeComplete();
        }

        protected override IList<ProtocolName> GetProtocolNames() => protocolNames;

        protected override TlsCredentialedSigner GetRsaSignerCredentials()
        {
            BcTlsCrypto? crypto = Crypto as BcTlsCrypto;
            var tlsCertificate = new BcTlsCertificate(crypto, certificate.CertificateStructure);
            var cert = new Certificate(new[] { tlsCertificate });
            var parm = new TlsCryptoParameters(m_context);

            return new BcDefaultTlsCredentialedSigner(parm, (BcTlsCrypto)Crypto, keyPair.Private, cert, algorithm);
        }

        protected override ProtocolVersion[] GetSupportedVersions()
        {
            if (versions == TlsVersions.Tls12)
                return ProtocolVersion.TLSv12.Only();
            else if (versions == TlsVersions.Tls13)
                return ProtocolVersion.TLSv13.Only();

            // Otherwise default to tls12/tls13 
            return ProtocolVersion.TLSv13.DownTo(ProtocolVersion.TLSv12);
        }
    }
}