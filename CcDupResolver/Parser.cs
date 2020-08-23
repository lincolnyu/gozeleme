using Logging;
using System.IO;
using System.Collections.Generic;

namespace CcDupResolver
{
    class Parser
    {
        public const string Bar = CcDupList.WriterConstants.CcBar; 
        private StreamReader _sr;
        private Logger _logger;
        public Parser(StreamReader sr, Logger logger)
        {
            _sr = sr;
            _logger = logger;
        }
        public IEnumerable<DupFileGroup> Run()
        {
            var iLine = 0;
            var g = new DupFileGroup();
            for (; ; iLine++)
            {
                if (_sr.EndOfStream)
                {
                    g.Validate(_logger);
                    if (g.IsValidDupGroup)
                    {
                        yield return g;
                    }
                    break;
                }
                var l = _sr.ReadLine();
                if (l == Bar)
                {
                    g.Validate(_logger);
                    if (g.IsValidDupGroup)
                    {
                        yield return g;
                    }
                    else if (iLine > 0)
                    {
                        _logger.ErrorLine($"Line {iLine}: Concluding a non-duplicate file group.");
                    }
                    g = new DupFileGroup();
                    continue;
                }
                var segs = l.Split('\t');
                if (segs.Length < 2)
                {
                    _logger.ErrorLine($"Line {iLine}: '{l}' Bad format.");
                    continue;
                }
                var fn = segs[0];
                var dir = segs[1];
                g.AddFile(Path.Combine(dir, fn), _logger);
            }
        }
    }
}
