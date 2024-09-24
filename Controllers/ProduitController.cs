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

        // GET: api/products
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Produit>>> GetProducts()
        {
            return await _context.Produits.ToListAsync();
        }

        // GET: api/products/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<Produit>> GetProduit(int id)
        {
            var product = await _context.Produits.FindAsync(id);

            if (product == null)
            {
                return NotFound(new { message = "Produit non trouvé." });
            }

            return product;
        }

        // GET: produit in commande
        [HttpGet("produitInCommande")]
        public async Task<ActionResult<List<Produit>>> GetProduitInCommande([FromQuery] List<int> produitsId)
        {
            // Utilisation de .Contains pour vérifier si les produits sont dans la liste
            var produits = await _context.Produits
                                         .Where(produit => produitsId.Contains(produit.Id))
                                         .ToListAsync();

            // Vérifier si des produits existent
            if (produits == null || !produits.Any())
            {
                return NotFound(new { message = "Aucun produit trouvé dans la commande" });
            }

            return Ok(produits);
        }


        // POST:
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

            // Ajouter le produit à la base de données
            _context.Produits.Add(produit);
            await _context.SaveChangesAsync();

            // Retourner une réponse avec le code 201 Created et l'objet produit ajouté
            return CreatedAtAction(nameof(GetProduit), new { id = produit.Id }, produit);
        }

        // PUT: api/products/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> PutProduit(int id, Produit product)
        {
            if (id != product.Id)
            {
                return BadRequest(new { message = "L'ID du produit ne correspond pas à l'ID dans l'URL." });
            }

            _context.Entry(product).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ProduitExists(id))
                {
                    return NotFound(new { message = "Produit non trouvé." });
                }
                else
                {
                    throw;
                }
            }

            return Ok(new { message = "Produit mis à jour avec succès." });
        }

        // DELETE: api/products/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduit(int id)
        {
            var product = await _context.Produits.FindAsync(id);
            if (product == null)
            {
                return NotFound(new { message = "Produit non trouvé." });
            }

            _context.Produits.Remove(product);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Produit supprimé avec succès." });
        }

        private bool ProduitExists(int id)
        {
            return _context.Produits.Any(e => e.Id == id);
        }
    }
}
