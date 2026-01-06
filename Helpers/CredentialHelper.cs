using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text;

namespace BeanModManager.Helpers
{
    public static class CredentialHelper
    {
        private const string SERVICE_NAME = "BeanModManager";
        private const string EPIC_SESSION_KEY = "epic_session";
        private const int CHUNK_SIZE = 1200;

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool CredWrite(ref Credential credential, uint flags);

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool CredRead(string target, int type, int reservedFlag, out IntPtr credentialPtr);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool CredFree(IntPtr credential);

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool CredDelete(string target, int type, int reservedFlag);

        private const int CRED_TYPE_GENERIC = 1;
        private const int CRED_PERSIST_LOCAL_MACHINE = 2;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct Credential
        {
            public int flags;
            public int type;
            public IntPtr targetName;
            public IntPtr comment;
            public System.Runtime.InteropServices.ComTypes.FILETIME lastWritten;
            public int credentialBlobSize;
            public IntPtr credentialBlob;
            public int persist;
            public int attributeCount;
            public IntPtr attributes;
            public IntPtr targetAlias;
            public IntPtr userName;
        }

        private static byte[] Compress(byte[] data)
        {
            using (var output = new MemoryStream())
            {
                using (var gzip = new GZipStream(output, CompressionMode.Compress, true))
                {
                    gzip.Write(data, 0, data.Length);
                }
                return output.ToArray();
            }
        }

        private static byte[] Decompress(byte[] data)
        {
            using (var input = new MemoryStream(data))
            using (var gzip = new GZipStream(input, CompressionMode.Decompress))
            using (var output = new MemoryStream())
            {
                gzip.CopyTo(output);
                return output.ToArray();
            }
        }

        private static string GetTargetName(string suffix)
        {
            return $"{SERVICE_NAME}_{EPIC_SESSION_KEY}_{suffix}";
        }

        public static void SaveEpicSession(string sessionJson)
        {
            try
            {
                ClearEpicSession();
                var jsonBytes = Encoding.UTF8.GetBytes(sessionJson);
                var compressed = Compress(jsonBytes);
                var encoded = Convert.ToBase64String(compressed);

                var chunks = new List<string>();
                for (int i = 0; i < encoded.Length; i += CHUNK_SIZE)
                {
                    int length = Math.Min(CHUNK_SIZE, encoded.Length - i);
                    chunks.Add(encoded.Substring(i, length));
                }

                SaveCredential("n", chunks.Count.ToString());

                for (int i = 0; i < chunks.Count; i++)
                {
                    SaveCredential(i.ToString(), chunks[i]);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to save Epic session: {ex.Message}", ex);
            }
        }

        private static void SaveCredential(string suffix, string data)
        {
            var targetName = GetTargetName(suffix);
            var bytes = Encoding.UTF8.GetBytes(data);

            var credential = new Credential
            {
                type = CRED_TYPE_GENERIC,
                targetName = Marshal.StringToCoTaskMemUni(targetName),
                credentialBlobSize = bytes.Length,
                credentialBlob = Marshal.AllocCoTaskMem(bytes.Length),
                persist = CRED_PERSIST_LOCAL_MACHINE
            };

            try
            {
                Marshal.Copy(bytes, 0, credential.credentialBlob, bytes.Length);

                if (!CredWrite(ref credential, 0))
                {
                    var error = Marshal.GetLastWin32Error();
                    throw new Exception($"Failed to save credential: Win32 error {error}");
                }
            }
            finally
            {
                Marshal.FreeCoTaskMem(credential.targetName);
                Marshal.FreeCoTaskMem(credential.credentialBlob);
            }
        }

        private static string LoadCredential(string suffix)
        {
            var targetName = GetTargetName(suffix);

            if (!CredRead(targetName, CRED_TYPE_GENERIC, 0, out IntPtr credentialPtr))
            {
                var error = Marshal.GetLastWin32Error();
                if (error == 1168)
                {
                    return null;
                }
                throw new Exception($"Failed to read credential: Win32 error {error}");
            }

            try
            {
                var credential = Marshal.PtrToStructure<Credential>(credentialPtr);
                var bytes = new byte[credential.credentialBlobSize];
                Marshal.Copy(credential.credentialBlob, bytes, 0, credential.credentialBlobSize);
                return Encoding.UTF8.GetString(bytes);
            }
            finally
            {
                CredFree(credentialPtr);
            }
        }

        public static string LoadEpicSession()
        {
            try
            {
                var countStr = LoadCredential("n");
                if (string.IsNullOrEmpty(countStr))
                {
                    return null;
                }

                if (!int.TryParse(countStr, out int count) || count <= 0)
                {
                    return null;
                }

                var chunks = new List<string>();
                for (int i = 0; i < count; i++)
                {
                    var chunk = LoadCredential(i.ToString());
                    if (chunk == null)
                    {
                        return null;
                    }
                    chunks.Add(chunk);
                }

                var encoded = string.Join("", chunks);
                var compressed = Convert.FromBase64String(encoded);
                var jsonBytes = Decompress(compressed);
                return Encoding.UTF8.GetString(jsonBytes);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to load Epic session: {ex.Message}", ex);
            }
        }

        public static void ClearEpicSession()
        {
            try
            {
                var countStr = LoadCredential("n");
                int count = 0;
                if (!string.IsNullOrEmpty(countStr) && int.TryParse(countStr, out count) && count > 0)
                {
                    for (int i = 0; i < count; i++)
                    {
                        var targetName = GetTargetName(i.ToString());
                        CredDelete(targetName, CRED_TYPE_GENERIC, 0);
                    }
                }

                var countTargetName = GetTargetName("n");
                CredDelete(countTargetName, CRED_TYPE_GENERIC, 0);
            }
            catch
            {
                // Ignore errors when clearing
            }
        }
    }
}