namespace GoogleTranslateBFBBRewrite
{
    public class TEXT
    {
        public string assetName { get; set; }
        public string assetPath { get; set; }

        public uint charCount { get; set; }
        public char[] text { get; set; }
        public byte[] padding { get; set; }

        public TEXT()
        {
            assetName = "";
            assetPath = "";
            charCount = 0;
            text = new char[] { };
            padding = new byte[0];
        }
    }
}
