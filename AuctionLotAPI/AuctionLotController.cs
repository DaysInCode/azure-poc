using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using System.Text.Json;

namespace AuctionLotAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuctionLotController : ControllerBase
    {
        private readonly CosmosClient _cosmosClient;
        private readonly Container _auctionsContainer;
        private readonly Container _lotsContainer;

        public AuctionLotController(CosmosClient cosmosClient)
        {
            _cosmosClient = cosmosClient;
            _auctionsContainer = _cosmosClient.GetContainer("AuctionsDB", "Auctions");
            _lotsContainer = _cosmosClient.GetContainer("AuctionsDB", "Lots");
        }

        [HttpPost("auction")]
        public async Task<IActionResult> CreateAuction([FromBody] JsonElement auction)
        {
            try
            {
                await _auctionsContainer.CreateItemAsync(auction, new PartitionKey(auction.GetProperty("id").GetString()));
                return Ok("Auction created successfully.");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("lot")]
        public async Task<IActionResult> CreateLot([FromBody] JsonElement lot)
        {
            try
            {
                await _lotsContainer.CreateItemAsync(lot, new PartitionKey(lot.GetProperty("auctionId").GetString()));
                return Ok("Lot created successfully.");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("auction/{id}")]
        public async Task<IActionResult> GetAuction(string id)
        {
            try
            {
                var response = await _auctionsContainer.ReadItemAsync<JsonElement>(id, new PartitionKey(id));
                return Ok(response.Resource);
            }
            catch (Exception ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpGet("lot/{id}")]
        public async Task<IActionResult> GetLot(string id)
        {
            try
            {
                var query = new QueryDefinition("SELECT * FROM c WHERE c.lotId = @lotId").WithParameter("@lotId", id);
                var iterator = _lotsContainer.GetItemQueryIterator<JsonElement>(query);

                if (iterator.HasMoreResults)
                {
                    var response = await iterator.ReadNextAsync();
                    return Ok(response.Resource.FirstOrDefault());
                }

                return NotFound("Lot not found.");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}