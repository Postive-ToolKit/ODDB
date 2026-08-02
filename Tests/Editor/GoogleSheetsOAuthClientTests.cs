using System;
using System.Linq;
using NUnit.Framework;
using TeamODD.ODDB.Editors.Settings;
using TeamODD.ODDB.Editors.Utils.Sheets.GoogleSheets;
using UnityEditor;
using UnityEngine;

namespace TeamODD.ODDB.Tests.Editor
{
    public sealed class GoogleSheetsOAuthClientTests
    {
        [Test]
        public void CreateCodeVerifier_ReturnsPkceCompatibleRandomValue()
        {
            var first = GoogleSheetsOAuthClient.CreateCodeVerifier();
            var second = GoogleSheetsOAuthClient.CreateCodeVerifier();

            Assert.That(first.Length, Is.InRange(43, 128));
            Assert.That(first.All(IsPkceCharacter), Is.True);
            Assert.That(second, Is.Not.EqualTo(first));
        }

        [Test]
        public void CreateCodeChallenge_UsesSha256Base64Url()
        {
            const string verifier = "dBjftJeZ4CVP-mB92K27uhbUJU1p1r_wW1gFWFOEjXk";

            var challenge = GoogleSheetsOAuthClient.CreateCodeChallenge(verifier);

            Assert.That(challenge, Is.EqualTo("E9Melhoa2OwvFrEMTJguCHaoeK1t8URWbuGJSstw-cM"));
        }

        [Test]
        public void BuildAuthorizationUrl_ContainsClientIdAndPkceWithoutSecret()
        {
            const string clientId = "test-client.apps.googleusercontent.com";
            var url = GoogleSheetsOAuthClient.BuildAuthorizationUrl(
                clientId,
                "http://127.0.0.1:49152/oauth2callback/",
                "state-value",
                "challenge-value");

            Assert.That(url, Does.Contain(Uri.EscapeDataString(clientId)));
            Assert.That(url, Does.Contain("code_challenge=challenge-value"));
            Assert.That(url, Does.Contain("code_challenge_method=S256"));
            Assert.That(url, Does.Not.Contain("client_secret"));
        }

        [Test]
        public void EditorSecretCipher_RoundTripsWithoutPlaintextStorage()
        {
            const string plaintext = "desktop-oauth-client-secret";

            var encrypted = ODDBEditorSecretCipher.Encrypt(plaintext);

            Assert.That(encrypted, Does.Not.Contain(plaintext));
            Assert.That(ODDBEditorSecretCipher.TryDecrypt(encrypted, out var decrypted), Is.True);
            Assert.That(decrypted, Is.EqualTo(plaintext));
        }

        [Test]
        public void EditorSecretCipher_RejectsTamperedPayload()
        {
            var encrypted = ODDBEditorSecretCipher.Encrypt("desktop-oauth-client-secret");
            var last = encrypted[encrypted.Length - 1];
            var tampered = encrypted.Substring(0, encrypted.Length - 1) + (last == 'A' ? 'B' : 'A');

            Assert.That(ODDBEditorSecretCipher.TryDecrypt(tampered, out _), Is.False);
        }

        [Test]
        public void EditorSettings_StoresOAuthClientEncryptedAndRoundTrips()
        {
            const string clientId = "unit-test-client" + ".apps.googleusercontent.com";
            const string clientSecret = "unit-test-client-secret";
            var settings = ScriptableObject.CreateInstance<ODDBEditorSettings>();

            try
            {
                Assert.That(settings.SetGoogleOAuthClientConfiguration(clientId, clientSecret), Is.True);
                Assert.That(
                    settings.TryGetGoogleOAuthClientConfiguration(
                        out var restoredClientId,
                        out var restoredClientSecret,
                        out var failureReason),
                    Is.True,
                    failureReason);
                Assert.That(restoredClientId, Is.EqualTo(clientId));
                Assert.That(restoredClientSecret, Is.EqualTo(clientSecret));

                var serialized = new SerializedObject(settings);
                Assert.That(
                    serialized.FindProperty("_googleOAuthClientIdEncrypted").stringValue,
                    Does.Not.Contain(clientId));
                Assert.That(
                    serialized.FindProperty("_googleOAuthClientSecretEncrypted").stringValue,
                    Does.Not.Contain(clientSecret));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(settings);
            }
        }

        [Test]
        public void ParseQuery_DecodesCallbackValues()
        {
            var values = GoogleSheetsOAuthClient.ParseQuery("?code=a%2Fb%2Bc&state=hello+world");

            Assert.That(values["code"], Is.EqualTo("a/b+c"));
            Assert.That(values["state"], Is.EqualTo("hello world"));
        }

        private static bool IsPkceCharacter(char value)
        {
            return char.IsLetterOrDigit(value) || value == '-' || value == '.' || value == '_' || value == '~';
        }
    }
}
