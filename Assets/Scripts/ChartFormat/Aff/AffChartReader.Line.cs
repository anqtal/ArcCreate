using System.Collections.Generic;
using System.IO;
using ArcCreate.Utility.Parser;
using UnityEngine;

namespace ArcCreate.ChartFormat
{
    /// <summary>
    ///     Object for reading a .aff chart file.
    /// </summary>
    public partial class AffChartReader
    {
        /// <summary>
        ///     Parse a timing aff line of the format "timing([start],[bpm],[divisor]);".
        /// </summary>
        /// <param name="line">The string to parse.</param>
        /// <param name="lineNumber">The line number of the event.</param>
        /// <returns>The parsed object.</returns>
        public Result<RawTiming, ChartError> ParseTiming(string line, int lineNumber)
        {
            var s = new StringParser(line);
            s.Skip("timing(".Length);
            if (!s.ReadInt(",").TryUnwrap(out var tick, out var e)
                || !s.ReadFloat(",").TryUnwrap(out var bpm, out e)
                || !s.ReadFloat(")").TryUnwrap(out var divisor, out e))
                return ChartError.Parsing(line, lineNumber, RawEventType.Timing, e);

            if (divisor < 0)
                return ChartError.Property(line, lineNumber, RawEventType.Timing, divisor.StartPos, divisor.Length,
                    ChartError.Kind.DivisorNegative);

            return new RawTiming
            {
                Timing = tick,
                Divisor = divisor,
                Bpm = bpm,
                Type = RawEventType.Timing,
                TimingGroup = CurrentTimingGroup,
                Line = lineNumber
            };
        }

        /// <summary>
        ///     Parse a tap aff line of the format "([timing],[lane]);".
        /// </summary>
        /// <param name="line">The string to parse.</param>
        /// <param name="lineNumber">The line number of the event.</param>
        /// <returns>The parsed object.</returns>
        public Result<RawTap, ChartError> ParseTap(string line, int lineNumber)
        {
            var s = new StringParser(line);
            s.Skip(1);
            if (!s.ReadInt(",").TryUnwrap(out var tick, out var e)
                || !s.ReadInt(")").TryUnwrap(out var lane, out e))
                return ChartError.Parsing(line, lineNumber, RawEventType.Tap, e);

            return new RawTap
            {
                Timing = tick,
                Lane = lane,
                Type = RawEventType.Tap,
                TimingGroup = CurrentTimingGroup,
                Line = lineNumber
            };
        }

        /// <summary>
        ///     Parse a hold aff line of the format "hold([timing],[endTiming],[lane]);".
        /// </summary>
        /// <param name="line">The string to parse.</param>
        /// <param name="lineNumber">The line number of the event.</param>
        /// <returns>The parsed object.</returns>
        public Result<RawHold, ChartError> ParseHold(string line, int lineNumber)
        {
            var s = new StringParser(line);
            s.Skip("hold(".Length);
            if (!s.ReadInt(",").TryUnwrap(out var tick, out var e)
                || !s.ReadInt(",").TryUnwrap(out var endtick, out e)
                || !s.ReadInt(")").TryUnwrap(out var track, out e))
                return ChartError.Parsing(line, lineNumber, RawEventType.Hold, e);

            if (endtick <= tick)
                return ChartError.Property(
                    line,
                    lineNumber,
                    RawEventType.Hold,
                    tick.StartPos,
                    endtick.StartPos + endtick.Length - tick.StartPos,
                    endtick == tick ? ChartError.Kind.DurationZero : ChartError.Kind.DurationNegative);

            return new RawHold
            {
                Timing = tick,
                EndTiming = endtick,
                Lane = track,
                Type = RawEventType.Hold,
                TimingGroup = CurrentTimingGroup,
                Line = lineNumber
            };
        }

