using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Markup;

namespace UnrealAutomationToolGUI
{
    public class EnumBindingResourceExtension : MarkupExtension
    {   
        public Type EnumType { get; set; }

        public EnumBindingResourceExtension(Type enumType)
        {
            if (enumType == null)
            {
                throw new Exception("enumType was null while EnumBindingResource");
            }
            if (!enumType.IsEnum)
            {
                throw new Exception("enumType must be an enum for EnumBindingResource");
            }

            EnumType = enumType;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return Enum.GetValues(EnumType);
        }
    }
}
