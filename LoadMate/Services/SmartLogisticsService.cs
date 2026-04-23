using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LoadMate.DBConn;

namespace LoadMate.Services
{
    internal class SmartLogisticsService
    {
        public List<Truck> GetAvailableTrucksForCargo(Cargo cargo, List<Truck> allTrucks)
        {
            if (cargo == null) return new List<Truck>();
            var destinationCity = cargo.Order?.FirstOrDefault()?.Route?.Address1?.Street?.City?.Name;

            return allTrucks.Where(t =>
                t.TruckStatus_id == 1 && 
                t.CanFit(cargo) &&       
                (!t.Order.Any(o => o.OrderStatus_id != 4) || t.Order.Any(o => o.OrderStatus_id != 4 && o.Route?.Address1?.Street?.City?.Name == destinationCity)
                )
            )
            .OrderByDescending(t => t.FreeWeight_kg) 
            .ToList();
        }
    }
}