        /// <summary>
        ///     Parse an arc aff line of the format
        ///     "arc([start],[end],[startx],[endx],[type],[starty],[endy],[color],[sfx],[istrace])[arctap([timing]),...];".
        /// </summary>
        /// <param name="line">The string to parse.</param>
        /// <param name="lineNumber">The line number of the event.</param>
        /// <returns>The parsed object.</returns>
        public Result<RawArc, ChartError> ParseArc(string line, int lineNumber)
        {
            var s = new StringParser(line);
            s.Skip("arc(".Length);
            if (!s.ReadInt(",").TryUnwrap(out var tick, out var e)
                || !s.ReadInt(",").TryUnwrap(out var endtick, out e)
                || !s.ReadFloat(",").TryUnwrap(out var startx, out e)
                || !s.ReadFloat(",").TryUnwrap(out var endx, out e)
                || !s.ReadString(",").TryUnwrap(out var linetype, out e)
                || !s.ReadFloat(",").TryUnwrap(out var starty, out e)
                || !s.ReadFloat(",").TryUnwrap(out var endy, out e)
                || !s.ReadInt(",").TryUnwrap(out var color, out e)
                || !s.ReadString(",").TryUnwrap(out var effect, out e)
                || !s.ReadBool(")").TryUnwrap(out var istrace, out e))
                return ChartError.Parsing(line, lineNumber, RawEventType.Arc, e);

            List<RawArcTap> arctap = null;

            if (color < 0)
                return ChartError.Property(line, lineNumber, RawEventType.Arc, color.StartPos, color.Length,
                    ChartError.Kind.ArcColorNegative);

            if (s.Current != ';')
            {
                arctap = new List<RawArcTap>();
                while (true)
                {
                    var startCharPos = s.Pos + 1;
                    float width = 1;
                    s.Skip("[arctap(".Length);
                    if (!s.ReadString(")").TryUnwrap(out var args, out var ae))
                        return ChartError.Parsing(line, lineNumber, RawEventType.ArcTap, ae);

                    var split = args.Value.Split(',');
                    if (split.Length == 2)
                        if (!Evaluator.TryFloat(split[1], out width))
                            return ChartError.Parsing(line, lineNumber, RawEventType.ArcTap, new ParsingError(
                                line,
                                args.StartPos + args.Value.IndexOf(','),
                                split[2].Length,
                                ParsingError.Kind.InvalidConversionToFloat));

                    if (!Evaluator.TryInt(split[0], out var timing))
                        return ChartError.Parsing(line, lineNumber, RawEventType.ArcTap, new ParsingError(
                            line,
                            args.StartPos,
                            split[0].Length,
                            ParsingError.Kind.InvalidConversionToInt));

                    var length = s.Pos - startCharPos;
                    if (timing < tick || timing > endtick)
                        return ChartError.Property(line, lineNumber, RawEventType.ArcTap, args.StartPos,
                            split[0].Length, ChartError.Kind.ArcTapOutOfRange);

                    arctap.Add(new RawArcTap
                    {
                        Type = RawEventType.ArcTap,
                        Timing = timing,
                        TimingGroup = CurrentTimingGroup,
                        Width = width,
                        Line = lineNumber,
                        CharacterStart = startCharPos,
                        Length = length
                    });

                    if (s.Current != ',') break;
                }
            }

            if (endtick < tick)
                return ChartError.Property(
                    line,
                    lineNumber,
                    RawEventType.Arc,
                    tick.StartPos,
                    endtick.StartPos + endtick.Length - tick.StartPos,
                    ChartError.Kind.DurationNegative);

            return new RawArc
            {
                Timing = tick,
                EndTiming = endtick,
                XStart = startx,
                XEnd = endx,
                LineType = linetype,
                YStart = starty,
                YEnd = endy,
                Color = color,
                IsTrace = istrace,
                Type = RawEventType.Arc,
                ArcTaps = arctap,
                Sfx = effect,
                TimingGroup = CurrentTimingGroup,
                Line = lineNumber
            };
        }

        /// <summary>
        ///     Parse a camera aff line of the format
        ///     "camera([timing],[x],[y],[z],[rotx],[roty],[rotz],[type],[duration]);".
        /// </summary>
        /// <param name="line">The string to parse.</param>
        /// <param name="lineNumber">The line number of the event.</param>
        /// <returns>The parsed object.</returns>
        public Result<RawCamera, ChartError> ParseCamera(string line, int lineNumber)
        {
            var s = new StringParser(line);
            s.Skip("camera(".Length);
            if (!s.ReadInt(",").TryUnwrap(out var tick, out var e)
                || !s.ReadFloat(",").TryUnwrap(out var mx, out e)
                || !s.ReadFloat(",").TryUnwrap(out var my, out e)
                || !s.ReadFloat(",").TryUnwrap(out var mz, out e)
                || !s.ReadFloat(",").TryUnwrap(out var rx, out e)
                || !s.ReadFloat(",").TryUnwrap(out var ry, out e)
                || !s.ReadFloat(",").TryUnwrap(out var rz, out e)
                || !s.ReadString(",").TryUnwrap(out var type, out e)
                || !s.ReadInt(")").TryUnwrap(out var duration, out e))
                return ChartError.Parsing(line, lineNumber, RawEventType.Camera, e);

            var move = new Vector3(mx, my, mz);
            var rotate = new Vector3(rx, ry, rz);

            if (duration < 0)
                return ChartError.Property(line, lineNumber, RawEventType.Camera, duration.StartPos, duration.Length,
                    ChartError.Kind.DurationNegative);

            return new RawCamera
            {
                TimingGroup = CurrentTimingGroup,
                Timing = tick,
                Duration = duration,
                Move = move,
                Rotate = rotate,
                CameraType = type,
                Type = RawEventType.Camera,
                Line = lineNumber
            };
        }

