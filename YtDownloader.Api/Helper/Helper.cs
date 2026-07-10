using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace YtDownloader.Api.Helper
{
    public static class Helper
    {
        public static int? TryParseProgress(string line)
        {
          const string marker = "[download]";
          var markerIndex = line.IndexOf(marker, StringComparison.OrdinalIgnoreCase);

          if (markerIndex < 0 )
            return null;

          var percentIndex = line.IndexOf('%', markerIndex);

          if (percentIndex < 0)
            return null;

          var start = percentIndex - 1;
          while (start >= 0 && (char.IsDigit(line[start]) || line[start] == '.'))
          {
            start--;
          }

          var valueText = line[(start + 1)..percentIndex];

          if (double.TryParse(valueText, out var value))
          {
            return (Math.Clamp((int)Math.Round(value), 0, 100));
          }

          return null;
        }
    }
}