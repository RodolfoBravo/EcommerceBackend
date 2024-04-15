using EcommerceBackend.Context;
using EcommerceBackend.Models;
using EcommerceBackend.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stripe;

namespace EcommerceBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrdersController : ControllerBase
    {
        private readonly AppDbContext _context;
        public OrdersController(AppDbContext context)
        {
            _context = context;
        }
        [HttpPost("create-order/{token}/{tokeStripe}")]
        public ActionResult<Order> CreateOrder(string token, string tokenStripe)
        {
            if (string.IsNullOrEmpty(token))
            {
                return BadRequest("Token no válido");
            }
            int userId;
            var isValidToken = TokenFunctions.ValidateToken(token, out userId);
            if (isValidToken)
            {
                var cart = _context.Carts
                            .Include(c => c.items)
                            .FirstOrDefault(c => c.userId == userId);

                if (cart == null)
                {
                    return NotFound("No se encontró un carrito para el usuario especificado.");
                }

                var order = new Order
                {
                    UserId = userId.ToString(),
                    OrderDate = DateTime.Now,
                    Status = "Pending",
                    OrderDetails = cart.items.Select(item => new OrderDetail
                    {
                        ProductId = item.productId,
                        Quantity = item.quantity,
                        Price = _context.Products.FirstOrDefault(p => p.id == item.productId)?.price ?? 0
                    }).ToList()
                };

                order.Total = order.OrderDetails.Sum(detail => detail.Quantity * detail.Price);

                // Procesar el pago
                StripeConfiguration.ApiKey = "pk_live_51J39xYAwnRQpE9leMUi73RtsTupCFE1pMFAfUTCJZX37jFBcWxMIbDyxd7F1NFcICwKYkzXShbIB8KeEfbKYbkDm00mJYBy23B";

                var chargeOptions = new ChargeCreateOptions
                {
                    Amount = (long)(order.Total * 100), // Stripe requiere el monto en centavos
                    Currency = "usd",
                    Source = tokenStripe,
                    Description = "Descripción del cargo"
                };

                var chargeService = new ChargeService();
                Charge charge;

                try
                {
                    charge = chargeService.Create(chargeOptions);
                }
                catch (StripeException e)
                {
                    return BadRequest(e.Message);
                }

                if (!charge.Paid)
                {
                    return BadRequest("El pago no se pudo procesar");
                }

                _context.Orders.Add(order);
                _context.SaveChanges();

                // Crear un objeto anónimo para devolver solo los campos requeridos
                var result = new
                {
                    order.OrderId,
                    order.UserId,
                    order.Total,
                    order.Status,
                    order.OrderDate,
                    OrderDetails = order.OrderDetails.Select(i => new
                    {
                        i.OrderDetailId,
                        i.ProductId,
                        i.Quantity,
                        i.Price
                    }).ToList()
                };

                return Ok(result);
            }
            return Unauthorized();
        }

        [HttpGet("get-all-orders")]
        public ActionResult<IEnumerable<Order>> GetAllOrders()
        {
            var orders = _context.Orders
                        .Include(o => o.OrderDetails)
                        .ToList();

            return Ok(orders);
        }

        [HttpGet("get-user-orders/{token}")]
        public ActionResult<IEnumerable<Order>> GetUserOrders(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return BadRequest("Token no válido");
            }
            int userId;
            var isValidToken = TokenFunctions.ValidateToken(token, out userId);
            if (isValidToken)
            {
                var orders = _context.Orders
                            .Include(o => o.OrderDetails)
                            .Where(o => o.UserId == userId.ToString())
                            .ToList();

                if (orders == null)
                {
                    return NotFound("No se encontraron órdenes para el usuario especificado.");
                }

                return Ok(orders);
            }
            return Unauthorized();
        }
    }
}
