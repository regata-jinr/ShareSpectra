using System;
using System.Collections.Generic;
using System.Text;

namespace Extensions
{
    class SharedError
    {
        public string FileSpectra  { get; set; }
        public string ErrorMessage { get; set; }

        public override string ToString()
        {
            return $"{FileSpectra}\t{ErrorMessage}";
        }

    }
}
