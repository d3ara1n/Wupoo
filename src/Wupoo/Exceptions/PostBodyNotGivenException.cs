using System;
using System.Collections.Generic;
using System.Text;

namespace Wupoo.Exceptions
{
    public class PostBodyNotGivenException: WapooException
    {
        public PostBodyNotGivenException() : base("Method Post requires a HTTP Post body.") { }
    }
}
