﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SixLabors.Fonts.Tables.General.Glyphs
{
    internal class SimpleGlyphLoader : GlyphLoader
    {
        private short[] xs;
        private short[] ys;
        private bool[] onCurves;
        private ushort[] endPoints;
        private Bounds bounds;

        public SimpleGlyphLoader(short[] xs, short[] ys, bool[] onCurves, ushort[] endPoints, Bounds bounds)
        {
            this.xs = xs;
            this.ys = ys;
            this.onCurves = onCurves;
            this.endPoints = endPoints;
            this.bounds = bounds;
        }

        public SimpleGlyphLoader(Bounds bounds)
        {
            this.ys = this.xs = new short[0];
            this.onCurves = new bool[0];
            this.endPoints = new ushort[0];
            this.bounds = bounds;
        }

        public override Glyph CreateGlyph(GlyphTable table)
        {
            // lets build some shapes ??? here from
            return new Glyph(this.xs, this.ys, this.onCurves, this.endPoints, this.bounds);
        }

        public static GlyphLoader LoadSimpleGlyph(BinaryReader reader, short count, Bounds bounds)
        {
            if (count == 0)
            {
                return new SimpleGlyphLoader(bounds);
            }

            // uint16         | endPtsOfContours[n] | Array of last points of each contour; n is the number of contours.
            // uint16         | instructionLength   | Total number of bytes for instructions.
            // uint8          | instructions[n]     | Array of instructions for each glyph; n is the number of instructions.
            // uint8          | flags[n]            | Array of flags for each coordinate in outline; n is the number of flags.
            // uint8 or int16 | xCoordinates[ ]     | First coordinates relative to(0, 0); others are relative to previous point.
            // uint8 or int16 | yCoordinates[]      | First coordinates relative to (0, 0); others are relative to previous point.
            var endPoints = reader.ReadUInt16Array(count);

            var instructionSize = reader.ReadUInt16();
            var instructions = reader.ReadUInt8Array(instructionSize);

            // TODO: should this take the max points rather?
            var pointCount = 0;
            if (count > 0)
            {
                pointCount = endPoints[count - 1] + 1;
            }

            var flags = reader.ReadUInt8Array<Flags>(pointCount);
            var xs = ReadCoordinates(reader, pointCount, flags, Flags.XByte, Flags.XSignOrSame);
            var ys = ReadCoordinates(reader, pointCount, flags, Flags.YByte, Flags.YSignOrSame);

            var onCurves = new bool[flags.Length];
            for (int i = flags.Length - 1; i >= 0; --i)
            {
                onCurves[i] = flags[i].HasFlag(Flags.OnCurve);
            }

            return new SimpleGlyphLoader(xs, ys, onCurves, endPoints, bounds);
        }

        private static short[] ReadCoordinates(BinaryReader reader, int pointCount, Flags[] flags, Flags isByte, Flags signOrSame)
        {
            var xs = new short[pointCount];
            int x = 0;
            for (int i = 0; i < pointCount; i++)
            {
                int dx;
                if (flags[i].HasFlag(isByte))
                {
                    var b = reader.ReadByte();
                    dx = flags[i].HasFlag(signOrSame) ? b : -b;
                }
                else
                {
                    if (signOrSame.HasFlag(flags[i]))
                    {
                        dx = 0;
                    }
                    else
                    {
                        dx = reader.ReadInt16();
                    }
                }

                x += dx;
                xs[i] = (short)x; // TODO: overflow?
            }

            return xs;
        }

        [Flags]
        private enum Flags : byte
        {
            ControlPoint = 0,
            OnCurve = 1,
            XByte = 2,
            YByte = 4,
            Repeat = 8,
            XSignOrSame = 16,
            YSignOrSame = 32
        }
    }
}
