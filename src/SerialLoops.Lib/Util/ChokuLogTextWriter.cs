using System;
using System.Globalization;
using System.IO;
using System.Text;
using HaruhiChokuretsuLib.Util;

namespace SerialLoops.Lib.Util;

public class ChokuLogTextWriter(ILogger log) : TextWriter
{
    public override Encoding Encoding => Encoding.UTF8;

    public override void Write(char[] buffer) => log.Log(new(buffer));
    public override void Write(StringBuilder value) => log.Log(value?.ToString());
    public override void Write(string format, params object[] arg) => log.Log(string.Format(format, arg));
    public override void Write(string format, object arg0, object arg1, object arg2) => log.Log(string.Format(format, arg0, arg1, arg2));
    public override void Write(string format, object arg0, object arg1) => log.Log(string.Format(format, arg0, arg1));
    public override void Write(string format, object arg0) => log.Log(string.Format(format, arg0));
    public override void Write(string value) => log.Log(value);
    public override void Write(float value) => log.Log(value.ToString(CultureInfo.InvariantCulture));
    public override void Write(ReadOnlySpan<char> buffer) => log.Log(new(buffer));
    public override void Write(long value) => log.Log(value.ToString());
    public override void Write(ulong value) => log.Log(value.ToString());
    public override void Write(uint value) => log.Log(value.ToString());
    public override void Write(object value) => log.Log(value?.ToString());
    public override void Write(double value) => log.Log(value.ToString(CultureInfo.InvariantCulture));
    public override void Write(decimal value) => log.Log(value.ToString(CultureInfo.InvariantCulture));
    public override void Write(bool value) => log.Log(value.ToString());
    public override void Write(char value) => log.Log(value.ToString());
    public override void Write(char[] buffer, int index, int count) => log.Log(new(buffer, index, count));
    public override void Write(int value) => log.Log(value.ToString());
    public override void WriteLine(long value) => log.Log("\n");
}
