using SorterLibrary.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SorterLibrary
{
    public static class ConverterManager
    {
        public static List<LineOfDataModel> Convert(string[] source)
        {
            List<LineOfDataModel> list = new List<LineOfDataModel>();

            foreach (var item in source)
            {
                var temp = item.Split(". ");
                LineOfDataModel model = new LineOfDataModel()
                {
                    Value = item,
                    Number = int.Parse(temp[0]),
                    Text = temp[1]
                };
                list.Add(model);
            }

            return list;
        }
    }
}
