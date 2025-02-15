using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace GoogleTranslateBFBBRewrite
{
    public static class TextParser
    {
        public static TEXT ReadTextAsset(string filePath)
        {
            try
            {
                using (BinaryReader reader = new BinaryReader(File.Open(filePath, FileMode.Open)))
                {
                    byte[] countBytes = reader.ReadBytes(4);
                    if (BitConverter.IsLittleEndian)
                        Array.Reverse(countBytes);

                    uint charCount = BitConverter.ToUInt32(countBytes, 0);

                    char[] textChars = new char[charCount];
                    for (int i = 0; i < charCount; i++)
                    {
                        textChars[i] = (char)reader.ReadByte();
                    }

                    byte nullTerminator = reader.ReadByte();
                    if (nullTerminator != 0x00)
                    {
                        throw new Exception("Expected null terminator after text, but found something else.");
                    }

                    long currentPosition = reader.BaseStream.Position;
                    int paddingSize = (int)((4 - (currentPosition % 4)) % 4);
                    byte[] padding = reader.ReadBytes(paddingSize);

                    return new TEXT
                    {
                        assetName = Path.GetFileNameWithoutExtension(filePath),
                        charCount = charCount,
                        text = textChars,
                        padding = padding
                    };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error reading text asset: " + ex.Message);
                return null;
            }
        }

        public static void WriteTextAsset(string filePath, TEXT text)
        {
            try
            {
                using (BinaryWriter writer = new BinaryWriter(File.Open(filePath, FileMode.Create)))
                {
                    byte[] countBytes = BitConverter.GetBytes(text.charCount);
                    if (BitConverter.IsLittleEndian)
                        Array.Reverse(countBytes);
                    writer.Write(countBytes);

                    writer.Write(Encoding.UTF8.GetBytes(text.text));
                    writer.Write((byte)0x00);

                    long currentPosition = writer.BaseStream.Position;
                    int paddingSize = (int)((4 - (currentPosition % 4)) % 4);
                    writer.Write(new byte[paddingSize]);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error writing text asset: " + ex.Message);
            }
        }
    }
}
