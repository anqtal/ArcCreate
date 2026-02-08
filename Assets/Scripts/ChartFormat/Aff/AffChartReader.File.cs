using System.IO;
using ArcCreate.Utility.Parser;

namespace ArcCreate.ChartFormat
{
    /// <summary>
    ///     Object for reading a .aff chart file.
    /// </summary>
    public partial class AffChartReader : ChartReader
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="AffChartReader" /> class. You should use
        ///     <see cref="ChartReaderFactory" /> to instantiate instead.
        /// </summary>
        /// <param name="fileAccess">File access wrapper. You should normally use <see cref="PhysicalFileAccess" />.</param>
        /// <param name="relativeDirectory">The directory relative to the base folder.</param>
        /// <param name="fullPath">The absolute path leading to the file.</param>
        /// <param name="filename">The file name. Passed as-is from include and fragment aff commands.</param>
        public AffChartReader(IFileAccessWrapper fileAccess, string relativeDirectory, string fullPath, string filename)
            : base(fileAccess, relativeDirectory, fullPath, filename)
        {
            TimingPointDensity = 1;
            AudioOffset = 0;
        }

        public override Result<ChartError> ParseLine(string line, string path, int lineNumber)
        {
            var type = DetermineType(line);
            switch (type)
            {
                case RawEventType.Timing:
                    if (!ParseTiming(line, lineNumber).TryUnwrap(out var timing, out var e)) return e;

                    Events.Add(timing);
                    break;

                case RawEventType.Tap:
                    if (!ParseTap(line, lineNumber).TryUnwrap(out var tap, out e)) return e;

                    GuideSoundTiming.Add(tap.Timing);
                    Events.Add(tap);
                    break;

                case RawEventType.Hold:
                    if (!ParseHold(line, lineNumber).TryUnwrap(out var hold, out e)) return e;
                    GuideSoundTiming.Add(hold.Timing);
                    GuideSoundTiming.Add(hold.EndTiming);
                    Events.Add(hold);
                    break;

                case RawEventType.Arc:
                    if (!ParseArc(line, lineNumber).TryUnwrap(out var arc, out e)) return e;

                    if (!arc.IsTrace) GuideSoundTiming.Add(arc.Timing);
                    if (arc.ArcTaps != null && arc.ArcTaps.Count != 0)
                        foreach (var rawArcTap in arc.ArcTaps)
                            GuideSoundTiming.Add(rawArcTap.Timing);

                    Events.Add(arc);
                    break;

                case RawEventType.Camera:
                    if (!ParseCamera(line, lineNumber).TryUnwrap(out var cam, out e)) return e;

                    Events.Add(cam);
                    break;

                case RawEventType.SceneControl:
                    if (!ParseSceneControl(line, lineNumber).TryUnwrap(out var sc, out e)) return e;

                    Events.Add(sc);
                    break;

                case RawEventType.TimingGroup:
                    TotalTimingGroup++;
                    CurrentTimingGroup = TotalTimingGroup - 1;
                    if (!ParseTimingGroup(line, lineNumber, Filename).TryUnwrap(out var tg, out e)) return e;

                    TimingGroups.Add(tg);
                    break;

                case RawEventType.TimingGroupEnd:
                    CurrentTimingGroup = 0;
                    break;

                case RawEventType.Include:
                    if (!ParseInclude(line, lineNumber).TryUnwrap(out var inclPath, out e)) return e;

                    var fullInclPath = SwitchFileName(FullPath, inclPath);

                    if (AllIncludes.Contains(fullInclPath))
                        return ChartError.Property(
                            line,
                            lineNumber,
                            RawEventType.Include,
                            0,
                            line.Length,
                            ChartError.Kind.IncludeReferencedMultipleTimes);

                    if (AllFragments.Contains(fullInclPath))
                        return ChartError.Property(
                            line,
                            lineNumber,
                            RawEventType.Fragment,
                            0,
                            line.Length,
                            ChartError.Kind.IncludeAReferencedFragment);

                    var includeResult = AddInclude(inclPath);
                    if (includeResult.IsError)
                        return ChartError.ReferencedFile(line, lineNumber, RawEventType.Include, includeResult.Error);

                    break;

                case RawEventType.Fragment:
                    if (!ParseFragment(line, lineNumber).TryUnwrap(out var fragment, out e)) return e;

                    var fullFragPath = SwitchFileName(FullPath, fragment.File);

                    if (AllIncludes.Contains(fullFragPath))
                        return ChartError.Property(
                            line,
                            lineNumber,
                            RawEventType.Fragment,
                            0,
                            line.Length,
                            ChartError.Kind.IncludeReferencedMultipleTimes);

                    var fragmentResult = AddFragment(fragment.Timing, fragment.File);
                    if (fragmentResult.IsError)
                        return ChartError.ReferencedFile(line, lineNumber, RawEventType.Fragment, fragmentResult.Error);

                    break;
            }

            return Result<ChartError>.Ok();
        }

