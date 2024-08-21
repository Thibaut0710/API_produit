using API_produit.Models;
using Microsoft.EntityFrameworkCore;
namespace API_produit.Context
{
    public class ProduitContext : DbContext
    {
        public ProduitContext(DbContextOptions<ProduitContext> options) : base(options) { }

        public DbSet<Produit> Produits { get; set; }

    }
}
