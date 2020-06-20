using System;
using System.IO;

namespace DeDup.Core
{
    public class DdFile : IReadByteAffirmative
    {
        private FileStream _stream;

        public DdFile(FileInfo file)
        {
            File = file;
        }

        public FileInfo File {get;}
        public long FileLength => File.Length;

        public bool InitializeRead()
        {
            try
            {
                _stream = File.OpenRead();
                return true;
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
            catch (IOException)
            {
                return false;
            }
        }

        public byte ReadByteAffirmative()
        {
            System.Diagnostics.Debug.Assert(_stream != null);
            return (byte)_stream.ReadByte();
        }

        public void FinalizeRead()
        {
            if (_stream != null)
            {
                _stream.Close();
                _stream = null;
            }
        }
    }
}
