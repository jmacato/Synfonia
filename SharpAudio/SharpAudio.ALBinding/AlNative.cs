using System.Diagnostics;
using System.Runtime.InteropServices;
using NativeLibraryLoader;

namespace SharpAudio.ALBinding
{
    public static unsafe partial class AlNative
    {
        private static readonly NativeLibrary m_alLibrary;

        private static NativeLibrary LoadOpenAL()
        {
            string[] names;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                names = new[]
                {
                    "OpenAL32.dll",
                    "soft_oal.dll"
                };
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                names = new[]
                {
                    "libopenal.so",
                    "libopenal.so.1"
                };
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                names = new[]
                {
                    "libopenal.dylib",
                    "soft_oal.so",
                    // HACK TODO: This tries to look up the openal-soft installed from homebrew. 
                    "/opt/homebrew/Cellar/openal-soft/1.21.1/lib/libopenal.1.dylib"
                };
            }
            else
            {
                Debug.WriteLine("Unknown OpenAL platform. Attempting to load \"openal\"");
                names = new[] { "openal" };
            }

            NativeLibrary lib = new NativeLibrary(names);
            return lib;
        }

        private static T LoadFunction<T>(string name)
        {
            return m_alLibrary.LoadFunction<T>(name);
        }

        static AlNative()
        {
            m_alLibrary = LoadOpenAL();

            LoadAlc();
            LoadAl();
        }
    }
}
