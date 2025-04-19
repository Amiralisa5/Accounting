using System.Collections.Generic;

namespace BigBang.App.Cloud.ERP.Accounting.Common.Collections
{
    public class ByteCollection : List<int>
    {
        public static bool TryParse(string parameters, out ByteCollection byteCollection)
        {
            byteCollection = [];

            if (parameters is null)
                return true;

            var values = parameters.Split(',');

            foreach (var value in values)
            {
                if (int.TryParse(value, out var enumValue))
                    byteCollection.Add(enumValue);
            }

            return true;
        }
    }
}