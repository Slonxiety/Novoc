using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Main.Machine
{
    public static class PositionEncoding
    {
        public static IPositionEncoder Encoder { get; } = new StandardEncoder();
    }
    public interface IPositionEncoder
    {
        int ByteSize { get; }
        int GetChapter(byte[] data);
        int GetParagraph(byte[] data);
        int GetLine(byte[] data);
        void SetChapter(ref byte[] data, int value);
        void SetParagraph(ref byte[] data, int value);
        void SetLine(ref byte[] data, int value);

        string GetDataString (byte[] data);
        void SetDataString (ref byte[] data, string value);

    }
    public class StandardEncoder : IPositionEncoder
    {
        int Csize = 2, Psize = 3, Lsize = 3; //maximum = 256 ^ size, i.e. 65536, 16777216
        public int ByteSize { get { return Csize + Psize + Lsize; } }
     
        private int ByteToInt(byte[] data, int start, int size)
        {
            int ret = 0;
            for (int i = start; i < start + size; i++)
            {
                ret *= 256;
                ret += data[i];
            }
            return ret;
        }
        private void SetByte(ref byte[] data, int start, int size, int value)
        {
            for (int i = start + size - 1; i >= start ; i--)
            {
                data[i] = (byte)(value % 256);
                value /= 256;
            }
        }
        
        
        public int GetChapter(byte[] data)
        {
            return ByteToInt(data, 0, Csize);
        }
        public int GetParagraph(byte[] data)
        {
            return ByteToInt(data, Csize, Psize);
        }
        public int GetLine(byte[] data)
        {
            return ByteToInt(data, Csize + Psize, Lsize);
        }
        public void SetChapter(ref byte[] data, int value)
        {
            SetByte(ref data, 0, Csize, value);
        }
        public void SetParagraph(ref byte[] data, int value)
        {
            SetByte(ref data, Csize, Psize, value);
        }
        public void SetLine(ref byte[] data, int value)
        {
            SetByte(ref data, Csize + Psize, Lsize, value);
        }

        public string GetDataString(byte[] data)
        {
            ulong ret = 0;
            for (int i = 0; i < ByteSize; i++)
                ret = (ret << 8) + data[i];
            
            return ret.ToString("x");
        }
        public void SetDataString(ref byte[] data, string value)
        {
            ulong val = ulong.Parse(value, System.Globalization.NumberStyles.HexNumber);
            for (int i = ByteSize - 1; i >= 0; i--)
            {
                data[i] = (byte)(val % 256);
                val /= 256;
            }
        }
    }
}
