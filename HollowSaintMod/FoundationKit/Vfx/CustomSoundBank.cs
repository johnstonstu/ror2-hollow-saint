using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace HollowSaint.FoundationKit.Vfx
{
    /// <summary>Our content bank only; the game continues to own Init and its SFX buses.</summary>
    internal static class CustomSoundBank
    {
        internal static bool Ready { get; private set; }
        private static uint bankId;
        private static bool ownsBank;

        internal static void Load()
        {
            if (Ready) return;
            try
            {
                byte[] bytes;
                using (var resource = Assembly.GetExecutingAssembly().GetManifestResourceStream("HollowSaint.Audio.HollowSaint.bnk"))
                {
                    if (resource == null) throw new FileNotFoundException("Embedded HollowSaint audio bank is missing.");
                    using (var buffer = new MemoryStream())
                    {
                        resource.CopyTo(buffer);
                        bytes = buffer.ToArray();
                    }
                }
                // Wwise requires aligned input. MemoryCopy owns its copy once this
                // synchronous call returns, so the temporary allocation can be freed.
                IntPtr raw = Marshal.AllocHGlobal(checked(bytes.Length + 15));
                AKRESULT result;
                try
                {
                    var aligned = new IntPtr((raw.ToInt64() + 15L) & ~15L);
                    Marshal.Copy(bytes, 0, aligned, bytes.Length);
                    result = AkSoundEngine.LoadBankMemoryCopy(aligned, checked((uint)bytes.Length), out bankId);
                }
                finally { Marshal.FreeHGlobal(raw); }
                ownsBank = result == AKRESULT.AK_Success;
                Ready = ownsBank || result == AKRESULT.AK_BankAlreadyLoaded;
                if (!Ready) throw new InvalidOperationException("LoadBankMemoryCopy returned " + result);
                Plugin.Log.LogInfo("HOLLOW_SAINT_CUSTOM_AUDIO_READY bank=" + bankId + " bytes=" + bytes.Length + " result=" + result);
            }
            catch (Exception error)
            {
                Ready = false;
                Plugin.Log.LogError("HOLLOW_SAINT_CUSTOM_AUDIO_FAILED; using vanilla fallback sounds. " + error);
            }
        }

        internal static void Unload()
        {
            Ready = false;
            if (!ownsBank) return;
            ownsBank = false;
            try
            {
                if (!AkSoundEngine.IsInitialized()) return;
                var result = AkSoundEngine.UnloadBank(bankId, IntPtr.Zero);
                if (result != AKRESULT.AK_Success)
                    Plugin.Log.LogWarning("HOLLOW_SAINT_CUSTOM_AUDIO_UNLOAD bank=" + bankId + " result=" + result);
            }
            catch (Exception error)
            {
                Plugin.Log.LogError("HOLLOW_SAINT_CUSTOM_AUDIO_UNLOAD_FAILED bank=" + bankId + " " + error);
            }
        }
    }
}
