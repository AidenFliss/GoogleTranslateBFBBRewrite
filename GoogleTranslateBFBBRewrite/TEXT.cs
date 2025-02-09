namespace GoogleTranslateBFBBRewrite
{
    public class TEXT
    {
        public uint charCount { get; set; }
        public char[] text { get; set; }
        public byte[] padding { get; set; }

        public TEXT()
        {
            charCount = 0;
            text = new char[] { };
            padding = new byte[0];
        }
    }
}
