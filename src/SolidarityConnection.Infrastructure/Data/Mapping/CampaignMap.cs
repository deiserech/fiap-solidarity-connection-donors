using SolidarityConnection.Domain.Entities;
using SolidarityConnection.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SolidarityConnection.Infrastructure.Mapping
{
    public class CampaignMap : IEntityTypeConfiguration<Campaign>
    {
        public void Configure(EntityTypeBuilder<Campaign> builder)
        {
            builder.HasKey(p => p.Id);

            builder.Property(p => p.Id)
                .ValueGeneratedOnAdd();

            builder.Property(p => p.Title)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(p => p.Description)
                .HasMaxLength(1000);

            builder.Property(p => p.GoalAmount)
                .HasColumnType("decimal(18,2)");

            builder.Property(p => p.TotalAmountRaised)
                .HasColumnType("decimal(18,2)")
                .HasDefaultValue(0m);

            builder.Property(p => p.Status)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(p => p.CreatedAt)
                .IsRequired();

            builder.Property(p => p.UpdatedAt)
                .IsRequired();

            builder.ToTable("Campaigns");
        }
    }
}
