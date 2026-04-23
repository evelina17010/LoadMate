using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace LoadMate.DBConn
{
    public partial class Truck
    {
        public decimal CurrentWeight_kg => Order?.Where(o => o.OrderStatus_id != 4).Sum(o => o.Cargo?.Weight_kg ?? 0) ?? 0;
        public decimal CurrentVolume_m3 => Order?.Where(o => o.OrderStatus_id != 4).Sum(o => o.Cargo?.Volume_m3 ?? 0) ?? 0;

        public decimal FreeWeight_kg => Capacity_kg - CurrentWeight_kg;
        public decimal FreeVolume_m3 => Capacity_m3 - CurrentVolume_m3;

        public bool CanFit(Cargo cargo)
        {
            if (cargo == null) return false;
            return FreeWeight_kg >= cargo.Weight_kg && FreeVolume_m3 >= cargo.Volume_m3;
        }
    }
}
    