        /// <summary>
        ///     Parse a scenecontrol aff line of the format
        ///     "scenecontrol([timing],[type],[arg1],[arg2]...);".
        /// </summary>
        /// <param name="line">The string to parse.</param>
        /// <param name="lineNumber">The line number of the event.</param>
        /// <returns>The parsed object.</returns>
        public Result<RawSceneControl, ChartError> ParseSceneControl(string line, int lineNumber)
        {
            var s = new StringParser(line);
            s.Skip("scenecontrol(".Length);
            if (!s.ReadInt(",").TryUnwrap(out var tick, out var e)
                || !s.ReadString(")").TryUnwrap(out var parameters, out e))
                return ChartError.Parsing(line, lineNumber, RawEventType.SceneControl, e);

            var parametersString = parameters.Value + ",";
            var p = new StringParser(parametersString);
            var currentString = "";
            var args = new List<object>();

            if (!p.ReadString(",").TryUnwrap(out var type, out e))
                return ChartError.Parsing(line, lineNumber, RawEventType.SceneControl, e);

            while (!p.HasEnded)
            {
                if (!p.ReadString(",").TryUnwrap(out var rawParamSpan, out e))
                    return ChartError.Parsing(line, lineNumber, RawEventType.SceneControl, e);

                var rawParam = rawParamSpan.Value;
                var isStart = rawParam[0] == '\"';
                var isEnd = rawParam.Length >= 2
                            && rawParam[rawParam.Length - 1] == '\"'
                            && rawParam[rawParam.Length - 2] != '\\';

                if (isStart)
                    currentString = rawParam;
                else if (currentString.Length > 0) currentString += "," + rawParam;

                if (currentString.Length == 0)
                {
                    if (!Evaluator.TryFloat(rawParam, out var val))
                        return ChartError.Parsing(
                            line,
                            lineNumber,
                            RawEventType.SceneControl,
                            new ParsingError(
                                rawParamSpan,
                                rawParamSpan.StartPos + parameters.StartPos,
                                rawParamSpan.Length,
                                ParsingError.Kind.InvalidConversionToFloat));

                    args.Add(val);
                }
                else if (isEnd)
                {
                    var param = currentString.Substring(1, currentString.Length - 2);
                    param = param.Replace("\\\"", "\"");
                    args.Add(param);
                    currentString = "";
                }
            }

            return new RawSceneControl
            {
                Timing = tick,
                Type = RawEventType.SceneControl,
                Arguments = args,
                SceneControlTypeName = type,
                TimingGroup = CurrentTimingGroup,
                Line = lineNumber
            };
        }

        /// <summary>
        ///     Parse a timing group aff line of the format "timinggroup([property],...){".
        /// </summary>
        /// <param name="line">The string to parse.</param>
        /// <param name="lineNumber">Line number of the string.</param>
        /// <param name="path">The path of the file this group was read from.</param>
        /// <returns>The parsed object.</returns>
        public Result<RawTimingGroup, ChartError> ParseTimingGroup(string line, int lineNumber, string path = "")
        {
            var s = new StringParser(line);
            s.Skip("timinggroup(".Length);
            if (!s.ReadString(")").TryUnwrap(out var properties, out var e))
                return ChartError.Parsing(line, lineNumber, RawEventType.TimingGroup, e);

            if (!RawTimingGroup.Parse(properties).TryUnwrap(out var tg, out var ce))
                return ChartError.Property(
                    line,
                    lineNumber,
                    RawEventType.TimingGroup,
                    ce.StartPosition.Or(0) + properties.StartPos,
                    ce.Length,
                    ce.ErrorKind);

            tg.File = path;
            return tg;
        }

        /// <summary>
        ///     Parse a include aff line of the format "include([reference]);".
        /// </summary>
        /// <param name="line">The string to parse.</param>
        /// <param name="lineNumber">Line number of the string.</param>
        /// <returns>The referenced file name.</returns>
        public Result<string, ChartError> ParseInclude(string line, int lineNumber)
        {
            var s = new StringParser(line);
            s.Skip("include(".Length);
            if (!s.ReadString(")").TryUnwrap(out var fileName, out var e))
                return ChartError.Parsing(line, lineNumber, RawEventType.Include, e);

            return fileName.Value.Trim();
        }

        /// <summary>
        ///     Parse a fragment aff line of the format "fragment([offset],[reference]);".
        /// </summary>
        /// <param name="line">The string to parse.</param>
        /// <param name="lineNumber">Line number of the string.</param>
        /// <returns>A tuple of the timing offset and referenced file name.</returns>
        public Result<RawFragment, ChartError> ParseFragment(string line, int lineNumber)
        {
            var s = new StringParser(line);
            s.Skip("fragment(".Length);
            if (!s.ReadInt(",").TryUnwrap(out var baseTiming, out var e)
                || !s.ReadString(")").TryUnwrap(out var fileName, out e))
                return ChartError.Parsing(line, lineNumber, RawEventType.Fragment, e);

            return new RawFragment
            {
                Timing = baseTiming,
                File = fileName.Value.Trim()
            };
        }

        private string SwitchFileName(string currentPath, string target)
        {
            var dir = Path.GetDirectoryName(currentPath);
            return Path.Combine(dir, target);
        }
    }
}