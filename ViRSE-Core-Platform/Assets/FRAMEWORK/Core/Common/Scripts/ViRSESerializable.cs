namespace ViRSE.Core.Shared
{
    public abstract class ViRSESerializable
    {
        public byte[] Bytes { get => ConvertToBytes(); set => PopulateFromBytes(value); }

        public ViRSESerializable() { }

        public ViRSESerializable(byte[] bytes)
        {
            PopulateFromBytes(bytes);
        }

        protected abstract byte[] ConvertToBytes();

        protected abstract void PopulateFromBytes(byte[] bytes);
    }
}
