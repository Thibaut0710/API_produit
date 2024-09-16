using API_produit.Models;
using System.Text.Json;

namespace API_Produit.Service
{
    public class ProduitService
    {
        private readonly IRabbitMQService _rabbitMQService;


        public ProduitService(IRabbitMQService rabbitMQService)
        {
            _rabbitMQService = rabbitMQService;
        }

    }

}
