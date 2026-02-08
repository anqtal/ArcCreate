using System.Collections.Generic;
using System.Text;

namespace ArcCreate.ChartFormat
{
    /// <summary>
    ///     Error class for a single chart file which may contain multiple errors.
    /// </summary>
    public class ChartFileErrors : Error
    {
        public ChartFileErrors(string file, List<ChartError> errors)
        {
            File = file;
            Errors = errors;
        }

        public string File { get; }

        public List<ChartError> Errors { get; }

        public override string Message
        {
            get
            {
                var content = new StringBuilder();
                foreach (var error in Errors) content.AppendLine(error.Message);

                content.ToString();
                return I18n.S("Format.Exception.File", new Dictionary<string, object>
                {
                    { "File", File },
                    { "Error", content }
                });
            }
        }
    }
}