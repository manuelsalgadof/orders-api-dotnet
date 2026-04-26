using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using OrdersApi.Data;
using OrdersApi.Entities;
using OrdersApi.Interfaces;

namespace OrdersApi.Repositories
{
    public class JobRepository : IJobRepository
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public JobRepository(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public async Task<Job> CreateAsync(Job job)
        {
            _context.Jobs.Add(job);
            await _context.SaveChangesAsync();
            return job;
        }

        public async Task<Job?> GetByIdAsync(Guid id)
        {
            return await _context.Jobs
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task UpdateAsync(Job job)
        {
            _context.Jobs.Update(job);
            await _context.SaveChangesAsync();
        }

        public async Task<int> ProcessOrdersAsync()
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection");

            await using var connection = new SqlConnection(connectionString);

            var result = await connection.QuerySingleAsync<int>(
                "ProcessOrders",
                commandType: System.Data.CommandType.StoredProcedure
            );

            return result;
        }
    }
}