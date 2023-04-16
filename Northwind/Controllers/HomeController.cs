using NorthwindModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace Northwind.Controllers
{
    public class HomeController : Controller
    {
        private const string url = "https://services.odata.org/V3/Northwind/Northwind.svc";

        [Authorize]
        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [Authorize]
        public ActionResult OrderList()
        {
            try
            {
                int filterRecord = 0;
                var draw = Request.Form["draw"];
                var sortColumn = Request.Form["columns[" + Request.Form["order[0][column]"].FirstOrDefault() + "][name]"];
                var sortColumnDirection = Request.Form["order[0][dir]"];
                var searchValue = Request.Form["search[value]"];
                int pageSize = Convert.ToInt32(string.IsNullOrEmpty(Request.Form["length"]) ? "0" : Request.Form["length"]);
                int skip = Convert.ToInt32(string.IsNullOrEmpty(Request.Form["start"]) ? "0" : Request.Form["start"]);
                DateTime startDate = DateTime.ParseExact(Request["startDate"], "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
                DateTime endDate = DateTime.ParseExact(Request["endDate"], "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
                
                NorthwindEntities northwind = new NorthwindEntities(new Uri(url));
                var rawData = northwind.Orders;
                rawData.Expand("Customer");
                rawData.Expand("Order_Details");
                var data = rawData.Where(w => w.OrderDate >= startDate && w.OrderDate <= endDate).Select(s => new
                {
                    s.OrderID,
                    s.OrderDate,
                    s.Customer.CompanyName,
                    s.Customer.Phone,
                    s.ShipCity,
                    TotalPrice = s.Order_Details.Select(od => (od.UnitPrice * od.Quantity)).Sum()
                }).ToList();
                
                int totalRecord = data.Count();
                if (!string.IsNullOrEmpty(searchValue))
                    data = data.Where(w => w.CompanyName.ToLower().Contains(searchValue.ToLower()) || w.ShipCity.ToLower().Contains(searchValue.ToLower())).ToList();
                

                filterRecord = data.Count();
                data = data.Skip(skip).Take(pageSize).ToList();
                return Json(new
                {
                    draw,
                    recordsTotal = totalRecord,
                    recordsFiltered = filterRecord,
                    data
                });
            }
            catch (Exception excp)
            {
                return Json(new
                {
                    status = "Error",
                    message = excp.Message
                });
            }
        }

        [HttpPost]
        [Authorize]
        public ActionResult ShipCity(string start, string end)
        {
            try
            {
                NorthwindEntities northwind = new NorthwindEntities(new Uri(url));
                DateTime startDate = DateTime.ParseExact(start, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
                DateTime endDate = DateTime.ParseExact(end, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
                var data = northwind.Orders.Where(w => w.OrderDate >= startDate && w.OrderDate <= endDate).Select(s => new
                {
                    s.ShipCity,
                    s.OrderID
                }).GroupBy(g => g.ShipCity).Select(s => new
                {
                    s.First().ShipCity,
                    Qty = s.Count()
                });
                return Json(new
                {
                    status = "OK",
                    data
                });
            }
            catch (Exception excp)
            {
                return Json(new
                {
                    status = "Error",
                    message = excp.Message
                });
            }
        }

        public class ProductTransaction
        {
            public int? CategoryID { get; set; }
            public string CategoryName { get; set; }
            public int Quantity { get; set; }
            public decimal Price { get; set; }
        }

        [HttpPost]
        [Authorize]
        public ActionResult Sales(string start, string end)
        {
            try
            {
                NorthwindEntities northwind = new NorthwindEntities(new Uri(url));
                DateTime startDate = DateTime.ParseExact(start, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
                DateTime endDate = DateTime.ParseExact(end, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
                List<ProductTransaction> dataOrder = new List<ProductTransaction>();
                var raw = northwind.Orders;
                
                var data = dataOrder.AsParallel().GroupBy(g => g.CategoryID).Select(s => new
                {
                    CategoryName = string.IsNullOrEmpty(s.First().CategoryName) ? "Other Category" : s.First().CategoryName,
                    Quantity = s.Sum(u => u.Quantity),
                    TotalPrice = s.Sum(u => u.Price * u.Quantity)
                }).ToList();
                return Json(new
                {
                    status = "OK",
                    data
                });
            }
            catch (Exception excp)
            {
                return Json(new
                {
                    status = "Error",
                    message = excp.Message
                });
            }
        }
    }
}