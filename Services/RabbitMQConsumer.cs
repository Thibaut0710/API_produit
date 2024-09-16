namespace ConsumerAPI
{
    public class RabbitMQConsumer
    {
        private readonly IRabbitMQService _rabbitMQService;

        public RabbitMQConsumer(IRabbitMQService rabbitMQService)
        {
            _rabbitMQService = rabbitMQService;
            _rabbitMQService.CreateConsumerProduitInCommmande();
        }
    }
}
