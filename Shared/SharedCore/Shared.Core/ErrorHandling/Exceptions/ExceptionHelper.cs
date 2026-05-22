using System.Text;
using System.Windows;

namespace Shared.Core.ErrorHandling.Exceptions
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

        public static List<string> GetErrorStringArray(Exception e)
        {
            var output = new List<string>();

            var innerE = e.InnerException;
            while (innerE != null)
            {
                output.Add(innerE.Message);
                innerE = innerE.InnerException;
            }

            return output;
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
