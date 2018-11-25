// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.Azure.Devices.Edge.Util.Test.Certificate
{
    using System;
    using System.IO;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Cryptography.X509Certificates;
    using System.Text;
    using Microsoft.Azure.Devices.Edge.Util.Edged.GeneratedCode;
    using Microsoft.Azure.Devices.Edge.Util.Test.Common;
    using Xunit;
    using CertificateHelper = Microsoft.Azure.Devices.Edge.Util.CertificateHelper;
    using TestCertificateHelper = Microsoft.Azure.Devices.Edge.Util.Test.Common.CertificateHelper;

    [Unit]
    public class CertificateHelperTest
    {
        [Fact]
        public void GetThumbprintNullCertThrows()
        {
            Assert.Throws<ArgumentNullException>(() => CertificateHelper.GetSha256Thumbprint(null));
        }

        [Fact]
        public void BuildCertificateListSuccess()
        {
            X509Certificate2 cert = TestCertificateHelper.GenerateSelfSignedCert("top secret");
            (IList<X509Certificate2> certs, Option<string> errors) = CertificateHelper.BuildCertificateList(cert, Option.None<IList<X509Certificate2>>());
            Assert.True(certs.Count == 1);
            Assert.False(errors.HasValue);
        }

        [Fact]
        public void ValidateCertNullArgumentsThrows()
        {
            var trustedCACerts = Option.None<IList<X509Certificate2>>();
            Assert.Throws<ArgumentNullException>(() => CertificateHelper.ValidateCert(null, new X509Certificate2[] { }, trustedCACerts));
            Assert.Throws<ArgumentNullException>(() => CertificateHelper.ValidateCert(new X509Certificate2(), null, trustedCACerts));
        }

        [Fact]
        public void ValidateCertSuccess()
        {
            var trustedCACerts = Option.None<IList<X509Certificate2>>();
            X509Certificate2 cert = TestCertificateHelper.GenerateSelfSignedCert("top secret");
            (bool validated, Option<string> errors) = CertificateHelper.ValidateCert(cert, new[] { cert }, trustedCACerts);
            Assert.True(validated);
            Assert.False(errors.HasValue);
        }

        [Fact]
        public void ValidateCertNoMatchFailure()
        {
            X509Certificate2 cert = TestCertificateHelper.GenerateSelfSignedCert("top secret");
            X509Certificate2 root = TestCertificateHelper.GenerateSelfSignedCert("root");
            IList<X509Certificate2> ca = new List<X509Certificate2>() { root };
            (bool validated, Option<string> errors) = CertificateHelper.ValidateCert(cert, new[] { cert }, Option.Some(ca));
            Assert.False(validated);
            Assert.True(errors.HasValue);
        }

        [Fact]
        public void ClientCertCallbackNullArgumentThrows()
        {
            var trustedCACerts = Option.None<IList<X509Certificate2>>();
            Assert.Throws<ArgumentNullException>(() =>
            CertificateHelper.ValidateClientCert(null, new X509Chain(), trustedCACerts, Logger.Factory.CreateLogger("something")));
            Assert.Throws<ArgumentNullException>(() =>
            CertificateHelper.ValidateClientCert(new X509Certificate2(), null, trustedCACerts, Logger.Factory.CreateLogger("something")));
            Assert.Throws<ArgumentNullException>(() =>
            CertificateHelper.ValidateClientCert(new X509Certificate2(), new X509Chain(), trustedCACerts, null));
        }

        [Fact]
        public void ClientCertCallbackNoCaCertsFails()
        {
            X509Certificate2 cert = TestCertificateHelper.GenerateSelfSignedCert("top secret");
            IList<X509Certificate2> ca = new List<X509Certificate2>();
            var trustedCACerts = Option.Some(ca);
            Assert.False(CertificateHelper.ValidateClientCert(cert, new X509Chain(), trustedCACerts, Logger.Factory.CreateLogger("something")));
        }


        [Fact]
        public void ExtractCertsNullArgumentFails()
        {
            Assert.Throws<ArgumentException>(() => CertificateHelper.ExtractCertsFromPem(null));
            Assert.Throws<ArgumentException>(() => CertificateHelper.ExtractCertsFromPem(""));
        }

        [Fact]
        public void GetServerCertificateAndChainFromFileRaisesArgExceptionWithInvalidCertFile()
        {
            string testFile = Path.GetRandomFileName();
            Assert.Throws<ArgumentException>(() => CertificateHelper.GetServerCertificateAndChainFromFile(null, testFile));
            Assert.Throws<ArgumentException>(() => CertificateHelper.GetServerCertificateAndChainFromFile("", testFile));
            Assert.Throws<ArgumentException>(() => CertificateHelper.GetServerCertificateAndChainFromFile("   ", testFile));
        }

        [Fact]
        public void GetServerCertificateAndChainFromFileRaisesArgExceptionWithInvalidPrivateKeyFile()
        {
            string testFile = Path.GetRandomFileName();
            Assert.Throws<ArgumentException>(() => CertificateHelper.GetServerCertificateAndChainFromFile(testFile, null));
            Assert.Throws<ArgumentException>(() => CertificateHelper.GetServerCertificateAndChainFromFile(testFile, ""));
            Assert.Throws<ArgumentException>(() => CertificateHelper.GetServerCertificateAndChainFromFile(testFile, "   "));
        }

        [Fact]
        public void ParseTrustedBundleFromFileRaisesExceptionWithInvalidTBFile()
        {
            string testFile = Path.GetRandomFileName();
            Assert.Throws<ArgumentException>(() => CertificateHelper.ParseTrustedBundleFromFile(null));
            Assert.Throws<ArgumentException>(() => CertificateHelper.ParseTrustedBundleFromFile(""));
            Assert.Throws<ArgumentException>(() => CertificateHelper.ParseTrustedBundleFromFile("   "));
            Assert.Throws<ArgumentException>(() => CertificateHelper.ParseTrustedBundleFromFile(testFile));
        }

        [Fact]
        public void ParseTrustBundleNullResponseRaisesException()
        {
            TrustBundleResponse response = null;
            Assert.Throws<ArgumentNullException>(() => CertificateHelper.ParseTrustBundleResponse(response));
        }

        [Fact]
        public void ParseTrustBundleEmptyResponseReturnsEmptyList()
        {
            var response = new TrustBundleResponse()
            {
                Certificate = "  ",
            };
            IEnumerable<X509Certificate2> certs = CertificateHelper.ParseTrustBundleResponse(response);
            Assert.Equal(certs.Count(), 0);
        }

        [Fact]
        public void ParseTrustBundleInvalidResponseReturnsEmptyList()
        {
            var response = new TrustBundleResponse()
            {
                Certificate = "somewhere over the rainbow",
            };
            IEnumerable<X509Certificate2> certs = CertificateHelper.ParseTrustBundleResponse(response);
            Assert.Equal(certs.Count(), 0);
        }

        [Fact]
        public void ParseTrustBundleResponseWithOneCertReturnsNonEmptyList()
        {
            var response = new TrustBundleResponse()
            {
                Certificate = $"{TestCertificateHelper.CertificatePem}\n",
            };
            IEnumerable<X509Certificate2> certs = CertificateHelper.ParseTrustBundleResponse(response);
            Assert.Equal(certs.Count(), 1);
        }

        [Fact]
        public void ParseTrustBundleResponseWithMultipleCertReturnsNonEmptyList()
        {
            var response = new TrustBundleResponse()
            {
                Certificate = $"{TestCertificateHelper.CertificatePem}\n{TestCertificateHelper.CertificatePem}",
            };
            IEnumerable<X509Certificate2> certs = CertificateHelper.ParseTrustBundleResponse(response);
            Assert.Equal(certs.Count(), 2);
        }

        [Fact]
        public void ParseCertificatesSingleShouldReturnCetificate()
        {
            IList<string> pemCerts = CertificateHelper.ParsePemCerts(TestCertificateHelper.CertificatePem);
            IEnumerable<X509Certificate2> certs = CertificateHelper.GetCertificatesFromPem(pemCerts);

            Assert.Equal(certs.Count(), 1);
        }

        [Fact]
        public void ParseCertificatesMultipleCertsShouldReturnCetificates()
        {
            IList<string> pemCerts = CertificateHelper.ParsePemCerts(TestCertificateHelper.CertificatePem + TestCertificateHelper.CertificatePem);
            IEnumerable<X509Certificate2> certs = CertificateHelper.GetCertificatesFromPem(pemCerts);

            Assert.Equal(certs.Count(), 2);
        }

        [Fact]
        public void ParseCertificatesWithNonCertificatesEntriesShouldReturnCetificates()
        {
            IList<string> pemCerts = CertificateHelper.ParsePemCerts(TestCertificateHelper.CertificatePem + TestCertificateHelper.CertificatePem + "test");
            IEnumerable<X509Certificate2> certs = CertificateHelper.GetCertificatesFromPem(pemCerts);

            Assert.Equal(certs.Count(), 2);
        }

        [Fact]
        public void ParseCertificatesNoCertificatesEntriesShouldReturnNoCetificates()
        {
            IList<string> pemCerts = CertificateHelper.ParsePemCerts("test");
            IEnumerable<X509Certificate2> certs = CertificateHelper.GetCertificatesFromPem(pemCerts);

            Assert.Equal(certs.Count(), 0);
        }

        [Fact]
        public void ParseCertificatesResponseInvalidCertificateShouldThrow()
        {
            var response = new CertificateResponse()
            {
                Certificate = "InvalidCert",
            };
            Assert.Throws<InvalidOperationException>(() => CertificateHelper.ParseCertificateResponse(response));
        }

        [Fact]
        public void ParseCertificatesResponseInvalidKeyShouldThrow()
        {
            var response = new CertificateResponse()
            {
                Certificate = TestCertificateHelper.CertificatePem,
                Expiration = DateTime.UtcNow.AddDays(1),
                PrivateKey = new PrivateKey()
                {
                    Bytes = "InvalidKey"
                }
            };

            Assert.Throws<InvalidOperationException>(() => CertificateHelper.ParseCertificateResponse(response));
        }

        [Fact]
        public void ParseCertificatesResponseShouldReturnCert()
        {
            TestCertificateHelper.GenerateSelfSignedCert("top secret").Export(X509ContentType.Cert);
            var response = new CertificateResponse()
            {
                Certificate = $"{TestCertificateHelper.CertificatePem}\n{TestCertificateHelper.CertificatePem}",
                Expiration = DateTime.UtcNow.AddDays(1),
                PrivateKey = new PrivateKey()
                {
                    Bytes = TestCertificateHelper.PrivateKeyPem
                }
            };
            (X509Certificate2 cert, IEnumerable<X509Certificate2> chain) = CertificateHelper.ParseCertificateResponse(response);

            var expected = new X509Certificate2(Encoding.UTF8.GetBytes(TestCertificateHelper.CertificatePem));
            Assert.Equal(expected, cert);
            Assert.True(cert.HasPrivateKey);
            Assert.Equal(chain.Count(), 1);
            Assert.Equal(expected, chain.First());
        }

        [Fact]
        public void ParseCertificateAndKeyShouldReturnCertAndKey()
        {
            TestCertificateHelper.GenerateSelfSignedCert("top secret").Export(X509ContentType.Cert);
            (X509Certificate2 cert, IEnumerable<X509Certificate2> chain) = CertificateHelper.ParseCertificateAndKey(TestCertificateHelper.CertificatePem, TestCertificateHelper.PrivateKeyPem);

            var expected = new X509Certificate2(Encoding.UTF8.GetBytes(TestCertificateHelper.CertificatePem));
            Assert.Equal(expected, cert);
            Assert.True(cert.HasPrivateKey);
            Assert.Equal(chain.Count(), 0);
        }

        [Fact]
        public void ParseMultipleCertificateAndKeyShouldReturnCertAndKey()
        {
            TestCertificateHelper.GenerateSelfSignedCert("top secret").Export(X509ContentType.Cert);
            string certificate = $"{TestCertificateHelper.CertificatePem}\n{TestCertificateHelper.CertificatePem}";
            (X509Certificate2 cert, IEnumerable<X509Certificate2> chain) = CertificateHelper.ParseCertificateAndKey(certificate, TestCertificateHelper.PrivateKeyPem);

            var expected = new X509Certificate2(Encoding.UTF8.GetBytes(TestCertificateHelper.CertificatePem));
            Assert.Equal(expected, cert);
            Assert.True(cert.HasPrivateKey);
            Assert.Equal(chain.Count(), 1);
            Assert.Equal(expected, chain.First());
        }

        [Fact]
        public void TestIfCACertificate()
        {
            var notBefore = DateTime.Now.Subtract(TimeSpan.FromDays(2));
            var notAfter = DateTime.Now.AddYears(1);
            var (caCert, caKeyPair) = TestCertificateHelper.GenerateSelfSignedCert("MyTestCA", notBefore, notAfter, true);
            Assert.True(CertificateHelper.IsCACertificate(caCert));

            var (clientCert, clientKeyPair) = TestCertificateHelper.GenerateSelfSignedCert("MyTestClient", notBefore, notAfter, false);
            Assert.False(CertificateHelper.IsCACertificate(clientCert));

            var (issuedClientCert, issuedClientKeyPair) = TestCertificateHelper.GenerateCertificate("MyIssuedTestClient", notBefore, notAfter, caCert, caKeyPair, false, null);
            Assert.False(CertificateHelper.IsCACertificate(issuedClientCert));
        }

        [Fact]
        public void TestValidateCertificateWithExpiredValidityFails()
        {
            var notBefore = DateTime.Now.Subtract(TimeSpan.FromDays(2));
            var notAfter = DateTime.Now.Subtract(TimeSpan.FromDays(1));
            var (clientCert, clientKeyPair) =  TestCertificateHelper.GenerateSelfSignedCert("MyTestClient", notBefore, notAfter, false);
            Assert.False(CertificateHelper.ValidateClientCert(clientCert, new List<X509Certificate2>() { clientCert }, Option.None<IList<X509Certificate2>>(), Logger.Factory.CreateLogger("something")));
        }

        [Fact]
        public void TestValidateCertificateWithFutureValidityFails()
        {
            var notBefore = DateTime.Now.AddYears(1);
            var notAfter = DateTime.Now.AddYears(2);
            var (clientCert, clientKeyPair) = TestCertificateHelper.GenerateSelfSignedCert("MyTestClient", notBefore, notAfter, false);

            Assert.False(CertificateHelper.ValidateClientCert(clientCert, new List<X509Certificate2>() { clientCert }, Option.None<IList<X509Certificate2>>(), Logger.Factory.CreateLogger("something")));
        }

        [Fact]
        public void TestValidateCertificateWithCAExtentionFails()
        {
            var caCert = TestCertificateHelper.GenerateSelfSignedCert("MyTestCA", true);

            Assert.False(CertificateHelper.ValidateClientCert(caCert, new List<X509Certificate2>() { caCert }, Option.None<IList<X509Certificate2>>(), Logger.Factory.CreateLogger("something")));
        }

        [Fact]
        public void TestValidateCertificateAndChainSucceeds()
        {
            var notBefore = DateTime.Now.Subtract(TimeSpan.FromDays(2));
            var notAfter = DateTime.Now.AddYears(1);
            var (caCert, caKeyPair) = TestCertificateHelper.GenerateSelfSignedCert("MyTestCA", notBefore, notAfter, true);
            var (issuedClientCert, issuedClientKeyPair) = TestCertificateHelper.GenerateCertificate("MyIssuedTestClient", notBefore, notAfter, caCert, caKeyPair, false, null);

            Assert.True(CertificateHelper.ValidateClientCert(issuedClientCert, new List<X509Certificate2>() { caCert }, Option.None<IList<X509Certificate2>>(), Logger.Factory.CreateLogger("something")));
        }

        //TODO need to discuss test failure
        //[Fact]
        //public void TestValidateCertificateAndChainFails()
        //{
        //    var notBefore = DateTime.Now.Subtract(TimeSpan.FromDays(2));
        //    var notAfter = DateTime.Now.AddYears(1);
        //    var (caCert, caKeyPair) = TestCertificateHelper.GenerateSelfSignedCert("MyTestCA", notBefore, notAfter, true);
        //    var (clientCert, clientKeyPair) = TestCertificateHelper.GenerateSelfSignedCert("MyTestClient", notBefore, notAfter, false);
        //    var (issuedClientCert, issuedClientKeyPair) = TestCertificateHelper.GenerateCertificate("MyIssuedTestClient", notBefore, notAfter, caCert, caKeyPair, false);

        //    Assert.False(CertificateHelper.ValidateClientCert(issuedClientCert, new List<X509Certificate2>() { clientCert }, Option.None<IList<X509Certificate2>>(), Logger.Factory.CreateLogger("something")));
        //}

        [Fact]
        public void TestValidateTrustedCACertificateAndChainSucceeds()
        {
            var notBefore = DateTime.Now.Subtract(TimeSpan.FromDays(2));
            var notAfter = DateTime.Now.AddYears(1);
            var (caCert, caKeyPair) = TestCertificateHelper.GenerateSelfSignedCert("MyTestCA", notBefore, notAfter, true);
            var (issuedClientCert, issuedClientKeyPair) = TestCertificateHelper.GenerateCertificate("MyIssuedTestClient", notBefore, notAfter, caCert, caKeyPair, false, null);
            IList<X509Certificate2> trustedCACerts = new List<X509Certificate2>() { caCert };

            Assert.True(CertificateHelper.ValidateClientCert(issuedClientCert, new List<X509Certificate2>() { caCert }, Option.Some(trustedCACerts), Logger.Factory.CreateLogger("something")));
        }

        [Fact]
        public void TestValidateTrustedCACertificateAndMistmatchChainFails()
        {
            var notBefore = DateTime.Now.Subtract(TimeSpan.FromDays(2));
            var notAfter = DateTime.Now.AddYears(1);
            var (caCert, caKeyPair) = TestCertificateHelper.GenerateSelfSignedCert("MyTestCA", notBefore, notAfter, true);
            var (clientCert, clientKeyPair) = TestCertificateHelper.GenerateSelfSignedCert("MyTestClient", notBefore, notAfter, false);
            var (issuedClientCert, issuedClientKeyPair) = TestCertificateHelper.GenerateCertificate("MyIssuedTestClient", notBefore, notAfter, caCert, caKeyPair, false, null);
            IList<X509Certificate2> trustedCACerts = new List<X509Certificate2>() { caCert };

            Assert.False(CertificateHelper.ValidateClientCert(issuedClientCert, new List<X509Certificate2>() { clientCert }, Option.Some(trustedCACerts), Logger.Factory.CreateLogger("something")));
        }

        [Fact]
        public void TestValidateTrustedCACertificateAndEmptyChainFails()
        {
            var notBefore = DateTime.Now.Subtract(TimeSpan.FromDays(2));
            var notAfter = DateTime.Now.AddYears(1);
            var (caCert, caKeyPair) = TestCertificateHelper.GenerateSelfSignedCert("MyTestCA", notBefore, notAfter, true);
            var (issuedClientCert, issuedClientKeyPair) = TestCertificateHelper.GenerateCertificate("MyIssuedTestClient", notBefore, notAfter, caCert, caKeyPair, false, null);
            IList<X509Certificate2> trustedCACerts = new List<X509Certificate2>() { caCert };

            Assert.False(CertificateHelper.ValidateClientCert(issuedClientCert, new List<X509Certificate2>() { }, Option.Some(trustedCACerts), Logger.Factory.CreateLogger("something")));
        }

        [Fact]
        public void ParseSanUrisTest()
        {
            var notBefore = DateTime.Now.Subtract(TimeSpan.FromDays(2));
            var notAfter = DateTime.Now.AddYears(1);
            var uris = new List<string>() { "aa://bb/cc/dd", "ww://xx/yy/zz" };
            var dnsNames = new List<string>();
            var sans = TestCertificateHelper.PrepareSanEntries(uris, dnsNames);
            var (clientCert, clientKeyPair) = TestCertificateHelper.GenerateCertificate("MyTestClient", notBefore, notAfter, null, null, false, sans);

            IEnumerable<string> difference = uris.Except(CertificateHelper.ParseSanUris(clientCert));
            Assert.False(difference.Any());
        }

        [Fact]
        public void ValidateSanUriTestFails()
        {
            string hub = "hub";
            string deviceId = "did";
            string moduleId = "mid";
            var notBefore = DateTime.Now.Subtract(TimeSpan.FromDays(2));
            var notAfter = DateTime.Now.AddYears(1);
            var uris = new List<string>() { "aa://bb/cc/dd", "ww://xx/yy/zz" };
            var dnsNames = new List<string>();
            var sans = TestCertificateHelper.PrepareSanEntries(uris, dnsNames);
            var (clientCert, clientKeyPair) = TestCertificateHelper.GenerateCertificate("MyTestClient", notBefore, notAfter, null, null, false, sans);

            Assert.False(CertificateHelper.ValidateSanUri(clientCert, hub, deviceId, moduleId));
        }

        [Fact]
        public void ValidateSanUriTestSucceeds()
        {
            string hub = "hub";
            string deviceId = "did";
            string moduleId = "mid";
            var notBefore = DateTime.Now.Subtract(TimeSpan.FromDays(2));
            var notAfter = DateTime.Now.AddYears(1);
            var uris = new List<string>() { "aa://bb/cc/dd", "ww://xx/yy/zz", $"azureiot://{hub}/devices/{deviceId}/modules/{moduleId}" };
            var dnsNames = new List<string>();
            var sans = TestCertificateHelper.PrepareSanEntries(uris, dnsNames);
            var (clientCert, clientKeyPair) = TestCertificateHelper.GenerateCertificate("MyTestClient", notBefore, notAfter, null, null, false, sans);

            Assert.True(CertificateHelper.ValidateSanUri(clientCert, hub, deviceId, moduleId));
        }
    }
}
