using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace DWMS_OCR.App_Code.Helper
{
    class Validation
    {

        public static bool IsDate(string sDate)
        {
            if (String.IsNullOrEmpty(sDate))
                return false;

            DateTime dt;
            bool isDate = true;

            try
            {
                dt = DateTime.Parse(sDate);
            }
            catch
            {
                isDate = false;
            }

            return isDate;
        }

        public static bool IsEmail(string email)
        {
            Regex re = new Regex(@"\w+([-+.']\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*");
            return re.IsMatch(email);
        }
    }
}
