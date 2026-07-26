internal interface INativePatchWriter
{
    byte[] Read(int rva, int length);

    void Write(int rva, byte[] bytes);
}
