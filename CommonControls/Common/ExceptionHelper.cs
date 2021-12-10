using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;

namespace CommonControls.Common
{
    public class ExceptionHelper
    {
        public static void ShowErrorBox(Exception e)
        {
            var errorStr = GetErrorString(e);
            MessageBox.Show(errorStr, "Error");
        }

        public static string GetErrorString(Exception e, string seperator = "\n")
        {
            var ss = new StringBuilder();
            ss.Append(e.Message + seperator);

            var innerE = e.InnerException;
            while (innerE != null)
            {
                ss.Append(innerE.Message + seperator);
                innerE = innerE.InnerException;
            }

            return ss.ToString();
        }


        public static Exception GetInnerMostException(Exception e)
        {
            var innerE = e.InnerException;
            if (innerE == null)
                return e;

            while (innerE.InnerException != null)
            {
                innerE = innerE.InnerException;
            }

            return innerE;
        }
    }
}
