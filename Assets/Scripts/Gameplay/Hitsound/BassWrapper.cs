namespace ArcCreate.Gameplay.Hitsound
{
    using System;
    using System.Runtime.InteropServices;

    public static class BassWrapper
    {
        private const string BASS_LIB = "@rpath/bass.framework/bass"; // 适用于 iOS

        [DllImport(BASS_LIB, EntryPoint = "BASS_Init")]
        public static extern bool BASS_Init(int device, int freq, int flags, IntPtr win);

        [DllImport(BASS_LIB, EntryPoint = "BASS_Free")]
        public static extern bool BASS_Free();

        [DllImport(BASS_LIB, EntryPoint = "BASS_StreamCreateFile")]
        public static extern int BASS_StreamCreateFile(bool mem, string file, long offset, long length, int flags);

        [DllImport(BASS_LIB, EntryPoint = "BASS_ChannelPlay")]
        public static extern bool BASS_ChannelPlay(int handle, bool restart);

        [DllImport(BASS_LIB, EntryPoint = "BASS_ChannelStop")]
        public static extern bool BASS_ChannelStop(int handle);
        
        [DllImport(BASS_LIB, EntryPoint = "BASS_ErrorGetCode")]
        public static extern int BASS_ErrorGetCode();

        [DllImport(BASS_LIB, EntryPoint = "BASS_StreamFree")]
        public static extern bool BASS_StreamFree(int handle);
    }
}