        public override Result<ChartError> ParseHeaderLine(string line, int lineNumber, string path,
            out bool endOfHeader)
        {
            endOfHeader = line == "-";
            if (endOfHeader) return Result<ChartError>.Ok();

            var s = new StringParser(line);
            if (!s.ReadString(":").TryUnwrap(out var headerType, out var e))
                return ChartError.Parsing(line, lineNumber, RawEventType.Header, e);

            switch (headerType)
            {
                case "AudioOffset":
                    if (!s.ReadInt().TryUnwrap(out var offset, out e))
                        return ChartError.Parsing(line, lineNumber, RawEventType.Header, e);

                    AudioOffset = offset;
                    return Result<ChartError>.Ok();

                case "TimingPointDensityFactor":
                case "TimingPointsDensityFactor":
                    if (!s.ReadFloat().TryUnwrap(out var density, out e))
                        return ChartError.Parsing(line, lineNumber, RawEventType.Header, e);

                    TimingPointDensity = density;
                    return Result<ChartError>.Ok();
            }

            return ChartError.Property(line, lineNumber, RawEventType.Header, 0, line.Length,
                ChartError.Kind.InvalidHeader);
        }

        public override Result<ChartError> FinalValidity()
        {
            var b = base.FinalValidity();
            if (b.IsError) return b;

            if (CurrentTimingGroup != 0)
                return ChartError.Format(
                    RawEventType.TimingGroup,
                    ChartError.Kind.TimingGroupPairInvalid);

            return Result<ChartError>.Ok();
        }

        private RawEventType DetermineType(string line)
        {
            if (line.StartsWith("(")) return RawEventType.Tap;

            if (line.StartsWith("timing(")) return RawEventType.Timing;

            if (line.StartsWith("hold(")) return RawEventType.Hold;

            if (line.StartsWith("arc(")) return RawEventType.Arc;

            if (line.StartsWith("camera(")) return RawEventType.Camera;

            if (line.StartsWith("scenecontrol(")) return RawEventType.SceneControl;

            if (line.StartsWith("timinggroup(")) return RawEventType.TimingGroup;

            if (line.StartsWith("include(")) return RawEventType.Include;

            if (line.StartsWith("fragment(")) return RawEventType.Fragment;

            if (line.StartsWith("};")) return RawEventType.TimingGroupEnd;

            return RawEventType.Unknown;
        }

        private Result<ChartFileErrors> AddInclude(string file)
        {
            AllIncludes.Add(SwitchFileName(FullPath, file));
            Events.Add(new RawInclude
            {
                Timing = 0,
                Type = RawEventType.Include,
                TimingGroup = CurrentTimingGroup,
                File = file
            });

            var extReader = ChartReaderFactory.GetReader(FileAccess, FullPath, file);
            extReader.BlockReferences(AllIncludes, AllFragments);
            var parseResult = extReader.Parse();
            if (parseResult.IsError) return parseResult.Error;

            foreach (var group in extReader.TimingGroups)
            {
                group.Editable = true;
                group.File = Path.Combine(RelativeDirectory, group.File);
            }

            References.Add(extReader);
            return Result<ChartFileErrors>.Ok();
        }

        private Result<ChartFileErrors> AddFragment(int timing, string file)
        {
            AllFragments.Add(SwitchFileName(FullPath, file));
            Events.Add(new RawFragment
            {
                Timing = timing,
                Type = RawEventType.Fragment,
                TimingGroup = CurrentTimingGroup,
                File = file
            });

            var extReader = ChartReaderFactory.GetReader(FileAccess, FullPath, file);
            extReader.BlockReferences(AllIncludes, AllFragments);
            var parseResult = extReader.Parse();
            if (parseResult.IsError) return parseResult.Error;

            foreach (var group in extReader.TimingGroups)
            {
                group.Editable = false;
                group.File = Path.Combine(RelativeDirectory, group.File);
            }

            foreach (var e in extReader.Events)
            {
                if (!(e is RawTiming && e.Timing == 0)) e.Timing += timing;

                if (e is RawHold) (e as RawHold).EndTiming += timing;

                if (e is RawArc) (e as RawArc).EndTiming += timing;
            }

            References.Add(extReader);
            return Result<ChartFileErrors>.Ok();
        }
    }
}