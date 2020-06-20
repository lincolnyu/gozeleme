using Logging;
using System.IO;
using System.Collections.Generic;

namespace CcDupResolver
{
    class Parser
    {
        const string Bar = "------------------------------------------------------------------------------------------------------------------------------------------------------";
        private StreamReader _sr;
        private Logger _logger;
        public Parser(StreamReader sr, Logger logger)
        {
            _sr = sr;
            _logger = logger;
        }
        public IEnumerable<DupFileGroup> Run()
        {
            var dupFileGroup = new DupFileGroup();
            var error = false;
            for (var iLine = 0; ; iLine++)
            {
                if (_sr.EndOfStream)
                {
                    if (dupFileGroup.Files.Count > 1)
                    {
                        yield return dupFileGroup;
                    }
                    else
                    {
                        
                    }
                    break;
                }
                var l = _sr.ReadLine();
                if (l == Bar)
                {
                    if (error)
                    {
                        _logger.ErrorLine("");
                    }
                    else if (dupFileGroup.Files.Count <= 1)
                    {
                        _logger.ErrorLine("");
                    }
                    else
                    {
                        yield return dupFileGroup;
                    }
                    dupFileGroup = new DupFileGroup();
                    error = false;
                    continue;
                }
                var segs = l.Split('\t');
                if (segs.Length < 2)
                {
                    _logger.ErrorLine($"Line {iLine}: '{l}' Bad format.");
                    error = true;
                    continue;
                }
                var fn = segs[0];
                var dir = segs[1];
                var dupFile = new DupFile
                {
                    Dir = dir,
                    File = fn,
                };
                if (!File.Exists(dupFile.FullPath))
                {
                    _logger.ErrorLine($"Line {iLine}: File '{dupFile.FullPath}' not found.");
                    error = true;
                    continue;
                }
                dupFileGroup.Files.Add(dupFile);
            }
        }
    }
}

