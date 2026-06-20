using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sidekick_E_Invoicing.Models
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