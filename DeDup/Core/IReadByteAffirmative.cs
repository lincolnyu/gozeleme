using System;
using System.IO;

namespace DeDup.Core
{
    public interface IReadByteAffirmative
    {
        byte ReadByteAffirmative();
        void FinalizeRead();
    }
}
