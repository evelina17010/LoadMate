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
        private const int AVERAGE_SPEED_KMPH = 60;
        private const int LOADING_TIME_HOURS = 2;
        public List<Truck> GetAvailableTrucksForCargo(Cargo cargo, List<Truck> allTrucks)
        {
            if (cargo == null) return new List<Truck>();

            var newOrder = cargo.Order?.FirstOrDefault();
            if (newOrder == null) return new List<Truck>();

            var startCity = newOrder.Route?.Address?.Street?.City?.Name;
            var endCity = newOrder.Route?.Address1?.Street?.City?.Name;
            var newPickupDate = newOrder.Scheduled_pickup ?? DateTime.MaxValue;

            return allTrucks
                .Where(t => t.TruckStatus_id == 1 && t.CanFit(cargo))
                .Where(t => CanTruckTakeOrder(t, newOrder, startCity, endCity, newPickupDate))
                .OrderByDescending(t => t.FreeWeight_kg)
                .ToList();
        }
        private bool CanTruckTakeOrder(Truck truck, Order newOrder, string startCity, string endCity, DateTime newPickupDate)
        {
            var activeOrders = truck.Order.Where(o => o.OrderStatus_id != 4).ToList();

            if (!activeOrders.Any()) return true;

            if (activeOrders.Any(o => o.Route?.Address?.Street?.City?.Name == startCity && o.Route?.Address1?.Street?.City?.Name == endCity))
                return true;

            var matchingOrders = activeOrders.Where(o => o.Route?.Address1?.Street?.City?.Name == endCity).ToList();

            foreach (var existingOrder in matchingOrders)
            {
                if (CanTakeOnTheWay(existingOrder, newOrder, newPickupDate))
                    return true;
            }

            return false;
        }
        private bool CanTakeOnTheWay(Order existingOrder, Order newOrder, DateTime newPickupDate)
        {
            var existingEndCity = existingOrder.Route?.Address1?.Street?.City?.Name;
            var newStartCity = newOrder.Route?.Address?.Street?.City?.Name;

            if (existingEndCity != newStartCity) return false;

            DateTime existingPickup = existingOrder.Scheduled_pickup ?? DateTime.MaxValue;

            if (newPickupDate < existingPickup) return false;

            var existingStartCity = existingOrder.Route?.Address?.Street?.City?.Name;
            var distance = GetDistanceBetweenCities(existingStartCity, existingEndCity);

            double travelHours = (double)(distance / AVERAGE_SPEED_KMPH);
            DateTime existingArrival = existingPickup.AddHours(travelHours + LOADING_TIME_HOURS);

            return newPickupDate >= existingArrival;
        }
        private decimal GetDistanceBetweenCities(string cityA, string cityB)
        {
            if (string.IsNullOrEmpty(cityA) || string.IsNullOrEmpty(cityB)) return 500;
            if (cityA == cityB) return 0;

            try
            {
                var db = Conn.loadMateEntities;
                var cityARecord = db.City.FirstOrDefault(c => c.Name == cityA);
                var cityBRecord = db.City.FirstOrDefault(c => c.Name == cityB);

                if (cityARecord == null || cityBRecord == null) return 500;

                var distance = db.Distance.FirstOrDefault(d =>
                    (d.City_A_id == cityARecord.City_id && d.City_B_id == cityBRecord.City_id) ||
                    (d.City_A_id == cityBRecord.City_id && d.City_B_id == cityARecord.City_id));

                return distance?.Distance_km ?? 500;
            }
            catch
            {
                return 500;
            }
        }
    }
}