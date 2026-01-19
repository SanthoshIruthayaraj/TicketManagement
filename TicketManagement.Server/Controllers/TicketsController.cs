using Microsoft.AspNetCore.Mvc;
using Syncfusion.EJ2.Base;
using TicketManagement.Server.Data;

namespace TicketManagement.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TicketsController : ControllerBase
    {
        private readonly TicketRepository _repo;
        private readonly DataOperations _dataOps = new DataOperations();

        public TicketsController(TicketRepository repo)
        {
            _repo = repo;
        }

        // READ (DataManager + UrlAdaptor expects POST)
        // POST api/tickets
        [HttpPost]
        public async Task<IActionResult> List([FromBody] DataManagerRequest dm)
        {
            IEnumerable<Tickets> data = await _repo.GetTicketsAsync();

            // Searching
            if (dm.Search != null && dm.Search.Count > 0)
            {
                data = _dataOps.PerformSearching(data, dm.Search);
            }

            // Filtering
            if (dm.Where != null && dm.Where.Count > 0)
            {
                data = _dataOps.PerformFiltering(data, dm.Where, dm.Where[0].Operator);
            }

            // Sorting
            if (dm.Sorted != null && dm.Sorted.Count > 0)
            {
                data = _dataOps.PerformSorting(data, dm.Sorted);
            }

            // Count BEFORE paging
            int count = data.Count();

            // Grouping
            if (dm.Group != null && dm.Group.Count > 0)
            {
                var grouped = _dataOps.PerformGrouping(data, dm.Group);

                // UrlAdaptor supports grouped result as-is; return { result, count }
                return Ok(new { result = grouped, count });
            }

            // Paging
            if (dm.Skip != 0)
                data = _dataOps.PerformSkip(data, dm.Skip);
            if (dm.Take != 0)
                data = _dataOps.PerformTake(data, dm.Take);

            // Final shape required by UrlAdaptor
            return Ok(dm.RequiresCounts ? new { result = data, count } : data);
        }

        [HttpGet("ping")]
        public IActionResult Ping() => Ok(new { ok = true, time = DateTime.UtcNow });

        // INSERT
        // POST api/tickets/insert
        [HttpPost("insert")]
        public async Task<IActionResult> Insert([FromBody] CRUDModel<Tickets> args)
        {
            if (args?.Value == null)
                return BadRequest("Invalid payload.");
            var created = await _repo.InsertAsync(args.Value);
            return Ok(created);
        }

        // UPDATE
        // POST api/tickets/update
        [HttpPost("update")]
        public async Task<IActionResult> Update([FromBody] CRUDModel<Tickets> args)
        {
            if (args?.Value == null)
                return BadRequest("Invalid payload.");
            if (args.Value.TicketId <= 0)
                return BadRequest("TicketId is required for update.");
            var updated = await _repo.UpdateAsync(args.Value);
            return Ok(updated);
        }

        // REMOVE
        // POST api/tickets/remove
        // UrlAdaptor sends { key: <id>, keyColumn: "TicketId", action: "remove" }
        [HttpPost("remove")]
        public async Task<IActionResult> Remove([FromBody] CRUDModel<Tickets> args)
        {
            if (args == null || args.Key == null)
                return BadRequest("Key is required.");
            if (!int.TryParse(args.Key.ToString(), out var id))
                return BadRequest("Invalid key format.");

            await _repo.DeleteAsync(id);
            return Ok(new { TicketId = id });
        }

        // BATCH
        // POST api/tickets/batch
        [HttpPost("batch")]
        public async Task<IActionResult> Batch([FromBody] CRUDModel<Tickets> args)
        {
            if (args == null)
                return BadRequest("Invalid payload.");

            if (args.Changed != null)
            {
                foreach (var t in args.Changed)
                    await _repo.UpdateAsync(t);
            }

            if (args.Added != null)
            {
                for (int i = 0; i < args.Added.Count; i++)
                    args.Added[i] = await _repo.InsertAsync(args.Added[i]);
            }

            if (args.Deleted != null)
            {
                foreach (var t in args.Deleted)
                    await _repo.DeleteAsync(t.TicketId);
            }

            return Ok(new { status = "ok" });
        }
    }
}
