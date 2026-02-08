namespace ArcCreate.Data
{
    public class SkinSettings
    {
        public string Side { get; set; }

        public string Note { get; set; }

        public string Particle { get; set; }

        public string Track { get; set; }

        public string Accent { get; set; }

        public string SingleLine { get; set; }

        public SkinSettings Clone()
        {
            return new SkinSettings
            {
                Side = Side,
                Note = Note,
                Particle = Particle,
                Track = Track,
                Accent = Accent,
                SingleLine = SingleLine
            };
        }
    }
}