using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EZAccess.Data
{
    public class BeforeCRUDEventArgs<TModel> : EventArgs
    {
        public TModel Model { get; set; }

        public BeforeCRUDEventArgs(TModel model)
        {
            Model = model;
        }

        public bool Cancel { get; set; }
    }
}
