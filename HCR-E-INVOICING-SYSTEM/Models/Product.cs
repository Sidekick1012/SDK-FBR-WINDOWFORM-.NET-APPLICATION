using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SDK_E_INVOICING_SYSTEM.Models
{
    public class Product
    {
        public int ProductId { get; set; }
        public string HsCode { get; set; }
        public string ProductDescription { get; set; }
        public string Rate { get; set; }
        public string UoM { get; set; }
    }
}