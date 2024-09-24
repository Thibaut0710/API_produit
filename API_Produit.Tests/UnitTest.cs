using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Xunit;
using API_produit.Controllers;
using API_produit.Models;
using API_produit.Context;
using Microsoft.AspNetCore.Mvc;

namespace API_Produit.Tests
{
    public class ProduitControllerTests
    {
        private DbContextOptions<ProduitContext> GetInMemoryDbContextOptions()
        {
            return new DbContextOptionsBuilder<ProduitContext>()
                .UseInMemoryDatabase(databaseName: "ProduitTestDb")
                .Options;
        }

        private ProduitContext GetInMemoryDbContext()
        {
            var options = GetInMemoryDbContextOptions();
            var context = new ProduitContext(options);

            if (!context.Produits.Any())
            {
                context.Produits.AddRange(
                    new Produit { Id = 1, Name = "Chocolat", Description = "Barre de chocolat cacao 70%.", Price = 3.5M, Stock = 100 },
                    new Produit { Id = 2, Name = "Café", Description = "Café torréfié", Price = 12.5M, Stock = 1600 }
                );
                context.SaveChanges();
            }

            return context;
        }

        [Fact]
        public async Task GetProducts_ShouldReturnAllProducts()
        {
            var context = GetInMemoryDbContext();
            var controller = new ProduitController(context);

            var result = await controller.GetProducts();

            var actionResult = Assert.IsType<ActionResult<IEnumerable<Produit>>>(result);
            var returnValue = Assert.IsType<List<Produit>>(actionResult.Value);
            Assert.Equal(2, returnValue.Count);
        }

        [Fact]
        public async Task GetProduit_ShouldReturnProductById()
        {
            var context = GetInMemoryDbContext();
            var controller = new ProduitController(context);

            var result = await controller.GetProduit(1);

            var actionResult = Assert.IsType<ActionResult<Produit>>(result);
            var returnValue = Assert.IsType<Produit>(actionResult.Value);
            Assert.Equal(1, returnValue.Id);
        }

        [Fact]
        public async Task GetProduit_ShouldReturnNotFound_ForInvalidId()
        {
            var context = GetInMemoryDbContext();
            var controller = new ProduitController(context);

            var result = await controller.GetProduit(999);

            var actionResult = Assert.IsType<ActionResult<Produit>>(result);
            Assert.IsType<NotFoundObjectResult>(actionResult.Result);
        }

        [Fact]
        public async Task CreateProduit_ShouldAddProduct()
        {
            var context = GetInMemoryDbContext();
            var controller = new ProduitController(context);
            var newProduit = new Produit { Id = 432131, Name = "FEZA89\'\"\'à)&=$\'", Description = "opdjkp&àé&&(ç_çàù*%;nfoà.PZKJAPOEJ\"\'(é(è-'", Price = 3132145.125M, Stock = 1289601803 };

            var result = await controller.CreateProduit(newProduit);

            var actionResult = Assert.IsType<CreatedAtActionResult>(result);
            var returnValue = Assert.IsType<Produit>(actionResult.Value);
            Assert.Equal(432131, returnValue.Id);
        }

        [Fact]
        public async Task DeleteProduit_ShouldDeleteProductById()
        {
            var context = GetInMemoryDbContext();
            var controller = new ProduitController(context);

            var result = await controller.DeleteProduit(1);

            var actionResult = Assert.IsType<OkObjectResult>(result);
            var products = context.Produits.ToList();
            Assert.DoesNotContain(products, p => p.Id == 1);
        }

        [Fact]
        public async Task DeleteProduit_ShouldReturnNotFound_ForInvalidId()
        {
            var context = GetInMemoryDbContext();
            var controller = new ProduitController(context);

            var result = await controller.DeleteProduit(999);

            var actionResult = Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task GetProduitInCommande_ShouldReturnProductsInCommande()
        {
            var context = GetInMemoryDbContext();
            var controller = new ProduitController(context);
            var produitsIds = new List<int> { 1, 2 };

            var result = await controller.GetProduitInCommande(produitsIds);

            var actionResult = Assert.IsType<ActionResult<List<Produit>>>(result);
            var returnValue = Assert.IsType<OkObjectResult>(actionResult.Result);
            var produits = Assert.IsType<List<Produit>>(returnValue.Value);
            Assert.Equal(2, produits.Count);
        }

        [Fact]
        public async Task GetProduitInCommande_ShouldReturnNotFound_ForNonExistentProducts()
        {
            var context = GetInMemoryDbContext();
            var controller = new ProduitController(context);
            var produitsIds = new List<int> { 999 };

            var result = await controller.GetProduitInCommande(produitsIds);

            var actionResult = Assert.IsType<ActionResult<List<Produit>>>(result);
            Assert.IsType<NotFoundObjectResult>(actionResult.Result);
        }
    }
}
