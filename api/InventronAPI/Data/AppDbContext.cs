using System.Reflection;
using InventronAPI.Models.Entities;
using InventronAPI.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace InventronAPI.Data
{
	public class AppDbContext : DbContext
	{
		private static readonly MethodInfo ApplyTenantFilterMethod =
			typeof(AppDbContext).GetMethod(nameof(ApplyTenantFilter), BindingFlags.NonPublic | BindingFlags.Instance)!;

		private readonly ITenantProvider _tenantProvider;

		public AppDbContext(DbContextOptions<AppDbContext> options, ITenantProvider tenantProvider) : base(options)
		{
			_tenantProvider = tenantProvider;
		}

		public DbSet<Item> Items { get; set; }

		public DbSet<Supplier> Suppliers { get; set; }

		public DbSet<InventoryTransaction> InventoryTransactions { get; set; }

		// Read by the query filters at query time, so each context instance only sees its own tenant.
		// A null tenant matches no rows, so a missing tenant fails closed.
		private Guid? CurrentTenantId => _tenantProvider.TenantId;

		protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
		{
			// Money and quantities are never floating point; 4 decimal places covers unit costs and fractional UoMs.
			configurationBuilder.Properties<decimal>().HavePrecision(18, 4);
		}

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			modelBuilder.Entity<Item>(b =>
			{
				b.Property(i => i.Sku).HasMaxLength(64);
				b.Property(i => i.Name).HasMaxLength(200);
				b.Property(i => i.UnitOfMeasure).HasMaxLength(20);
				b.Property(i => i.RowVersion).IsRowVersion();

				b.HasIndex(i => new { i.TenantId, i.Sku }).IsUnique();

				b.HasOne(i => i.PreferredSupplier)
					.WithMany()
					.HasForeignKey(i => i.PreferredSupplierId)
					.OnDelete(DeleteBehavior.Restrict);
			});

			modelBuilder.Entity<Supplier>(b =>
			{
				b.Property(s => s.Company).HasMaxLength(200);
				b.Property(s => s.Email).HasMaxLength(256);
				b.Property(s => s.Phone).HasMaxLength(50);

				b.HasIndex(s => s.TenantId);
			});

			modelBuilder.Entity<InventoryTransaction>(b =>
			{
				b.Property(t => t.Reference).HasMaxLength(100);
				b.Property(t => t.Notes).HasMaxLength(1000);

				// On-hand is SUM(Quantity) per item within a tenant.
				b.HasIndex(t => new { t.TenantId, t.ItemId });

				b.HasOne(t => t.Item)
					.WithMany()
					.HasForeignKey(t => t.ItemId)
					.OnDelete(DeleteBehavior.Restrict);
			});

			foreach (var entityType in modelBuilder.Model.GetEntityTypes())
			{
				// Query filters can only be set on the root of an inheritance hierarchy.
				if (typeof(TenantEntity).IsAssignableFrom(entityType.ClrType) && entityType.BaseType is null)
				{
					ApplyTenantFilterMethod.MakeGenericMethod(entityType.ClrType).Invoke(this, [modelBuilder]);
				}
			}
		}

		public override int SaveChanges(bool acceptAllChangesOnSuccess)
		{
			StampTenantEntities();
			return base.SaveChanges(acceptAllChangesOnSuccess);
		}

		public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
		{
			StampTenantEntities();
			return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
		}

		private void ApplyTenantFilter<TEntity>(ModelBuilder modelBuilder) where TEntity : TenantEntity
		{
			modelBuilder.Entity<TEntity>().HasQueryFilter(e => e.TenantId == CurrentTenantId);
		}

		private void StampTenantEntities()
		{
			var now = DateTime.UtcNow;

			foreach (var entry in ChangeTracker.Entries<TenantEntity>())
			{
				if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted))
				{
					continue;
				}

				var tenantId = CurrentTenantId
					?? throw new InvalidOperationException("No tenant is set; tenant-owned rows cannot be saved.");

				if (entry.State == EntityState.Added)
				{
					if (entry.Entity.TenantId != Guid.Empty && entry.Entity.TenantId != tenantId)
					{
						throw new InvalidOperationException($"Cannot add a {entry.Metadata.ClrType.Name} for another tenant.");
					}

					entry.Entity.TenantId = tenantId;
					entry.Entity.CreatedAtUtc = now;
					continue;
				}

				// Attach/Update/Remove bypass the query filter, so check the row really belongs to this tenant.
				if (entry.Property(e => e.TenantId).OriginalValue != tenantId)
				{
					throw new InvalidOperationException($"Cannot change a {entry.Metadata.ClrType.Name} owned by another tenant.");
				}

				if (entry.State == EntityState.Modified)
				{
					entry.Property(e => e.TenantId).IsModified = false;
					entry.Property(e => e.CreatedAtUtc).IsModified = false;
					entry.Entity.UpdatedAtUtc = now;
				}
			}
		}
	}
}
