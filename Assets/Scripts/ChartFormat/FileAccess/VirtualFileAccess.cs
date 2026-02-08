using System;
using System.IO;

namespace ArcCreate.ChartFormat
{
    public class VirtualFileAccess : IFileAccessWrapper
    {
        private readonly string[] data;
        private readonly StreamWriter streamWriter;

        public VirtualFileAccess(string data)
        {
            this.data = data.Split('\n');
        }

        public VirtualFileAccess(Stream stream)
        {
            streamWriter = new StreamWriter(stream);
        }

        public Uri GetFileUri(string path)
        {
            return new Uri(path);
        }

        public Option<string[]> ReadFileByLines(string path)
        {
            return data;
        }

        public StreamWriter WriteFile(string path)
        {
            return streamWriter;
        }
    }
}