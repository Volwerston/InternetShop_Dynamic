using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InternetShop_Dynamic.Models.Auxiliary
{
    public interface IDtoFactory<TSource, TRes>
    {
        TRes Create(TSource source);
    }
}
