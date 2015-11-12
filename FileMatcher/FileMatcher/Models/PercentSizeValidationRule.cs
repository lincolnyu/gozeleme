using System.Globalization;
using System.Windows.Controls;

namespace FileMatcherApp.Models
{
    public class PercentSizeValidationRule : ValidationRule
    {
        #region Methods

        #region ValidationRule members

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            if (value != null)
            {
                var sval = value.ToString();
                int num;
                var isNum = int.TryParse(sval, out num);
                if (isNum && num >= 0 && num <= 100) return new ValidationResult(true, null);
            }
            return new ValidationResult(false, "It's not a number between 0 and 100");
        }

        #endregion

        #endregion
    }
}
