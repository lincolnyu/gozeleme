using Logging;
using System.Collections.Generic;

namespace CcDupList
{

    public interface IDupFileGroup<TDupFile> where TDupFile : IDupFile
    {
        long FileLength {get;}

        ICollection<TDupFile> Files {get;}
    }
}