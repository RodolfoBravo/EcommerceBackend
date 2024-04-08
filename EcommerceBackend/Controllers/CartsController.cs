using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EcommerceBackend.Context;
using EcommerceBackend.Models;
using EcommerceBackend.Utils;

namespace EcommerceBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CartsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CartsController(AppDbContext context)
        {
            _context = context;
        }
        [HttpPost("cart/add")]
        public ActionResult AddToCart(string token, [FromBody] CartItem item)
        {
            if (string.IsNullOrEmpty(token))
            {
                return BadRequest("Token no válido");
            }
            int userId;
            var isValidToken = TokenFunctions.ValidateToken(token, out userId);
            if (isValidToken)
            {
                // Verificar si ya existe un carrito para el usuario
                var existingCart = _context.Carts.Include(c => c.items).FirstOrDefault(c => c.userId == userId);
                if (existingCart != null)
                {
                    return BadRequest("El usuario ya tiene un carrito.");
                }

                // Crear un nuevo carrito
                var cart = new Cart
                {
                    userId = userId,
                    items = new List<CartItem> { item }
                };

                _context.Carts.Add(cart);
                _context.SaveChanges(); // Guardar cambios en la base de datos

                return Ok(cart);
            }
            return Unauthorized();
        }

        [HttpPost("cart/update")]
        public ActionResult UpdateCartItem(string token, [FromBody] CartItem item)
        {
            if (string.IsNullOrEmpty(token))
            {
                return BadRequest("Token no válido");
            }
            int userId;
            var isValidToken = TokenFunctions.ValidateToken(token, out userId);
            if (isValidToken)
            {
                var cart = _context.Carts.Include(c => c.items).FirstOrDefault(c => c.userId == userId);

                if (cart == null)
                {
                    return NotFound("No se encontró un carrito para el usuario especificado.");
                }

                var existingItem = cart.items.FirstOrDefault(i => i.productId == item.productId);
                if (existingItem != null)
                {
                    existingItem.quantity = item.quantity;
                }
                else
                {
                    cart.items.Add(item);
                }

                _context.SaveChanges(); // Guardar cambios en la base de datos

                return Ok(cart);
            }
            return Unauthorized();
        }

        [HttpPost("cart/remove")]
        public ActionResult RemoveFromCart(string token, [FromBody] int itemId)
        {
            if (string.IsNullOrEmpty(token))
            {
                return BadRequest("Token no válido");
            }
            int userId;
            var isValidToken = TokenFunctions.ValidateToken(token, out userId);
            if (isValidToken)
            {
                // Buscar el carrito del usuario que contiene el artículo a eliminar
                var cart = _context.Carts.Include(c => c.items).FirstOrDefault(c => c.userId == userId && c.items.Any(i => i.id == itemId));

                if (cart == null)
                {
                    return NotFound("No se encontró un carrito para el usuario especificado que contenga el artículo especificado.");
                }

                var itemToRemove = cart.items.FirstOrDefault(i => i.id == itemId);
                if (itemToRemove != null)
                {
                    cart.items.Remove(itemToRemove);
                }

                _context.SaveChanges(); // Guardar cambios en la base de datos

                return Ok(cart);
            }
            return Unauthorized();
        }

        [HttpGet("get-cart/{token}")]
        public ActionResult<Cart> GetCart(string token)

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

                // Crear un objeto anónimo para devolver solo los campos requeridos
                var result = new
                {
                    cart.id,
                    cart.userId,
                    items = cart.items.Select(i => new
                    {
                        i.id,
                        i.quantity,
                        product = _context.Products.Where(p => p.id == i.productId)
                                                   .Select(p => new
                                                   {
                                                       p.id,
                                                       p.name,
                                                       p.description,
                                                       p.price
                                                   }).FirstOrDefault()
                    }).ToList()
                };

                return Ok(result);
            }
            return Unauthorized();
        }

        [HttpPost("cart/clear")]
        public ActionResult ClearCart(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return BadRequest("Token no válido");
            }
            int userId;
            var isValidToken = TokenFunctions.ValidateToken(token, out userId);
            if (isValidToken)
            {
                var cart = _context.Carts.Include(c => c.items).FirstOrDefault(c => c.userId == userId);

                if (cart == null)
                {
                    return NotFound("No se encontró un carrito para el usuario especificado.");
                }

                _context.Carts.Remove(cart);
                _context.SaveChanges(); // Guardar cambios en la base de datos

                return Ok("El carrito ha sido borrado con éxito.");
            }
            return Unauthorized();
        }
    }
}
