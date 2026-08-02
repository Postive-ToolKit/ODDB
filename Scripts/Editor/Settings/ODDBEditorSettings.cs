using System;
using System.Security.Cryptography;
using System.Text;
using TeamODD.ODDB.Runtime.Attributes;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TeamODD.ODDB.Editors.Settings
{
    /// <summary>
    /// Editor-only ODDB settings. Resolved through AssetDatabase by type so
    /// the settings asset can be moved without breaking lookups.
    /// Runtime fields live on ODDBRuntimeSettings.
    /// </summary>
    public class ODDBEditorSettings : ScriptableObject
    {
        private const string DefaultFolderPath = "Assets/Settings";
        private const string DefaultAssetPath = DefaultFolderPath + "/ODDBEditorSettings.asset";
        private const string LegacyAssetPath = "Assets/Editor/ODDBEditorSettings.asset";
        private static ODDBEditorSettings _cachedSetting;

        /// <summary>Pure read; returns null if the asset doesn't exist yet. No side effects.</summary>
        public static ODDBEditorSettings TryLoad()
        {
#if UNITY_EDITOR
            var guids = AssetDatabase.FindAssets($"t:{nameof(ODDBEditorSettings)}");
            ODDBEditorSettings fallback = null;
            var fallbackPath = string.Empty;

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var settings = AssetDatabase.LoadAssetAtPath<ODDBEditorSettings>(path);
                if (settings == null)
                    continue;

                if (path == DefaultAssetPath)
                    return settings;

                if (fallback == null || IsPreferredFallback(path, fallbackPath))
                {
                    fallback = settings;
                    fallbackPath = path;
                }
            }

            return fallback;
#else
            return null;
#endif
        }

        public static ODDBEditorSettings Setting
        {
            get
            {
#if UNITY_EDITOR
                // AssetDatabase.FindAssets is project-wide and this property is read
                // while binding every visible view-reference cell. Unity keeps the
                // ScriptableObject instance updated when its asset changes, so reuse
                // it until it is deleted or the domain reloads.
                if (_cachedSetting != null)
                    return _cachedSetting;

                var s = TryLoad();
                if (s != null)
                {
                    _cachedSetting = s;
                    return s;
                }
                s = CreateInstance<ODDBEditorSettings>();
                s.name = "ODDBEditorSettings";
                EnsureFolder(DefaultFolderPath);
                AssetDatabase.CreateAsset(s, DefaultAssetPath);
                AssetDatabase.SaveAssets();
                _cachedSetting = s;
                return s;
#else
                return null;
#endif
            }
        }

        public int MaxHistoryCount => _maxHistoryCount;
        public bool UseFirstColumnAsRowName => _useFirstColumnAsRowName;
        public string GeneratedCodePath => _generatedCodePath;
        public bool DisableGoogleSheetExport => _disableGoogleSheetExport;
        public string GoogleSpreadsheetId => _googleSpreadsheetId;
        public bool ConfirmGoogleSheetColumnDeletion => _confirmGoogleSheetColumnDeletion;

        internal bool HasGoogleOAuthClientConfiguration =>
            TryGetGoogleOAuthClientConfiguration(out _, out _, out _);

        internal bool TryGetGoogleOAuthClientConfiguration(
            out string clientId,
            out string clientSecret,
            out string failureReason)
        {
            clientId = string.Empty;
            clientSecret = string.Empty;

            if (string.IsNullOrEmpty(_googleOAuthClientIdEncrypted)
                || string.IsNullOrEmpty(_googleOAuthClientSecretEncrypted))
            {
                failureReason = "Enter and save a Google OAuth Desktop Client ID and Client Secret in ODDBEditorSettings.";
                return false;
            }

            if (!ODDBEditorSecretCipher.TryDecrypt(_googleOAuthClientIdEncrypted, out clientId)
                || !ODDBEditorSecretCipher.TryDecrypt(_googleOAuthClientSecretEncrypted, out clientSecret))
            {
                clientId = string.Empty;
                clientSecret = string.Empty;
                failureReason = "The encrypted Google OAuth client configuration could not be read. Clear it and enter the credentials again.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
            {
                failureReason = "The saved Google OAuth client configuration is incomplete.";
                return false;
            }

            failureReason = string.Empty;
            return true;
        }

        internal bool SetGoogleOAuthClientConfiguration(string clientId, string clientSecret)
        {
            clientId = clientId?.Trim() ?? string.Empty;
            clientSecret = clientSecret?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(clientId))
                throw new ArgumentException("Google OAuth Client ID is required.", nameof(clientId));
            if (!clientId.EndsWith(".apps.googleusercontent.com", StringComparison.Ordinal))
                throw new ArgumentException("Google OAuth Client ID must end with .apps.googleusercontent.com.", nameof(clientId));
            if (string.IsNullOrEmpty(clientSecret))
                throw new ArgumentException("Google OAuth Client Secret is required.", nameof(clientSecret));

            var changed = !TryGetGoogleOAuthClientConfiguration(
                              out var existingClientId,
                              out var existingClientSecret,
                              out _)
                          || !string.Equals(existingClientId, clientId, StringComparison.Ordinal)
                          || !string.Equals(existingClientSecret, clientSecret, StringComparison.Ordinal);
            if (!changed)
                return false;

            _googleOAuthClientIdEncrypted = ODDBEditorSecretCipher.Encrypt(clientId);
            _googleOAuthClientSecretEncrypted = ODDBEditorSecretCipher.Encrypt(clientSecret);
            return true;
        }

        internal void ClearGoogleOAuthClientConfiguration()
        {
            _googleOAuthClientIdEncrypted = string.Empty;
            _googleOAuthClientSecretEncrypted = string.Empty;
        }

        public bool EnableMCPServer => _enableMCPServer;
        public int MCPServerPort => _mcpServerPort;
        public string MCPServerHost => _mcpServerHost;
        public bool MCPServerVerbose => _mcpServerVerbose;

        [Header("Editor Settings")]
        [Tooltip("The maximum number of history items to keep in the undo stack.")]
        [SerializeField, Min(1)] private int _maxHistoryCount = 50;
        [Tooltip("Use the first column of the row as the row name when show dropdown selector in the editor.")]
        [SerializeField] private bool _useFirstColumnAsRowName = false;

        [Space(10)]
        [Header("Code Generation")]
        [Tooltip("Output folder for generated POCO classes (Assets-relative). Leave empty to disable code generation.")]
        [PathSelector(true)]
        [SerializeField] private string _generatedCodePath = string.Empty;

        [Space(10)]
        [Header("MCP Server")]
        [Tooltip("Enable the in-Editor MCP server that exposes ODDB to AI clients via HTTP.")]
        [SerializeField] private bool _enableMCPServer = true;
        [Tooltip("TCP port for the MCP HTTP server. If busy, ODDB retries this same port instead of switching ports.")]
        [SerializeField] private int _mcpServerPort = 9123;
        [Tooltip("Bind host. 127.0.0.1 keeps the server loopback-only.")]
        [SerializeField] private string _mcpServerHost = "127.0.0.1";
        [Tooltip("Log every MCP call to the Unity console.")]
        [SerializeField] private bool _mcpServerVerbose = false;

        [Space(10)]
        [Header("Google Sheets Settings")]
        [SerializeField] private bool _disableGoogleSheetExport = false;
        [Tooltip("The spreadsheet ID from the Google Sheets document URL.")]
        [SerializeField] private string _googleSpreadsheetId = string.Empty;
        [Tooltip("Ask for confirmation before an export physically deletes ODDB-managed columns.")]
        [SerializeField] private bool _confirmGoogleSheetColumnDeletion = true;
        [SerializeField, HideInInspector] private string _googleOAuthClientIdEncrypted = string.Empty;
        [SerializeField, HideInInspector] private string _googleOAuthClientSecretEncrypted = string.Empty;

        // Kept serialized for one migration cycle so existing assets do not lose
        // their values before users finish moving away from Apps Script.
        [SerializeField, HideInInspector] private string _googleSheetAPIURL = string.Empty;
        [SerializeField, HideInInspector] private string _googleSheetAPISecretKey = string.Empty;

#if UNITY_EDITOR
        private static void EnsureFolder(string folderPath)
        {
            var parts = folderPath.Split('/');
            var current = parts[0];

            for (var i = 1; i < parts.Length; i++)
            {
                var next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        private static bool IsPreferredFallback(string path, string currentPath)
        {
            if (path == LegacyAssetPath)
                return currentPath != LegacyAssetPath;

            if (currentPath == LegacyAssetPath)
                return false;

            return string.Compare(path, currentPath, StringComparison.OrdinalIgnoreCase) < 0;
        }
#endif
    }

    internal static class ODDBEditorSecretCipher
    {
        private const string Prefix = "v1:";
        private static readonly byte[] EncryptionKey = DeriveKey("TeamODD.ODDB.EditorSettings.Encryption.v1");
        private static readonly byte[] AuthenticationKey = DeriveKey("TeamODD.ODDB.EditorSettings.Authentication.v1");

        public static string Encrypt(string plaintext)
        {
            if (string.IsNullOrEmpty(plaintext))
                return string.Empty;

            byte[] iv;
            byte[] ciphertext;
            using (var aes = Aes.Create())
            {
                aes.Key = EncryptionKey;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                aes.GenerateIV();
                iv = aes.IV;

                using (var encryptor = aes.CreateEncryptor())
                {
                    var bytes = Encoding.UTF8.GetBytes(plaintext);
                    ciphertext = encryptor.TransformFinalBlock(bytes, 0, bytes.Length);
                }
            }

            var authenticatedData = Combine(iv, ciphertext);
            byte[] signature;
            using (var hmac = new HMACSHA256(AuthenticationKey))
                signature = hmac.ComputeHash(authenticatedData);

            return Prefix + Convert.ToBase64String(Combine(authenticatedData, signature));
        }

        public static bool TryDecrypt(string encrypted, out string plaintext)
        {
            plaintext = string.Empty;
            if (string.IsNullOrEmpty(encrypted)
                || !encrypted.StartsWith(Prefix, StringComparison.Ordinal))
            {
                return false;
            }

            try
            {
                var payload = Convert.FromBase64String(encrypted.Substring(Prefix.Length));
                const int ivLength = 16;
                const int signatureLength = 32;
                if (payload.Length <= ivLength + signatureLength)
                    return false;

                var authenticatedLength = payload.Length - signatureLength;
                var authenticatedData = Slice(payload, 0, authenticatedLength);
                var signature = Slice(payload, authenticatedLength, signatureLength);
                byte[] expectedSignature;
                using (var hmac = new HMACSHA256(AuthenticationKey))
                    expectedSignature = hmac.ComputeHash(authenticatedData);
                if (!FixedTimeEquals(signature, expectedSignature))
                    return false;

                var iv = Slice(authenticatedData, 0, ivLength);
                var ciphertext = Slice(authenticatedData, ivLength, authenticatedLength - ivLength);
                using (var aes = Aes.Create())
                {
                    aes.Key = EncryptionKey;
                    aes.IV = iv;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;
                    using (var decryptor = aes.CreateDecryptor())
                    {
                        var bytes = decryptor.TransformFinalBlock(ciphertext, 0, ciphertext.Length);
                        plaintext = Encoding.UTF8.GetString(bytes);
                        return true;
                    }
                }
            }
            catch (CryptographicException)
            {
                return false;
            }
            catch (FormatException)
            {
                return false;
            }
        }

        private static byte[] DeriveKey(string purpose)
        {
            using (var sha256 = SHA256.Create())
                return sha256.ComputeHash(Encoding.UTF8.GetBytes(purpose));
        }

        private static byte[] Combine(byte[] first, byte[] second)
        {
            var result = new byte[first.Length + second.Length];
            Buffer.BlockCopy(first, 0, result, 0, first.Length);
            Buffer.BlockCopy(second, 0, result, first.Length, second.Length);
            return result;
        }

        private static byte[] Slice(byte[] source, int offset, int length)
        {
            var result = new byte[length];
            Buffer.BlockCopy(source, offset, result, 0, length);
            return result;
        }

        private static bool FixedTimeEquals(byte[] left, byte[] right)
        {
            if (left == null || right == null || left.Length != right.Length)
                return false;

            var difference = 0;
            for (var index = 0; index < left.Length; index++)
                difference |= left[index] ^ right[index];
            return difference == 0;
        }
    }
}
