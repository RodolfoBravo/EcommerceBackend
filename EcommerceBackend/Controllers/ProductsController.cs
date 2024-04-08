using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EcommerceBackend.Context;
using EcommerceBackend.Models;

namespace EcommerceBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ProductsController(AppDbContext context)
        {
            _context = context;
        }


        // POST: api/Products
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost("registe-product")]
        public async Task<ActionResult<Product>> PostProduct([FromForm] ProductWithImages productWithImages)
        {
            // Crear un nuevo producto
            var newProduct = new Product
            {
                name = productWithImages.name,
                description = productWithImages.description,
                price = productWithImages.price,
                categoryId = productWithImages.categoryId,
                isAvailable = productWithImages.isAvailable,
                stock = productWithImages.stock,
                userId = productWithImages.userId
            };

            // Agregar el producto a la base de datos y guardar los cambios
            _context.Products.Add(newProduct);
            await _context.SaveChangesAsync();

            // Guardar las imágenes en la tabla de imágenes con el ID del producto
            foreach (var imageFile in productWithImages.images)
            {
                var imageUrl = Path.Combine("Files", productWithImages.userId.ToString(), productWithImages.name.ToString(), imageFile.FileName);
                Directory.CreateDirectory(Path.GetDirectoryName(imageUrl));
                using (var stream = new FileStream(imageUrl, FileMode.Create))
                {
                    await imageFile.CopyToAsync(stream);
                }

                // Guardar la URL de la imagen en la tabla de imágenes
                var image = new Image
                {
                    url = imageUrl,
                    productId = newProduct.id
                };
                _context.Images.Add(image);
            }

            await _context.SaveChangesAsync();

            return Ok(newProduct);

        }

        public class ProductWithImages
        {
            public string name { get; set; }
            public string description { get; set; }
            public decimal price { get; set; }
            public int categoryId { get; set; }
            public bool isAvailable { get; set; }
            public int stock { get; set; }
            public int userId { get; set; }
            public List<IFormFile> images { get; set; }
        }

        [HttpGet("get-products-by-user/{userId}")]
        public async Task<ActionResult<IEnumerable<Product>>> GetProductsByUser(int userId)
        {
            var products = await _context.Products
                .Include(p => p.images)
                .Where(p => p.userId == userId)
                .ToListAsync();

            if (products == null || !products.Any())
            {
                return NotFound("No se encontraron productos para el usuario especificado.");
            }

            return Ok(products);
        }

        [HttpGet("get-products")]
        public async Task<ActionResult<IEnumerable<Product>>> GetAllProducts()
        {
            var products = await _context.Products
                .Include(p => p.images)
                .ToListAsync();

            if (products == null || !products.Any())
            {
                return NotFound("No se encontraron productos en el sistema.");
            }

            return Ok(products);
        }

        // DELETE: api/Products/5
        [HttpDelete("delete-product/{productId}")]
        public async Task<ActionResult> DeleteProduct(int productId)
        {
            var product = await _context.Products
                .Include(p => p.images)
                .FirstOrDefaultAsync(p => p.id == productId);

            if (product == null)
            {
                return NotFound("Producto no encontrado.");
            }

            // Eliminar las imágenes asociadas al producto y sus archivos en el sistema
            foreach (var image in product.images)
            {
                if (System.IO.File.Exists(image.url))
                {
                    System.IO.File.Delete(image.url);
                }
                _context.Images.Remove(image);
            }

            // Eliminar el producto de la base de datos
            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            // Eliminar los documentos asociados al producto en la carpeta 'files'
            var filesPath = Path.Combine("Files", product.userId.ToString(), product.name);
            if (Directory.Exists(filesPath))
            {
                Directory.Delete(filesPath, true);
            }

            return Ok("Producto y documentos eliminados correctamente.");
        }
    }
}
