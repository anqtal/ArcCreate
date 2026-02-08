using System.Collections.Generic;

namespace ArcCreate.Data
{
    public class ColorSettings
    {
        public string Trace { get; set; }

        public string Shadow { get; set; }

        public List<string> Arc { get; set; } = new();

        public List<string> ArcLow { get; set; } = new();

        public ColorSettings Clone()
        {
            return new ColorSettings
            {
                Trace = Trace,
                Shadow = Shadow,
                Arc = new List<string>(Arc),
                ArcLow = new List<string>(ArcLow)
            };
        }
    }
}