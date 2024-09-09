using API_produit.Context;
using API_produit.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API_produit.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProduitController : ControllerBase
    {
        private readonly ProduitContext _context;

        public ProduitController(ProduitContext context)
        {
            _context = context;
        }

        // GET: api/produits
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Produit>>> GetProducts()
        {
            return await _context.Produits.AsNoTracking().ToListAsync();
        }

        // GET: api/produits/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<Produit>> GetProduct(int id)
        {
            var product = await _context.Produits.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                return NotFound(new { message = "Produit non trouvé." });
            }

            return Ok(product);
        }

        // GET: api/produits/produitInCommande
        [HttpGet("produitInCommande")]
        public async Task<ActionResult<List<Produit>>> GetProduitInCommande([FromQuery] List<int> produitsId)
        {
            var produits = await _context.Produits
                                         .Where(produit => produitsId.Contains(produit.Id))
                                         .AsNoTracking()
                                         .ToListAsync();

            if (!produits.Any())
            {
                return NotFound(new { message = "Aucun produit trouvé dans la commande." });
            }

            return Ok(produits);
        }

        // POST: api/produits
        [HttpPost]
        public async Task<IActionResult> CreateProduit([FromBody] Produit produit)
        {
            if (produit == null)
            {
                return BadRequest(new { message = "Le produit ne peut pas être null." });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _context.Produits.Add(produit);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Une erreur est survenue lors de la création du produit.", detail = ex.Message });
            }

            return CreatedAtAction(nameof(GetProduct), new { id = produit.Id }, produit);
        }

        // PUT: api/produits/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> PutProduct(int id, [FromBody] Produit product)
        {
            if (id != product.Id)
            {
                return BadRequest(new { message = "L'ID du produit ne correspond pas à l'ID dans l'URL." });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _context.Entry(product).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ProductExists(id))
                {
                    return NotFound(new { message = "Produit non trouvé." });
                }
                else
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Une erreur est survenue lors de la mise à jour du produit.", detail = ex.Message });
            }

            return Ok(new { message = "Produit mis à jour avec succès." });
        }

        // DELETE: api/produits/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _context.Produits.FindAsync(id);
            if (product == null)
            {
                return NotFound(new { message = "Produit non trouvé." });
            }

            _context.Produits.Remove(product);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Une erreur est survenue lors de la suppression du produit.", detail = ex.Message });
            }

            return Ok(new { message = "Produit supprimé avec succès." });
        }

        private bool ProductExists(int id)
        {
            return _context.Produits.Any(e => e.Id == id);
        }
    }
}
